using Backend.Demo.DependencyInjection;
using Backend.Demo.Resource;
using FlowEngine.Execution;
using FlowEngine.Execution.Resource;
using FlowEngine.Execution.Consoles;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Xunit;

namespace Backend.Demo.Tests;

public sealed class PortResourceRulesTest : IDisposable {
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"backend-demo-port-rules-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task AcquireInboundPortTask_ResolvesIdleInboundPort() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication($"Data Source={_dbPath}");

        await using var provider = services.BuildServiceProvider(true);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IBackendDemoInitializer>().InitializeAsync();

        var resourceManagerProvider = scope.ServiceProvider.GetRequiredService<IResourceManagerProvider>();
        var console = ActivatorUtilities.CreateInstance<FunctionConsole>(scope.ServiceProvider);
        await resourceManagerProvider.StartAsync(CancellationToken.None);
        await console.StartAsync(CancellationToken.None);
        try {
            var task = new AcquireInboundPortTask {
                Console = console,
                Status = ExecutableStatus.Scheduled,
                WarehouseId = 1
            };

            await task.ExecuteAsync();

            Assert.Equal("IN-PORT-01", task.InboundPortCode);
            Assert.True(task.InboundPortId > 0);
        } finally {
            await console.StopAsync(CancellationToken.None);
            await resourceManagerProvider.StopAsync(CancellationToken.None);
        }
    }

    public void Dispose() {
        if (File.Exists(_dbPath)) {
            File.Delete(_dbPath);
        }
    }
}
