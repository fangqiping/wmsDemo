using System;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    private const string InboundFlowCode = "inbound-basic";
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

    [Fact]
    public async Task InitializeAsync_PublishesRetryableDemoOperationNodes() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication($"Data Source={_dbPath}");

        await using var provider = services.BuildServiceProvider(true);
        await using var scope = provider.CreateAsyncScope();

        var initializer = scope.ServiceProvider.GetRequiredService<IBackendDemoInitializer>();
        await initializer.InitializeAsync();

        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var definitions = (await manager.GetAsync<string, FlowDefinition>(
            query => query.Where(definition => definition.ActiveVersionId != null).OrderBy(definition => definition.Id)))
            .ToArray();
        var activeVersionIds = definitions.Select(definition => definition.ActiveVersionId!.Value).ToArray();
        var activeVersions = await manager.GetAsync<long, FlowVersion>(
            versions => versions.Where(version => activeVersionIds.Contains(version.Id))
                .OrderBy(version => version.Id));

        Assert.All(activeVersions, version => {
            using var document = JsonDocument.Parse(version.CompiledGraphJson);
            var nodes = document.RootElement.GetProperty("nodes").EnumerateArray()
                .Where(node => node.GetProperty("id").GetString() != "Root")
                .ToArray();
            Assert.NotEmpty(nodes);
            Assert.All(nodes, node => {
                Assert.False(node.GetProperty("shouldThrowOnFailed").GetBoolean());
                Assert.False(node.GetProperty("shouldThrowOnCanceled").GetBoolean());
            });
        });
    }

    [Fact]
    public async Task InitializeAsync_RepublishesDemoFlows_WhenSeedDraftChanges() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication($"Data Source={_dbPath}");

        await using var provider = services.BuildServiceProvider(true);
        await using var scope = provider.CreateAsyncScope();

        var initializer = scope.ServiceProvider.GetRequiredService<IBackendDemoInitializer>();
        await initializer.InitializeAsync();

        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var originalVersions = await manager.GetAsync<long, FlowVersion>(
            versions => versions.Where(version => version.Id > 0).OrderBy(version => version.Id));
        Assert.Equal(2, originalVersions.Count());

        await manager.UpdateAsync<string, FlowDraft>(InboundFlowCode, draft => {
            draft.DraftDocumentJson = draft.DraftDocumentJson
                .Replace("\"shouldThrowOnFailed\":false", "\"shouldThrowOnFailed\":true", StringComparison.Ordinal)
                .Replace("\"shouldThrowOnCanceled\":false", "\"shouldThrowOnCanceled\":true", StringComparison.Ordinal);
        });

        await initializer.InitializeAsync();

        var inboundDefinition = await manager.GetByIdAsync<string, FlowDefinition>(InboundFlowCode);
        Assert.NotNull(inboundDefinition);
        var inboundVersions = await manager.GetAsync<long, FlowVersion>(
            versions => versions.Where(version => version.FlowDefinitionId == InboundFlowCode).OrderBy(version => version.VersionNumber));
        Assert.Equal(2, inboundVersions.Count());
        var latestInbound = inboundVersions.Last();
        Assert.Equal(inboundDefinition!.ActiveVersionId, latestInbound.Id);

        using var document = JsonDocument.Parse(latestInbound.CompiledGraphJson);
        var nodes = document.RootElement.GetProperty("nodes").EnumerateArray()
            .Where(node => node.GetProperty("id").GetString() != "Root")
            .ToArray();
        Assert.All(nodes, node => {
            Assert.False(node.GetProperty("shouldThrowOnFailed").GetBoolean());
            Assert.False(node.GetProperty("shouldThrowOnCanceled").GetBoolean());
        });
    }

    public void Dispose() {
        if (File.Exists(_dbPath)) {
            File.Delete(_dbPath);
        }
    }
}
