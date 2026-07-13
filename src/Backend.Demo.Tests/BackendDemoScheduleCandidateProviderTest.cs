using System.Text.Json;
using Backend.Demo.DependencyInjection;
using Backend.Demo.Domain;
using Backend.Demo.Scheduling;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.Consoles;
using FlowEngine.Execution.Design;
using FlowEngine.Execution.FlowEngine;
using FlowEngine.Execution.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Demo.Tests;

public sealed class BackendDemoScheduleCandidateProviderTest {
    [Fact]
    public async Task GetCandidatesAsync_MapsFixedConsoleOperationNodes() {
        var reader = new InMemoryReader(
            FlowTask(101, "inbound-runtime", "OrderCode", "\"IN-1001\"", "InboundPalletCode", "\"PLT-IN-1001\"", "TargetLocationCode", "\"RACK-A1\"", "SkuCode", "\"SKU-A\""),
            FlowTask(102, "outbound-runtime", "OrderCode", "\"OUT-2001\"", "SourceLocationCode", "\"RACK-B2\"", "TargetLocationCode", "\"SHIP-DOCK\"", "SourcePalletId", "301", "SkuCode", "\"SKU-B\""));
        reader.AddRange(
            Operation(1001, 101, "ConveyorToInboundPort"),
            Operation(1002, 101, "StackCraneMoveToRack"),
            Operation(1003, 102, "StackCraneMoveToOutboundPort"),
            Operation(1004, 102, "ConveyorFromOutboundPort"));
        reader.AddRange(
            FlowVersion("inbound-runtime", Node("ConveyorToInboundPort", 45_000), Node("StackCraneMoveToRack", 120_000)),
            FlowVersion("outbound-runtime", Node("StackCraneMoveToOutboundPort", 120_000), Node("ConveyorFromOutboundPort", 45_000)));
        reader.AddRange(InboundOrder(11, 101, "IN-1001", "PLT-IN-1001", "SKU-A", "RACK-A1"));
        reader.AddRange(OutboundOrder(21, 102, "OUT-2001", "SKU-B", "RACK-B1", "SHIP-DOCK"));

        var candidates = await new BackendDemoScheduleCandidateProvider(reader)
            .GetCandidatesAsync(Context(), CancellationToken.None);

        Assert.Equal(4, candidates.Count);
        AssertCandidate(candidates.Single(candidate => candidate.Execution.NodeId == "ConveyorFromOutboundPort"),
            102, "ConveyorFromOutboundPort", ConveyorConsole.NAME, TimeSpan.FromSeconds(45));
        AssertCandidate(candidates.Single(candidate => candidate.Execution.NodeId == "ConveyorToInboundPort"),
            101, "ConveyorToInboundPort", ConveyorConsole.NAME, TimeSpan.FromSeconds(45));
        AssertCandidate(candidates.Single(candidate => candidate.Execution.NodeId == "StackCraneMoveToRack"),
            101, "StackCraneMoveToRack", StackCraneConsole.NAME, TimeSpan.FromSeconds(120));
        AssertCandidate(candidates.Single(candidate => candidate.Execution.NodeId == "StackCraneMoveToOutboundPort"),
            102, "StackCraneMoveToOutboundPort", StackCraneConsole.NAME, TimeSpan.FromSeconds(120));

        var inboundStoreCandidate = Assert.Single(candidates, candidate => candidate.Execution.NodeId == "StackCraneMoveToRack");
        Assert.Equal("IN-1001 / PLT-IN-1001 · 上架", inboundStoreCandidate.DisplayLabel);
        using var inboundContext = JsonDocument.Parse(inboundStoreCandidate.DisplayContextJson!);
        Assert.Equal("inbound", inboundContext.RootElement.GetProperty("orderType").GetString());
        Assert.Equal(11, inboundContext.RootElement.GetProperty("orderId").GetInt32());
        Assert.Equal("IN-1001", inboundContext.RootElement.GetProperty("orderCode").GetString());
        Assert.Equal("SKU-A", inboundContext.RootElement.GetProperty("sku").GetString());
        Assert.Equal("PLT-IN-1001", inboundContext.RootElement.GetProperty("pallet").GetString());
        Assert.Equal("RACK-A1", inboundContext.RootElement.GetProperty("targetLocation").GetString());
    }

    [Fact]
    public async Task GetCandidatesAsync_UsesOneSecondDurationFallback_WhenNodeMetadataDurationIsMissing() {
        var reader = new InMemoryReader(FlowTask(201, "inbound-runtime", "OrderCode", "\"IN-1002\""));
        reader.AddRange(Operation(2001, 201, "ConveyorToInboundPort"));
        reader.AddRange(FlowVersion("inbound-runtime", NodeWithoutDuration("ConveyorToInboundPort")));

        var candidate = Assert.Single(await new BackendDemoScheduleCandidateProvider(reader)
            .GetCandidatesAsync(Context(), CancellationToken.None));

        Assert.Equal(TimeSpan.FromSeconds(1), candidate.Execution.ExpectedDuration);
        Assert.Equal(TimeSpan.FromSeconds(1), Assert.Single(candidate.Execution.Occupancies).ExpectedDuration);
    }

