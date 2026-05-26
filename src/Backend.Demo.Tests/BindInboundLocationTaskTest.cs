using Backend.Demo.DependencyInjection;
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using Backend.Demo.Resource;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.Consoles;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Xunit;

namespace Backend.Demo.Tests;

public sealed class BindInboundLocationTaskTest : IDisposable {
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"backend-demo-bind-inbound-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task ExecuteAsync_CreatesPallet_BindsSku_AndMarksLocationOccupied() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication($"Data Source={_dbPath}");

        await using var provider = services.BuildServiceProvider(true);
        await using var scope = provider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IBackendDemoInitializer>();
        await initializer.InitializeAsync();

        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var console = ActivatorUtilities.CreateInstance<FunctionConsole>(scope.ServiceProvider);

        var sku = (await manager.GetAsync<int, Sku>()).First();
        var location = (await manager.GetAsync<int, Location>()).First(entity => entity.Code == "RACK-A1");
        var inboundPort = (await manager.GetAsync<int, Port>()).First(entity => entity.Code == "IN-PORT-01");
        await manager.UpdateAsync<int, Location>(location.Id, entity => {
            entity.Acquired = true;
            entity.Status = LocationStatus.Empty;
            entity.CurrentPalletId = null;
        });
        await manager.UpdateAsync<int, Port>(inboundPort.Id, entity => {
            entity.Acquired = true;
            entity.Status = PortStatus.Occupied;
            entity.CurrentPalletId = null;
        });

        var task = new BindInboundLocationTask {
            Console = console,
            Status = ExecutableStatus.Scheduled,
            OrderCode = "IN-UT-1001",
            InboundPortId = inboundPort.Id,
            SkuId = sku.Id,
            SkuCode = sku.Code,
            TargetLocationId = location.Id,
            TargetLocationCode = location.Code
        };

        await task.ExecuteAsync();
        Assert.True(task.Status == ExecutableStatus.Completed, task.ErrorMessage ?? "Task did not complete.");

        var refreshedLocation = await manager.GetByIdAsync<int, Location>(location.Id);
        var refreshedPort = await manager.GetByIdAsync<int, Port>(inboundPort.Id);
        var pallet = await manager.GetByIdAsync<int, Pallet>(task.InboundPalletId);

        Assert.NotNull(refreshedLocation);
        Assert.Equal(LocationStatus.Occupied, refreshedLocation!.Status);
        Assert.Equal(task.InboundPalletId, refreshedLocation.CurrentPalletId);
        Assert.NotNull(refreshedPort);
        Assert.Equal(PortStatus.Idle, refreshedPort!.Status);
        Assert.Null(refreshedPort.CurrentPalletId);
        Assert.NotNull(pallet);
        Assert.Equal($"PLT-{task.OrderCode}", pallet!.Code);
        Assert.Equal(sku.Id, pallet.SkuId);
        Assert.Equal("Inbound pallet bound to target rack.", task.CompletionStatus);
    }

    public void Dispose() {
        if (File.Exists(_dbPath)) {
            File.Delete(_dbPath);
        }
    }
}
