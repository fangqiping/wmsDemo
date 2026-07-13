using System.Text.Json;
using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.Consoles;
using FlowEngine.Execution.Design;
using FlowEngine.Execution.FlowEngine;
using FlowEngine.Execution.Scheduling;

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

    private readonly IReader _reader;

    public BackendDemoScheduleCandidateProvider(IReader reader) {
        _reader = reader;
    }

    public Task<IReadOnlyList<ScheduleCandidate>> GetCandidatesAsync(
        ScheduleCandidateContext context,
        CancellationToken cancellationToken) {
        var flowTasks = _reader.Get<FlowTaskDetail>()
            .ToDictionary(task => task.Id);
        var durationsByFlow = LoadDurationsByFlow();
        var inboundOrders = _reader.Get<InboundOrder>()
            .Where(order => order.FlowTaskId.HasValue)
            .ToDictionary(order => order.FlowTaskId!.Value);
        var outboundOrders = _reader.Get<OutboundOrder>()
            .Where(order => order.FlowTaskId.HasValue)
            .ToDictionary(order => order.FlowTaskId!.Value);

        var candidates = new List<ScheduleCandidate>();
        var operationTasks = _reader.Get<OperationTaskDetail>()
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
            var execution = new NodeExecution(
                key,
                expectedDuration,
                predecessors: null,
                occupancies: new[] { occupancy });

            var display = CreateDisplay(flowTask, operationTask.NodeId, fixedOperation, inboundOrders, outboundOrders);
            candidates.Add(new ScheduleCandidate(
                execution,
                display.Label,
                JsonSerializer.Serialize(display.Metadata, JsonOptions)));
        }

        return Task.FromResult<IReadOnlyList<ScheduleCandidate>>(candidates);
    }

    private DisplayInfo CreateDisplay(
        FlowTaskDetail flowTask,
        string nodeId,
        FixedConsoleOperation fixedOperation,
        IReadOnlyDictionary<long, InboundOrder> inboundOrders,
        IReadOnlyDictionary<long, OutboundOrder> outboundOrders) {
        var variables = (flowTask.VariableEntities ?? Enumerable.Empty<VariableEntity>()).ToDictionary(
            variable => variable.Id,
            variable => ReadVariableString(variable.Value),
            StringComparer.Ordinal);

        if (inboundOrders.TryGetValue(flowTask.Id, out var inboundOrder)) {
            var line = ResolveInboundLine(inboundOrder);
            var sku = variables.GetValueOrDefault("SkuCode")
                ?? line?.Sku?.Code
                ?? ResolveSkuCode(line?.SkuId);
            var pallet = variables.GetValueOrDefault("InboundPalletCode")
                ?? (!string.IsNullOrWhiteSpace(inboundOrder.Code) ? $"PLT-{inboundOrder.Code}" : null);
            var targetLocation = ResolveAcquiredLocationCode(flowTask, "AcquireTargetLocation")
                ?? variables.GetValueOrDefault("TargetLocationCode")
                ?? line?.TargetLocation?.Code
                ?? ResolveLocationCode(line?.TargetLocationId);
            var requestedTargetLocation = variables.GetValueOrDefault("RequestedTargetLocationCode")
                ?? line?.TargetLocation?.Code
                ?? ResolveLocationCode(line?.TargetLocationId);

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
            var line = ResolveOutboundLine(outboundOrder);
            var sku = variables.GetValueOrDefault("SkuCode")
                ?? line?.Sku?.Code
                ?? ResolveSkuCode(line?.SkuId);
            var sourcePalletId = ReadVariableInt(
                flowTask.VariableEntities ?? Enumerable.Empty<VariableEntity>(),
                "SourcePalletId");
            var pallet = ResolvePalletCode(sourcePalletId);
            var sourceLocation = ResolveAcquiredLocationCode(flowTask, "AcquireSourceLocation");
            var requestedSourceLocation = variables.GetValueOrDefault("RequestedSourceLocationCode")
                ?? line?.SourceLocation?.Code
                ?? ResolveLocationCode(line?.SourceLocationId);
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

    private Dictionary<string, Dictionary<string, TimeSpan>> LoadDurationsByFlow() {
        var result = new Dictionary<string, Dictionary<string, TimeSpan>>(StringComparer.Ordinal);
        foreach (var version in _reader.Get<FlowVersion>()) {
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

    private InboundOrderLine? ResolveInboundLine(InboundOrder order) {
        return order.Lines.FirstOrDefault()
            ?? _reader.Get<InboundOrderLine>().FirstOrDefault(line => line.InboundOrderId == order.Id);
    }

    private OutboundOrderLine? ResolveOutboundLine(OutboundOrder order) {
        return order.Lines.FirstOrDefault()
            ?? _reader.Get<OutboundOrderLine>().FirstOrDefault(line => line.OutboundOrderId == order.Id);
    }

    private string? ResolveSkuCode(int? skuId) {
        if (!skuId.HasValue) {
            return null;
        }
        return _reader.Get<Sku>().FirstOrDefault(sku => sku.Id == skuId.Value)?.Code;
    }

    private string? ResolveLocationCode(int? locationId) {
        if (!locationId.HasValue) {
            return null;
        }
        return _reader.Get<Location>().FirstOrDefault(location => location.Id == locationId.Value)?.Code;
    }

    private string? ResolvePalletCode(int? palletId) {
        if (!palletId.HasValue) {
            return null;
        }
        return _reader.Get<Pallet>().FirstOrDefault(pallet => pallet.Id == palletId.Value)?.Code
            ?? $"Pallet-{palletId.Value}";
    }

    private string? ResolveAcquiredLocationCode(FlowTaskDetail flowTask, string acquireNodeId) {
        var resourceDetail = (flowTask.ResourceDetails ?? Enumerable.Empty<ResourceDetail>())
            .Where(resource => resource.NodeId == acquireNodeId)
            .Where(resource => resource.ResourceType == typeof(Location).FullName)
            .OrderByDescending(resource => resource.AcquiredAt)
            .FirstOrDefault();
        if (resourceDetail == null || !int.TryParse(resourceDetail.ResourceId, out var locationId)) {
            return null;
        }
        return ResolveLocationCode(locationId);
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
