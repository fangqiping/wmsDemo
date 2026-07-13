using System.Text.Json;
using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.Consoles;
using FlowEngine.Execution.Design;
using FlowEngine.Execution.FlowEngine;
using FlowEngine.Execution.Scheduling;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Demo.Scheduling;

public sealed class BackendDemoScheduleCandidateProvider : IScheduleCandidateProvider {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, FixedConsoleOperation> FixedConsoleOperations =
        new Dictionary<string, FixedConsoleOperation>(StringComparer.Ordinal) {
            ["ConveyorToInboundPort"] = new(ConveyorConsole.NAME, "入库输送"),
            ["StackCraneMoveToRack"] = new(StackCraneConsole.NAME, "上架"),
            ["StackCraneMoveToOutboundPort"] = new(StackCraneConsole.NAME, "下架"),
            ["ConveyorFromOutboundPort"] = new(ConveyorConsole.NAME, "出库输送")
        };

    private readonly IServiceScopeFactory _scopeFactory;

    public BackendDemoScheduleCandidateProvider(IServiceScopeFactory scopeFactory) {
        _scopeFactory = scopeFactory;
    }

    public Task<IReadOnlyList<ScheduleCandidate>> GetCandidatesAsync(
        ScheduleCandidateContext context,
        CancellationToken cancellationToken) {
        using var scope = _scopeFactory.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IReader>();
        var flowTasks = reader.Get<FlowTaskDetail>()
            .ToDictionary(task => task.Id);
        var variablesByFlowTaskId = reader.Get<FlowTaskDetail>()
            .SelectMany(task => task.VariableEntities!)
            .GroupBy(variable => variable.FlowTaskId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var resourcesByFlowTaskId = reader.Get<ResourceDetail>()
            .GroupBy(resource => resource.FlowTaskId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var durationsByFlow = LoadDurationsByFlow(reader);
        var inboundOrders = reader.Get<InboundOrder>()
            .Where(order => order.FlowTaskId.HasValue)
            .ToDictionary(order => order.FlowTaskId!.Value);
        var outboundOrders = reader.Get<OutboundOrder>()
            .Where(order => order.FlowTaskId.HasValue)
            .ToDictionary(order => order.FlowTaskId!.Value);

        var candidates = new List<ScheduleCandidate>();
        var operationTasks = reader.Get<OperationTaskDetail>()
            .Where(task => task.ParentFlowTaskId.HasValue)
            .ToList()
            .Where(task => !task.Acknowledged)
            .Where(task => task.Status is ExecutableStatus.Scheduled or ExecutableStatus.Starting or ExecutableStatus.Started)
            .OrderBy(task => task.ParentFlowTaskId)
            .ThenBy(task => task.Id)
            .ToList();

        foreach (var operationTask in operationTasks) {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(operationTask.NodeId)
                || !FixedConsoleOperations.TryGetValue(operationTask.NodeId, out var fixedOperation)) {
                continue;
            }

            var flowTaskId = operationTask.ParentFlowTaskId!.Value;
            if (!flowTasks.TryGetValue(flowTaskId, out var flowTask)) {
                continue;
            }

            var expectedDuration = ResolveDuration(durationsByFlow, flowTask.FlowId, operationTask.NodeId);
            var occurrence = operationTask.ScheduleOccurrence.GetValueOrDefault(1);
            var key = new ScheduleTaskKey(flowTaskId, operationTask.NodeId, occurrence);
            var occupancy = new ResourceOccupancy(
                typeof(ConsoleInfo).FullName!,
                fixedOperation.ConsoleId,
                TimeSpan.Zero,
                expectedDuration);
            if (HasOpenFixedConsoleOccupancyAfterNodeClosed(context.CurrentPlan, key, occupancy)) {
                continue;
            }

            var execution = new NodeExecution(
                key,
                expectedDuration,
                predecessors: null,
                occupancies: new[] { occupancy });

            var display = CreateDisplay(
                reader,
                flowTask,
                operationTask.NodeId,
                fixedOperation,
                inboundOrders,
                outboundOrders,
                variablesByFlowTaskId,
                resourcesByFlowTaskId);
            candidates.Add(new ScheduleCandidate(
                execution,
                display.Label,
                JsonSerializer.Serialize(display.Metadata, JsonOptions)));
        }

        return Task.FromResult<IReadOnlyList<ScheduleCandidate>>(candidates);
    }

    private static bool HasOpenFixedConsoleOccupancyAfterNodeClosed(
        SchedulePlan? currentPlan,
        ScheduleTaskKey key,
        ResourceOccupancy occupancy) {
        if (currentPlan == null) {
            return false;
        }

        var hasClosedNodeExecution = currentPlan.Items.Any(item =>
            item.ItemKind == ScheduleItemKind.NodeExecution
            && MatchesKey(item, key)
            && IsClosed(item));
        if (!hasClosedNodeExecution) {
            return false;
        }

        return currentPlan.Items.Any(item =>
            item.ItemKind == ScheduleItemKind.ResourceOccupancy
            && MatchesKey(item, key)
            && item.ResourceType == occupancy.ResourceType
            && item.ResourceId == occupancy.ResourceId
            && item.ActualEnd == null);
    }

    private static bool MatchesKey(SchedulePlanItem item, ScheduleTaskKey key) {
        return item.FlowTaskId == key.FlowTaskId
            && item.NodeId == key.NodeId
            && item.Occurrence == key.Occurrence;
    }

    private static bool IsClosed(SchedulePlanItem item) {
        return item.ActualEnd.HasValue
            || item.Status is SchedulePlanItemStatus.Completed or SchedulePlanItemStatus.Canceled;
    }

    private DisplayInfo CreateDisplay(
        IReader reader,
        FlowTaskDetail flowTask,
        string nodeId,
        FixedConsoleOperation fixedOperation,
        IReadOnlyDictionary<long, InboundOrder> inboundOrders,
        IReadOnlyDictionary<long, OutboundOrder> outboundOrders,
        IReadOnlyDictionary<long, List<VariableEntity>> variablesByFlowTaskId,
        IReadOnlyDictionary<long, List<ResourceDetail>> resourcesByFlowTaskId) {
        var variableEntities = ResolveVariables(flowTask, variablesByFlowTaskId);
        var resourceDetails = ResolveResources(flowTask, resourcesByFlowTaskId);
        var variables = variableEntities.ToDictionary(
            variable => variable.Id,
            variable => ReadVariableString(variable.Value),
            StringComparer.Ordinal);

        if (inboundOrders.TryGetValue(flowTask.Id, out var inboundOrder)) {
            var line = ResolveInboundLine(reader, inboundOrder);
            var sku = variables.GetValueOrDefault("SkuCode")
                ?? line?.Sku?.Code
                ?? ResolveSkuCode(reader, line?.SkuId);
            var pallet = variables.GetValueOrDefault("InboundPalletCode")
                ?? (!string.IsNullOrWhiteSpace(inboundOrder.Code) ? $"PLT-{inboundOrder.Code}" : null);
            var targetLocation = ResolveAcquiredLocationCode(reader, resourceDetails, "AcquireTargetLocation")
                ?? variables.GetValueOrDefault("TargetLocationCode")
                ?? line?.TargetLocation?.Code
                ?? ResolveLocationCode(reader, line?.TargetLocationId);
            var requestedTargetLocation = variables.GetValueOrDefault("RequestedTargetLocationCode")
                ?? line?.TargetLocation?.Code
                ?? ResolveLocationCode(reader, line?.TargetLocationId);

            return new DisplayInfo(
                $"{inboundOrder.Code} / {pallet ?? "-"} · {fixedOperation.OperationLabel}",
                new BackendDemoScheduleDisplayMetadata(
                    OrderType: "inbound",
                    OrderId: inboundOrder.Id,
                    OrderCode: inboundOrder.Code,
                    Sku: sku,
                    Pallet: pallet,
                    SourceLocation: variables.GetValueOrDefault("SourceLocationCode"),
                    RequestedSourceLocation: null,
                    TargetLocation: targetLocation,
                    RequestedTargetLocation: requestedTargetLocation));
        }

        if (outboundOrders.TryGetValue(flowTask.Id, out var outboundOrder)) {
            var line = ResolveOutboundLine(reader, outboundOrder);
            var sku = variables.GetValueOrDefault("SkuCode")
                ?? line?.Sku?.Code
                ?? ResolveSkuCode(reader, line?.SkuId);
            var sourcePalletId = ReadVariableInt(variableEntities, "SourcePalletId");
            var pallet = ResolvePalletCode(reader, sourcePalletId);
            var sourceLocation = ResolveAcquiredLocationCode(reader, resourceDetails, "AcquireSourceLocation");
            var requestedSourceLocation = variables.GetValueOrDefault("RequestedSourceLocationCode")
                ?? line?.SourceLocation?.Code
                ?? ResolveLocationCode(reader, line?.SourceLocationId);
            var targetLocation = variables.GetValueOrDefault("TargetLocationCode")
                ?? outboundOrder.Destination;

            return new DisplayInfo(
                $"{outboundOrder.Code} / {pallet ?? "-"} · {fixedOperation.OperationLabel}",
                new BackendDemoScheduleDisplayMetadata(
                    OrderType: "outbound",
                    OrderId: outboundOrder.Id,
                    OrderCode: outboundOrder.Code,
                    Sku: sku,
                    Pallet: pallet,
                    SourceLocation: sourceLocation,
                    RequestedSourceLocation: requestedSourceLocation,
                    TargetLocation: targetLocation,
                    RequestedTargetLocation: null));
        }

        var orderCode = variables.GetValueOrDefault("OrderCode");
        return new DisplayInfo(
            $"{orderCode ?? flowTask.Id.ToString()} · {fixedOperation.OperationLabel}",
            new BackendDemoScheduleDisplayMetadata(
                OrderType: "unknown",
                OrderId: null,
                OrderCode: orderCode,
                Sku: variables.GetValueOrDefault("SkuCode"),
                Pallet: null,
                SourceLocation: null,
                RequestedSourceLocation: null,
                TargetLocation: null,
                RequestedTargetLocation: null));
    }

    private static List<VariableEntity> ResolveVariables(
        FlowTaskDetail flowTask,
        IReadOnlyDictionary<long, List<VariableEntity>> variablesByFlowTaskId) {
        if (variablesByFlowTaskId.TryGetValue(flowTask.Id, out var variables)) {
            return variables;
        }
        return flowTask.VariableEntities ?? new List<VariableEntity>();
    }

    private static List<ResourceDetail> ResolveResources(
        FlowTaskDetail flowTask,
        IReadOnlyDictionary<long, List<ResourceDetail>> resourcesByFlowTaskId) {
        if (resourcesByFlowTaskId.TryGetValue(flowTask.Id, out var resources)) {
            return resources;
        }
        return flowTask.ResourceDetails ?? new List<ResourceDetail>();
    }

    private static Dictionary<string, Dictionary<string, TimeSpan>> LoadDurationsByFlow(IReader reader) {
        var result = new Dictionary<string, Dictionary<string, TimeSpan>>(StringComparer.Ordinal);
        foreach (var version in reader.Get<FlowVersion>()) {
            var nodeDurations = ParseNodeDurations(version.SourceGraphJson);
            if (nodeDurations.Count == 0) {
                nodeDurations = ParseNodeDurations(version.CompiledGraphJson);
            }
            result[version.RuntimeFlowId] = nodeDurations;
        }
        return result;
    }

    private static Dictionary<string, TimeSpan> ParseNodeDurations(string? graphJson) {
        var result = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(graphJson)) {
            return result;
        }

        try {
            using var document = JsonDocument.Parse(graphJson);
            if (!document.RootElement.TryGetProperty("nodes", out var nodes)
                || nodes.ValueKind != JsonValueKind.Array) {
                return result;
            }

            foreach (var node in nodes.EnumerateArray()) {
                if (!node.TryGetProperty("id", out var idProperty)
                    || idProperty.GetString() is not { Length: > 0 } nodeId
                    || !node.TryGetProperty("estimatedDurationMilliseconds", out var durationProperty)
                    || !durationProperty.TryGetInt64(out var milliseconds)
                    || milliseconds <= 0) {
                    continue;
                }
                result[nodeId] = TimeSpan.FromMilliseconds(milliseconds);
            }
        } catch (JsonException) {
            return result;
        }

        return result;
    }

