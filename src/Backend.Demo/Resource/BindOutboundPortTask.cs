using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution.Consoles;

namespace Backend.Demo.Resource;

public sealed class BindOutboundPortTask : OperationTask<FunctionConsole> {
    [Input]
    public int SourceLocationId { get; set; }

    [Input]
    public int SourcePalletId { get; set; }

    [Input]
    public int OutboundPortId { get; set; }

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var managerFactory = console.ServiceProvider.GetRequiredService<IManagerFactory>();
        using var scopedManager = managerFactory.Create();

        await scopedManager.Service.UpdateAsync<int, Location>(SourceLocationId, entity => {
            entity.Status = LocationStatus.Empty;
            entity.CurrentPalletId = null;
        });
        await scopedManager.Service.UpdateAsync<int, Port>(OutboundPortId, entity => {
            entity.Status = PortStatus.Occupied;
            entity.CurrentPalletId = SourcePalletId;
        });
    }
}
