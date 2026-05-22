using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.Design;
using FlowEngine.Execution.FlowEngine;
using Microsoft.EntityFrameworkCore;

namespace Backend.Demo.Application;

public sealed class OrderFlowService : IOrderFlowService {
    private readonly IManager _manager;
    private readonly IManager<string, IFlow> _flowManager;
    private readonly IFlowScheduler _flowScheduler;

    public OrderFlowService(IManager manager, IManager<string, IFlow> flowManager, IFlowScheduler flowScheduler) {
        _manager = manager;
        _flowManager = flowManager;
        _flowScheduler = flowScheduler;
    }

    public async Task<InboundOrder> StartInboundFlowAsync(int orderId) {
        var order = await _manager.GetByIdAsync<int, InboundOrder>(
            orderId,
            orders => orders.Include(entity => entity.Lines))
            ?? throw new ArgumentException($"InboundOrder-{orderId} not found.", nameof(orderId));
        var firstLine = order.Lines.FirstOrDefault()
            ?? throw new ArgumentException($"InboundOrder-{orderId} does not contain any lines.", nameof(orderId));

        var sku = await _manager.GetByIdAsync<int, Sku>(firstLine.SkuId)
            ?? throw new ArgumentException($"Sku-{firstLine.SkuId} not found.");
        var targetLocation = await _manager.GetByIdAsync<int, Location>(firstLine.TargetLocationId)
            ?? throw new ArgumentException($"Location-{firstLine.TargetLocationId} not found.");

        var (definition, version, flow) = await ResolveFlowAsync(BusinessFlowType.InboundOrder, order.FlowDefinitionCode);
        var options = new ExecutionOptions.Builder(flow)
            .WithInput(order.Code, "OrderCode")
            .WithInput(order.Source, "SourceLocationCode")
            .WithInput(targetLocation.WarehouseId, "WarehouseId")
            .WithInput(targetLocation.Code, "TargetLocationCode")
            .WithInput(targetLocation.Id, "TargetLocationId")
            .WithInput(sku.Id, "SkuId")
            .WithInput(sku.Code, "SkuCode")
            .Build();

        var flowTask = await _flowScheduler.ScheduleAsync(flow, options);
        order = await _manager.UpdateAsync<int, InboundOrder>(order.Id, entity => {
            entity.FlowDefinitionCode = definition.Id;
            entity.FlowVersionNumber = version.VersionNumber;
            entity.FlowTaskId = flowTask.Id;
            entity.Status = OrderStatus.Running;
            entity.UpdatedTime = DateTimeOffset.UtcNow;
        });
        order = await SyncTerminalInboundOrderStatusAsync(order);
        return order;
    }

    public async Task<OutboundOrder> StartOutboundFlowAsync(int orderId) {
        var order = await _manager.GetByIdAsync<int, OutboundOrder>(
            orderId,
            orders => orders.Include(entity => entity.Lines))
            ?? throw new ArgumentException($"OutboundOrder-{orderId} not found.", nameof(orderId));
        var firstLine = order.Lines.FirstOrDefault()
            ?? throw new ArgumentException($"OutboundOrder-{orderId} does not contain any lines.", nameof(orderId));

        var sku = await _manager.GetByIdAsync<int, Sku>(firstLine.SkuId)
            ?? throw new ArgumentException($"Sku-{firstLine.SkuId} not found.");
        var sourceLocation = await _manager.GetByIdAsync<int, Location>(firstLine.SourceLocationId)
            ?? throw new ArgumentException($"Location-{firstLine.SourceLocationId} not found.");

        var (definition, version, flow) = await ResolveFlowAsync(BusinessFlowType.OutboundOrder, order.FlowDefinitionCode);
        var options = new ExecutionOptions.Builder(flow)
            .WithInput(order.Code, "OrderCode")
            .WithInput(sourceLocation.WarehouseId, "WarehouseId")
            .WithInput(sourceLocation.Code, "SourceLocationCode")
            .WithInput(sourceLocation.Id, "SourceLocationId")
            .WithInput(order.Destination, "TargetLocationCode")
            .WithInput(sku.Id, "SkuId")
            .WithInput(sku.Code, "SkuCode")
            .Build();

        var flowTask = await _flowScheduler.ScheduleAsync(flow, options);
        order = await _manager.UpdateAsync<int, OutboundOrder>(order.Id, entity => {
            entity.FlowDefinitionCode = definition.Id;
            entity.FlowVersionNumber = version.VersionNumber;
            entity.FlowTaskId = flowTask.Id;
            entity.Status = OrderStatus.Running;
            entity.UpdatedTime = DateTimeOffset.UtcNow;
        });
        order = await SyncTerminalOutboundOrderStatusAsync(order);
        return order;
    }