    private static TimeSpan ResolveDuration(
        IReadOnlyDictionary<string, Dictionary<string, TimeSpan>> durationsByFlow,
        string flowId,
        string nodeId) {
        if (durationsByFlow.TryGetValue(flowId, out var durations)
            && durations.TryGetValue(nodeId, out var duration)
            && duration > TimeSpan.Zero) {
            return duration;
        }
        return TimeSpan.FromSeconds(1);
    }

    private static InboundOrderLine? ResolveInboundLine(IReader reader, InboundOrder order) {
        return order.Lines.FirstOrDefault()
            ?? reader.Get<InboundOrderLine>().FirstOrDefault(line => line.InboundOrderId == order.Id);
    }

    private static OutboundOrderLine? ResolveOutboundLine(IReader reader, OutboundOrder order) {
        return order.Lines.FirstOrDefault()
            ?? reader.Get<OutboundOrderLine>().FirstOrDefault(line => line.OutboundOrderId == order.Id);
    }

    private static string? ResolveSkuCode(IReader reader, int? skuId) {
        if (!skuId.HasValue) {
            return null;
        }
        return reader.Get<Sku>().FirstOrDefault(sku => sku.Id == skuId.Value)?.Code;
    }

    private static string? ResolveLocationCode(IReader reader, int? locationId) {
        if (!locationId.HasValue) {
            return null;
        }
        return reader.Get<Location>().FirstOrDefault(location => location.Id == locationId.Value)?.Code;
    }

