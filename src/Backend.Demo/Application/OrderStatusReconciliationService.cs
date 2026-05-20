using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.FlowEngine;
using FlowEngine.Utils;

namespace Backend.Demo.Application;

public sealed class OrderStatusReconciliationService : HostedService {
    private readonly IManagerFactory _managerFactory;

    public OrderStatusReconciliationService(
        ILogger<HostedService> logger,
        IManagerFactory managerFactory) : base(logger) {
        _managerFactory = managerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            await ReconcileAsync();
            await Task.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
        }
    }

    private async Task ReconcileAsync() {
        using var manager = _managerFactory.Create();

        var inboundOrders = await manager.Service.GetAsync<int, InboundOrder>(
            search: orders => orders.Where(order =>
                order.FlowTaskId != null &&
                order.Status == OrderStatus.Running));
        foreach (var order in inboundOrders) {
            await ReconcileInboundAsync(manager.Service, order);
        }

        var outboundOrders = await manager.Service.GetAsync<int, OutboundOrder>(
            search: orders => orders.Where(order =>
                order.FlowTaskId != null &&
                order.Status == OrderStatus.Running));
        foreach (var order in outboundOrders) {
            await ReconcileOutboundAsync(manager.Service, order);
        }
    }

    private static async Task ReconcileInboundAsync(IManager manager, InboundOrder order) {
        var flowTask = await manager.GetByIdAsync<long, FlowTaskDetail>(order.FlowTaskId!.Value);
        if (flowTask == null || !IsTerminal(flowTask.Status)) {
            return;
        }

        await manager.UpdateAsync<int, InboundOrder>(order.Id, entity => {
            entity.Status = MapStatus(flowTask.Status);
            entity.UpdatedTime = DateTimeOffset.UtcNow;
            entity.CompletedTime = flowTask.FinishedTime;
        });
    }

    private static async Task ReconcileOutboundAsync(IManager manager, OutboundOrder order) {
        var flowTask = await manager.GetByIdAsync<long, FlowTaskDetail>(order.FlowTaskId!.Value);
        if (flowTask == null || !IsTerminal(flowTask.Status)) {
            return;
        }

        await manager.UpdateAsync<int, OutboundOrder>(order.Id, entity => {
            entity.Status = MapStatus(flowTask.Status);
            entity.UpdatedTime = DateTimeOffset.UtcNow;
            entity.CompletedTime = flowTask.FinishedTime;
        });
    }

    private static bool IsTerminal(ExecutableStatus status) {
        return status is ExecutableStatus.Completed or ExecutableStatus.Failed or ExecutableStatus.Canceled;
    }

    private static OrderStatus MapStatus(ExecutableStatus status) {
        return status switch {
            ExecutableStatus.Completed => OrderStatus.Completed,
            ExecutableStatus.Failed => OrderStatus.Failed,
            ExecutableStatus.Canceled => OrderStatus.Canceled,
            _ => OrderStatus.Running,
        };
    }
}
