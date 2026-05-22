using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowEngine.Execution.Consoles;
using Microsoft.EntityFrameworkCore;

namespace Backend.Demo.Resource;

public sealed class LoadLocationSnapshotOperationTask : OperationTask<FunctionConsole> {
    [Input]
    public int LocationId { get; set; }

    [Output]
    public string LocationCode { get; set; } = string.Empty;

    [Output]
    public int CurrentPalletId { get; set; }

    [Output]
    public string CurrentPalletCode { get; set; } = string.Empty;

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var managerFactory = console.ServiceProvider.GetRequiredService<IManagerFactory>();
        using var scopedManager = managerFactory.Create();
        var location = await scopedManager.Service.GetByIdAsync<int, Location>(
            LocationId,
            include: query => query.Include(entity => entity.CurrentPallet));
        if (location == null) {
            throw new InvalidOperationException($"Location-{LocationId} not found.");
        }

        LocationCode = location.Code;
        CurrentPalletId = location.CurrentPalletId ?? 0;
        CurrentPalletCode = location.CurrentPallet?.Code ?? string.Empty;
    }
}