    private static string? ResolvePalletCode(IReader reader, int? palletId) {
        if (!palletId.HasValue) {
            return null;
        }
        return reader.Get<Pallet>().FirstOrDefault(pallet => pallet.Id == palletId.Value)?.Code
            ?? $"Pallet-{palletId.Value}";
    }

    private static string? ResolveAcquiredLocationCode(
        IReader reader,
        IEnumerable<ResourceDetail> resourceDetails,
        string acquireNodeId) {
        var resourceDetail = resourceDetails
            .Where(resource => resource.NodeId == acquireNodeId)
            .Where(resource => resource.ResourceType == typeof(Location).FullName)
            .OrderByDescending(resource => resource.AcquiredAt)
            .FirstOrDefault();
        if (resourceDetail == null || !int.TryParse(resourceDetail.ResourceId, out var locationId)) {
            return null;
        }
        return ResolveLocationCode(reader, locationId);
    }

    private static string? ReadVariableString(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        try {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind switch {
                JsonValueKind.String => document.RootElement.GetString(),
                JsonValueKind.Number => document.RootElement.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => null
            };
        } catch (JsonException) {
            return value;
        }
    }

    private static int? ReadVariableInt(IEnumerable<VariableEntity> variables, string id) {
        var value = variables.FirstOrDefault(variable => variable.Id == id)?.Value;
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        try {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind == JsonValueKind.Number
                && document.RootElement.TryGetInt32(out var numericValue)) {
                return numericValue;
            }
            if (document.RootElement.ValueKind == JsonValueKind.String
                && int.TryParse(document.RootElement.GetString(), out var stringValue)) {
                return stringValue;
            }
        } catch (JsonException) {
            if (int.TryParse(value, out var rawValue)) {
                return rawValue;
            }
        }

        return null;
    }

    private sealed record FixedConsoleOperation(string ConsoleId, string OperationLabel);

    private sealed record DisplayInfo(string Label, BackendDemoScheduleDisplayMetadata Metadata);
}
