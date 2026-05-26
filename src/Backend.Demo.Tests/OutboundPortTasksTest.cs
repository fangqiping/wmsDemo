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

public sealed class OutboundPortTasksTest : IDisposable {
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"backend-demo-bind-outbound-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task ExecuteAsync_MovesPalletFromRackToOutboundPort_ThenReleasesPort() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication($"Data Source={_dbPath}");

        await using var provider = services.BuildServiceProvider(true);
        await using var scope = provider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IBackendDemoInitializer>();
        await initializer.InitializeAsync();

        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var console = ActivatorUtilities.CreateInstance<FunctionConsole>(scope.ServiceProvider);

        var location = (await manager.GetAsync<int, Location>()).First(entity => entity.Code == "RACK-A2");
        var outboundPort = (await manager.GetAsync<int, Port>()).First(entity => entity.Code == "OUT-PORT-01");
        Assert.NotNull(location.CurrentPalletId);

        var bindTask = new BindOutboundPortTask {
            Console = console,
            Status = ExecutableStatus.Scheduled,
            SourceLocationId = location.Id,
            SourcePalletId = location.CurrentPalletId!.Value,
            OutboundPortId = outboundPort.Id
        };

        await bindTask.ExecuteAsync();
        Assert.Equal(ExecutableStatus.Completed, bindTask.Status);

        var boundLocation = await manager.GetByIdAsync<int, Location>(location.Id);
        var boundPort = await manager.GetByIdAsync<int, Port>(outboundPort.Id);
        Assert.Equal(LocationStatus.Empty, boundLocation!.Status);
        Assert.Null(boundLocation.CurrentPalletId);
        Assert.Equal(PortStatus.Occupied, boundPort!.Status);
        Assert.Equal(bindTask.SourcePalletId, boundPort.CurrentPalletId);

        var releaseTask = new ReleaseOutboundPortTask {
            Console = console,
            Status = ExecutableStatus.Scheduled,
            OutboundPortId = outboundPort.Id,
            SourcePalletId = bindTask.SourcePalletId
        };

        await releaseTask.ExecuteAsync();
        Assert.Equal(ExecutableStatus.Completed, releaseTask.Status);

        var releasedPort = await manager.GetByIdAsync<int, Port>(outboundPort.Id);
        var releasedPallet = await manager.GetByIdAsync<int, Pallet>(bindTask.SourcePalletId);
        Assert.Equal(PortStatus.Idle, releasedPort!.Status);
        Assert.Null(releasedPort.CurrentPalletId);
        Assert.NotNull(releasedPallet);
        Assert.False(releasedPallet!.Enabled);
    }

    public void Dispose() {
        if (File.Exists(_dbPath)) {
            File.Delete(_dbPath);
        }
    }
}
