using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution.Consoles;

namespace Backend.Demo.Resource;

public sealed class BindInboundLocationTask : OperationTask<FunctionConsole> {
    [Input]
    public string OrderCode { get; set; } = string.Empty;

    [Input]
    public int InboundPortId { get; set; }

    [Input]
    public int SkuId { get; set; }

    [Input]
    public string SkuCode { get; set; } = string.Empty;

    [Input]
    public int TargetLocationId { get; set; }

    [Input]
    public string TargetLocationCode { get; set; } = string.Empty;

    [Output]
    public int InboundPalletId { get; set; }

    [Output]
    public string InboundPalletCode { get; set; } = string.Empty;

    [Output]
    public string CompletionStatus { get; set; } = string.Empty;

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var managerFactory = console.ServiceProvider.GetRequiredService<IManagerFactory>();
        using var scopedManager = managerFactory.Create();
        var locationExists = await scopedManager.Service.AnyAsync<int, Location>(
            search: query => query.Where(entity => entity.Id == TargetLocationId));
        if (!locationExists) {
            throw new InvalidOperationException($"Location-{TargetLocationId} not found.");
        }

        InboundPalletCode = $"PLT-{OrderCode}";
        var pallet = await scopedManager.Service.AddAsync<int, Pallet>(new Pallet {
            Code = InboundPalletCode,
            Enabled = true,
            Acquired = false,
            SkuId = SkuId,
            Quantity = 1
        });
        await scopedManager.Service.UpdateAsync<int, Location>(TargetLocationId, entity => {
            entity.Status = LocationStatus.Occupied;
            entity.CurrentPalletId = pallet.Id;
        });
        await scopedManager.Service.UpdateAsync<int, Port>(InboundPortId, entity => {
            entity.Status = PortStatus.Idle;
            entity.CurrentPalletId = null;
        });

        InboundPalletId = pallet.Id;
        CompletionStatus = "Inbound pallet bound to target rack.";
    }
}
