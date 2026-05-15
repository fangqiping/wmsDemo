using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Backend.Demo.DependencyInjection;
using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.Design;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Demo.Tests;

public sealed class BackendDemoInitializationTest : IDisposable {
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"backend-demo-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task InitializeAsync_SeedsMasterDataConsolesAndPublishedFlows() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication($"Data Source={_dbPath}");

        await using var provider = services.BuildServiceProvider(true);
        await using var scope = provider.CreateAsyncScope();

        var initializer = scope.ServiceProvider.GetRequiredService<IBackendDemoInitializer>();
        await initializer.InitializeAsync();

        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var consoleProvider = scope.ServiceProvider.GetRequiredService<IConsoleProvider>();

        Assert.True(await manager.AnyAsync<int, Warehouse>());
        Assert.True(await manager.AnyAsync<int, Location>());
        Assert.True(await manager.AnyAsync<int, Sku>());
        Assert.Equal(2, await manager.CountAsync<int, FlowBinding>());
        Assert.Equal(2, await manager.CountAsync<string, FlowDefinition>());
        Assert.Equal(2, await manager.CountAsync<long, FlowVersion>());

        var definitions = await manager.GetAsync<string, FlowDefinition>(sort: flows => flows.OrderBy(flow => flow.Id));
        Assert.All(definitions, definition => Assert.Equal(FlowDefinitionStatus.Active, definition.Status));

        var consoleIds = consoleProvider.GetAll().Select(console => console.Id).OrderBy(id => id).ToArray();
        Assert.Contains("ConveyorConsole", consoleIds);
        Assert.Contains("StackCraneConsole", consoleIds);
    }

    public void Dispose() {
        if (File.Exists(_dbPath)) {
            File.Delete(_dbPath);
        }
    }
}
