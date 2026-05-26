using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Execution.Resource;

namespace Backend.Demo.Resource;

[Rule(AcquireInboundPortTask.RULE_NAME)]
public sealed class InboundPortRule : IRule<int, Port, InboundPortRequest> {
    public async Task ApplyAsync(
        IResourceContext<int, Port, InboundPortRequest> context,
        IEnumerable<InboundPortRequest> requests) {
        foreach (var request in requests) {
            var selected = (await context.Manager.GetAsync(
                search: ports => ports.Where(port =>
                    port.Enabled
                    && !port.Acquired
                    && port.PortType == PortType.Inbound
                    && port.Status == PortStatus.Idle
                    && port.WarehouseId == request.WarehouseId),
                sort: ports => ports.OrderBy(port => port.Id)))
                .FirstOrDefault();

            if (selected != null) {
                context.Succeed(request, selected.Id);
            }
        }
    }
}

[Rule(AcquireOutboundPortTask.RULE_NAME)]
public sealed class OutboundPortRule : IRule<int, Port, OutboundPortRequest> {
    public async Task ApplyAsync(
        IResourceContext<int, Port, OutboundPortRequest> context,
        IEnumerable<OutboundPortRequest> requests) {
        foreach (var request in requests) {
            var selected = (await context.Manager.GetAsync(
                search: ports => ports.Where(port =>
                    port.Enabled
                    && !port.Acquired
                    && port.PortType == PortType.Outbound
                    && port.Status == PortStatus.Idle
                    && port.WarehouseId == request.WarehouseId),
                sort: ports => ports.OrderBy(port => port.Id)))
                .FirstOrDefault();

            if (selected != null) {
                context.Succeed(request, selected.Id);
            }
        }
    }
}
