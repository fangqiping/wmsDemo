using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.FlowEngine;
using Microsoft.EntityFrameworkCore;

namespace Backend.Demo.Watchers;

[Watcher]
public sealed class OutboundOrderFlowTaskLinkWatcher : WatcherBase<OutboundOrder> {
    private readonly IManagerFactory _managerFactory;

    public OutboundOrderFlowTaskLinkWatcher(IManagerFactory managerFactory) {
        _managerFactory = managerFactory;
    }

    public override async Task OnUpdatedAsync(OutboundOrder from, OutboundOrder to, IReader reader) {
        if (from.FlowTaskId == to.FlowTaskId || !to.FlowTaskId.HasValue) {
            return;
        }

        var flowTask = await reader.Get<FlowTaskDetail>()
            .Where(detail => detail.Id == to.FlowTaskId.Value)
            .SingleOrDefaultAsync();
        if (flowTask == null || !IsTerminal(flowTask.Status)) {
            return;
        }

        using var scopedManager = _managerFactory.Create();
        await scopedManager.Service.UpdateAsync<int, OutboundOrder>(to.Id, entity => {
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
