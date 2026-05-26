using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution.Consoles;

namespace Backend.Demo.Resource;

public sealed class ReleaseOutboundPortTask : OperationTask<FunctionConsole> {
    [Input]
    public int OutboundPortId { get; set; }

    [Input]
    public int SourcePalletId { get; set; }

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var managerFactory = console.ServiceProvider.GetRequiredService<IManagerFactory>();
        using var scopedManager = managerFactory.Create();

        await scopedManager.Service.UpdateAsync<int, Port>(OutboundPortId, entity => {
            entity.Status = PortStatus.Idle;
            entity.CurrentPalletId = null;
        });
        await scopedManager.Service.UpdateAsync<int, Pallet>(SourcePalletId, entity => {
            entity.Enabled = false;
        });
    }
}
