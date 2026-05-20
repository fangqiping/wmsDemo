using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Backend.Demo.DependencyInjection;
using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowEngine.Data.EntityFramework.Storage;
using FlowEngine.Execution;
using FlowEngine.Execution.Design;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task InitializeAsync_CreatesMigrationHistoryForSqlite() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication($"Data Source={_dbPath}");

        await using var provider = services.BuildServiceProvider(true);
        await using var scope = provider.CreateAsyncScope();

        var initializer = scope.ServiceProvider.GetRequiredService<IBackendDemoInitializer>();
        await initializer.InitializeAsync();

        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "select count(*) from sqlite_master where type = 'table' and name = '__EFMigrationsHistory';";

        var result = (long)(await command.ExecuteScalarAsync() ?? 0L);
        Assert.Equal(1L, result);
    }

    [Fact]
    public async Task InitializeAsync_RecreatesLegacyEnsureCreatedSqliteDatabase() {
        var setupServices = new ServiceCollection();
        setupServices.AddLogging();
        setupServices.AddBackendDemoApplication($"Data Source={_dbPath}");

        await using (var setupProvider = setupServices.BuildServiceProvider(true))
        await using (var setupScope = setupProvider.CreateAsyncScope()) {
            var dbContext = (DataDbContext)setupScope.ServiceProvider.GetRequiredService<DbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication($"Data Source={_dbPath}");

        await using var provider = services.BuildServiceProvider(true);
        await using var scope = provider.CreateAsyncScope();

        var initializer = scope.ServiceProvider.GetRequiredService<IBackendDemoInitializer>();
        await initializer.InitializeAsync();

        await using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "select count(*) from __EFMigrationsHistory;";

        var result = Convert.ToInt64(await command.ExecuteScalarAsync() ?? 0L);
        Assert.True(result >= 1L);
    }

    public void Dispose() {
        if (File.Exists(_dbPath)) {
            File.Delete(_dbPath);
        }
    }
}