    [Fact]
    public async Task GetCandidatesAsync_RejectsUnknownConsoleNodes_AndDoesNotEmitDynamicResources() {
        var reader = new InMemoryReader(FlowTask(
            301,
            "inbound-runtime",
            "OrderCode", "\"IN-1003\""));
        reader.AddRange(
            Operation(3001, 301, "AcquireInboundPort"),
            Operation(3002, 301, "AcquireTargetLocation"),
            Operation(3003, 301, "AcquireSourcePallet"),
            Operation(3004, 301, "CustomConveyorHold", ConveyorConsole.NAME),
            Operation(3005, 301, "ConveyorToInboundPort"));
        reader.AddRange(FlowVersion(
            "inbound-runtime",
            Node("AcquireInboundPort", 30_000),
            Node("AcquireTargetLocation", 30_000),
            Node("AcquireSourcePallet", 15_000),
            Node("CustomConveyorHold", 60_000),
            Node("ConveyorToInboundPort", 45_000)));

        var candidate = Assert.Single(await new BackendDemoScheduleCandidateProvider(reader)
            .GetCandidatesAsync(Context(), CancellationToken.None));

        Assert.Equal("ConveyorToInboundPort", candidate.Execution.NodeId);
        Assert.All(candidate.Execution.Occupancies, occupancy => {
            Assert.Equal(typeof(ConsoleInfo).FullName, occupancy.ResourceType);
            Assert.DoesNotContain(nameof(Location), occupancy.ResourceType);
            Assert.DoesNotContain(nameof(Port), occupancy.ResourceType);
            Assert.DoesNotContain(nameof(Pallet), occupancy.ResourceType);
        });
    }

    [Fact]
    public async Task GetCandidatesAsync_UsesActualAcquiredSourceLocation_ForOutboundFallbackContext() {
        var actualSource = new Location { Id = 2, Code = "RACK-A2" };
        var requestedSource = new Location { Id = 1, Code = "RACK-A1" };
        var sku = new Sku { Id = 7, Code = "SKU-B" };
        var reader = new InMemoryReader(FlowTask(
            401,
            "outbound-runtime",
            new ResourceDetail {
                FlowTaskId = 401,
                NodeId = "AcquireSourceLocation",
                ResourceType = typeof(Location).FullName!,
                ResourceId = actualSource.Id.ToString()
            },
            "OrderCode", "\"OUT-2002\"",
            "RequestedSourceLocationCode", "\"RACK-A1\"",
            "TargetLocationCode", "\"SHIP-DOCK\"",
            "SkuCode", "\"SKU-B\""));
        reader.AddRange(Operation(4001, 401, "StackCraneMoveToOutboundPort"));
        reader.AddRange(FlowVersion("outbound-runtime", Node("StackCraneMoveToOutboundPort", 120_000)));
        reader.AddRange(new OutboundOrder {
            Id = 31,
            Code = "OUT-2002",
            Destination = "SHIP-DOCK",
            FlowTaskId = 401,
            Lines = {
                new OutboundOrderLine {
                    SkuId = sku.Id,
                    Sku = sku,
                    SourceLocationId = requestedSource.Id,
                    SourceLocation = requestedSource
                }
            }
        });
        reader.AddRange(actualSource, requestedSource);

        var candidate = Assert.Single(await new BackendDemoScheduleCandidateProvider(reader)
            .GetCandidatesAsync(Context(), CancellationToken.None));

        using var context = JsonDocument.Parse(candidate.DisplayContextJson!);
        Assert.Equal("RACK-A2", context.RootElement.GetProperty("sourceLocation").GetString());
        Assert.Equal("RACK-A1", context.RootElement.GetProperty("requestedSourceLocation").GetString());
    }

    [Fact]
    public void AddBackendDemoApplication_RegistersScheduleCandidateProvider() {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBackendDemoApplication("Data Source=:memory:");

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IScheduleCandidateProvider) &&
            descriptor.ImplementationType == typeof(BackendDemoScheduleCandidateProvider) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    private static void AssertCandidate(
        ScheduleCandidate candidate,
        long flowTaskId,
        string nodeId,
        string resourceId,
        TimeSpan duration) {
        Assert.Equal(flowTaskId, candidate.Execution.Key.FlowTaskId);
        Assert.Equal(flowTaskId, candidate.Execution.FlowTaskId);
        Assert.Equal(nodeId, candidate.Execution.NodeId);
        Assert.Equal(1, candidate.Execution.Occurrence);
        Assert.Equal(duration, candidate.Execution.ExpectedDuration);

        var occupancy = Assert.Single(candidate.Execution.Occupancies);
        Assert.Equal(typeof(ConsoleInfo).FullName, occupancy.ResourceType);
        Assert.Equal(resourceId, occupancy.ResourceId);
        Assert.Equal(TimeSpan.Zero, occupancy.StartOffset);
        Assert.Equal(duration, occupancy.ExpectedDuration);
    }

