using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.FlowEngine;
using Microsoft.EntityFrameworkCore;

namespace Backend.Demo.Watchers;

[Watcher]
public sealed class FlowTaskOrderStatusWatcher : WatcherBase<FlowTaskDetail> {
    private readonly IManagerFactory _managerFactory;

    public FlowTaskOrderStatusWatcher(IManagerFactory managerFactory) {
        _managerFactory = managerFactory;
    }

    public override async Task OnUpdatedAsync(FlowTaskDetail from, FlowTaskDetail to, IReader reader) {
        if (from.Status == to.Status) {
            return;
        }

        var inbound = await reader.Get<InboundOrder>()
            .Where(order => order.FlowTaskId == to.Id)
            .SingleOrDefaultAsync();
        if (inbound != null) {
            using var scopedManager = _managerFactory.Create();
            await scopedManager.Service.UpdateAsync<int, InboundOrder>(inbound.Id, entity => ApplyStatus(entity, to));
        }

        var outbound = await reader.Get<OutboundOrder>()
            .Where(order => order.FlowTaskId == to.Id)
            .SingleOrDefaultAsync();
        if (outbound != null) {
            using var scopedManager = _managerFactory.Create();
            await scopedManager.Service.UpdateAsync<int, OutboundOrder>(outbound.Id, entity => ApplyStatus(entity, to));
        }
    }

    private static void ApplyStatus(InboundOrder order, FlowTaskDetail flowTask) {
        order.Status = MapStatus(flowTask.Status);
        order.UpdatedTime = DateTimeOffset.UtcNow;
        order.CompletedTime = ShouldSetCompletedTime(flowTask.Status) ? flowTask.FinishedTime : null;
    }

    private static void ApplyStatus(OutboundOrder order, FlowTaskDetail flowTask) {
        order.Status = MapStatus(flowTask.Status);
        order.UpdatedTime = DateTimeOffset.UtcNow;
        order.CompletedTime = ShouldSetCompletedTime(flowTask.Status) ? flowTask.FinishedTime : null;
    }

    private static OrderStatus MapStatus(ExecutableStatus executableStatus) {
        return executableStatus switch {
            ExecutableStatus.Completed => OrderStatus.Completed,
            ExecutableStatus.Failed => OrderStatus.Failed,
            ExecutableStatus.Canceled => OrderStatus.Canceled,
            _ => OrderStatus.Running
        };
    }

    private static bool ShouldSetCompletedTime(ExecutableStatus executableStatus) {
        return executableStatus is ExecutableStatus.Completed or ExecutableStatus.Failed or ExecutableStatus.Canceled;
    }
}