    private async Task<InboundOrder> SyncTerminalInboundOrderStatusAsync(InboundOrder order) {
        if (!order.FlowTaskId.HasValue) {
            return order;
        }

        var flowTaskDetail = await _manager.GetByIdAsync<long, FlowTaskDetail>(order.FlowTaskId.Value);
        if (flowTaskDetail == null || !IsTerminal(flowTaskDetail.Status)) {
            return order;
        }

        return await _manager.UpdateAsync<int, InboundOrder>(order.Id, entity => {
            entity.Status = MapOrderStatus(flowTaskDetail.Status);
            entity.UpdatedTime = DateTimeOffset.UtcNow;
            entity.CompletedTime = flowTaskDetail.FinishedTime;
        });
    }

    private async Task<OutboundOrder> SyncTerminalOutboundOrderStatusAsync(OutboundOrder order) {
        if (!order.FlowTaskId.HasValue) {
            return order;
        }

        var flowTaskDetail = await _manager.GetByIdAsync<long, FlowTaskDetail>(order.FlowTaskId.Value);
        if (flowTaskDetail == null || !IsTerminal(flowTaskDetail.Status)) {
            return order;
        }

        return await _manager.UpdateAsync<int, OutboundOrder>(order.Id, entity => {
            entity.Status = MapOrderStatus(flowTaskDetail.Status);
            entity.UpdatedTime = DateTimeOffset.UtcNow;
            entity.CompletedTime = flowTaskDetail.FinishedTime;
        });
    }

    private async Task<(FlowDefinition definition, FlowVersion version, IFlow flow)> ResolveFlowAsync(
        BusinessFlowType businessType,
        string? explicitFlowDefinitionCode) {
        var flowDefinitionCode = explicitFlowDefinitionCode;
        if (string.IsNullOrWhiteSpace(flowDefinitionCode)) {
            var binding = (await _manager.GetAsync<int, FlowBinding>(
                search: bindings => bindings.Where(entity => entity.BusinessType == businessType && entity.Enabled)))
                .SingleOrDefault()
                ?? throw new ArgumentException($"Enabled flow binding for {businessType} not found.");
            flowDefinitionCode = binding.FlowDefinitionCode;
        }

        var definition = await _manager.GetByIdAsync<string, FlowDefinition>(flowDefinitionCode)
            ?? throw new ArgumentException($"FlowDefinition-{flowDefinitionCode} not found.");
        if (!definition.ActiveVersionId.HasValue) {
            throw new ArgumentException($"FlowDefinition-{flowDefinitionCode} does not have an active version.");
        }

        var version = await _manager.GetByIdAsync<long, FlowVersion>(definition.ActiveVersionId.Value)
            ?? throw new ArgumentException($"FlowVersion-{definition.ActiveVersionId.Value} not found.");
        var flow = await _flowManager.GetByIdAsync(version.RuntimeFlowId)
            ?? throw new ArgumentException($"Runtime flow {version.RuntimeFlowId} not found.");
        return (definition, version, flow);
    }

    private static bool IsTerminal(ExecutableStatus status) {
        return status is ExecutableStatus.Completed or ExecutableStatus.Failed or ExecutableStatus.Canceled;
    }

    private static OrderStatus MapOrderStatus(ExecutableStatus status) {
        return status switch {
            ExecutableStatus.Completed => OrderStatus.Completed,
            ExecutableStatus.Failed => OrderStatus.Failed,
            ExecutableStatus.Canceled => OrderStatus.Canceled,
            _ => OrderStatus.Running,
        };
    }
}