    private static ScheduleCandidateContext Context() {
        var now = DateTimeOffset.Parse("2026-07-13T00:00:00Z");
        var plan = new SchedulePlan(
            version: 1,
            horizonStart: now,
            horizonEnd: now.AddHours(8),
            solverStatus: ScheduleSolverStatus.Unknown,
            trigger: ScheduleTrigger.Initial,
            triggerDetail: "test",
            createdAt: now,
            items: Array.Empty<SchedulePlanItem>(),
            makespan: null);
        return new ScheduleCandidateContext(now, now.AddHours(8), plan, now);
    }

    private static OperationTaskDetail Operation(
        long id,
        long parentFlowTaskId,
        string nodeId,
        string? consoleId = null) {
        return new OperationTaskDetail {
            Id = id,
            ParentFlowTaskId = parentFlowTaskId,
            NodeId = nodeId,
            ConsoleId = consoleId ?? FunctionConsole.NAME,
            Status = ExecutableStatus.Scheduled
        };
    }

    private static FlowTaskDetail FlowTask(long id, string flowId, params string[] variables) {
        return FlowTask(id, flowId, Array.Empty<ResourceDetail>(), variables);
    }

    private static FlowTaskDetail FlowTask(long id, string flowId, ResourceDetail resource, params string[] variables) {
        return FlowTask(id, flowId, new[] { resource }, variables);
    }

    private static FlowTaskDetail FlowTask(long id, string flowId, IEnumerable<ResourceDetail> resources, params string[] variables) {
        var detail = new FlowTaskDetail {
            Id = id,
            FlowId = flowId,
            Status = ExecutableStatus.Started,
            VariableEntities = new List<VariableEntity>(),
            ResourceDetails = new List<ResourceDetail>()
        };
        for (var index = 0; index < variables.Length; index += 2) {
            detail.VariableEntities.Add(new VariableEntity {
                FlowTaskId = id,
                Id = variables[index],
                Value = variables[index + 1]
            });
        }
        detail.ResourceDetails.AddRange(resources);
        return detail;
    }

    private static InboundOrder InboundOrder(int id, long flowTaskId, string code, string pallet, string skuCode, string targetLocationCode) {
        var sku = new Sku { Id = 1, Code = skuCode };
        return new InboundOrder {
            Id = id,
            Code = code,
            FlowTaskId = flowTaskId,
            Lines = {
                new InboundOrderLine {
                    SkuId = sku.Id,
                    Sku = sku,
                    TargetLocationId = 1,
                    TargetLocation = new Location { Id = 1, Code = targetLocationCode }
                }
            },
            Remark = pallet
        };
    }

    private static OutboundOrder OutboundOrder(int id, long flowTaskId, string code, string skuCode, string sourceLocationCode, string destination) {
        var sku = new Sku { Id = 2, Code = skuCode };
        return new OutboundOrder {
            Id = id,
            Code = code,
            Destination = destination,
            FlowTaskId = flowTaskId,
            Lines = {
                new OutboundOrderLine {
                    SkuId = sku.Id,
                    Sku = sku,
                    SourceLocationId = 2,
                    SourceLocation = new Location { Id = 2, Code = sourceLocationCode }
                }
            }
        };
    }

    private static FlowVersion FlowVersion(string runtimeFlowId, params object[] nodes) {
        return new FlowVersion {
            Id = runtimeFlowId.GetHashCode(),
            RuntimeFlowId = runtimeFlowId,
            SourceGraphJson = JsonSerializer.Serialize(new {
                nodes
            }),
            CompiledGraphJson = "{}"
        };
    }

    private static object Node(string id, long estimatedDurationMilliseconds) => new {
        id,
        estimatedDurationMilliseconds
    };

    private static object NodeWithoutDuration(string id) => new {
        id
    };

    private sealed class InMemoryReader : IReader {
        private readonly Dictionary<Type, List<object>> _entities = new();

        public InMemoryReader(params FlowTaskDetail[] flowTasks) {
            AddRange(flowTasks);
        }

        public IQueryable<TEntity> Get<TEntity>() where TEntity : class, IEntity {
            return _entities.TryGetValue(typeof(TEntity), out var entities)
                ? entities.Cast<TEntity>().AsQueryable()
                : Enumerable.Empty<TEntity>().AsQueryable();
        }

        public void AddRange<TEntity>(params TEntity[] entities) {
            var list = _entities.GetValueOrDefault(typeof(TEntity));
            if (list == null) {
                list = new List<object>();
                _entities[typeof(TEntity)] = list;
            }
            list.AddRange(entities.Cast<object>());
        }
    }
}
