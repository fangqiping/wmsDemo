using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution.Consoles;

namespace Backend.Demo.Resource;

public sealed class OccupyInboundPortTask : OperationTask<FunctionConsole> {
    [Input]
    public int InboundPortId { get; set; }

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var managerFactory = console.ServiceProvider.GetRequiredService<IManagerFactory>();
        using var scopedManager = managerFactory.Create();
        await scopedManager.Service.UpdateAsync<int, Port>(InboundPortId, entity => {
            entity.Status = PortStatus.Occupied;
            entity.CurrentPalletId = null;
        });
    }
}
