using System.Net.Http.Json;
using Backend.Demo.Contracts.Orders;
using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowEngine.Execution.FlowEngine;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Demo.Tests;

public sealed class BackendDemoApiSmokeTest : IAsyncLifetime {
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"backend-demo-api-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program> _factory = default!;
    private HttpClient _client = default!;

    [Fact]
    public async Task InboundOrderStartFlow_ThroughHttp_ProducesFlowTask() {
        await using var scope = _factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var sku = (await manager.GetAsync<int, Sku>()).First();
        var targetLocation = (await manager.GetAsync<int, Location>()).First(location => location.Code == "RACK-A1");

        var createResponse = await _client.PostAsJsonAsync("/api/InboundOrders", new InboundOrderModel {
            Code = "IN-1001",
            Source = "IN-01",
            Remark = "Smoke test",
            Lines = new List<InboundOrderLineModel> {
                new() {
                    SkuId = sku.Id,
                    Quantity = 1,
                    TargetLocationId = targetLocation.Id
                }
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<InboundOrderModel>();
        Assert.NotNull(created);

        var startResponse = await _client.PostAsync($"/api/InboundOrders/{created!.Id}/start-flow", null);
        startResponse.EnsureSuccessStatusCode();
        var started = await startResponse.Content.ReadFromJsonAsync<InboundOrderModel>();
        Assert.NotNull(started);
        Assert.NotNull(started!.FlowTaskId);
        Assert.NotNull(started.FlowVersionNumber);

        var flowTask = await manager.GetByIdAsync<long, FlowTaskDetail>(started.FlowTaskId!.Value);
        Assert.NotNull(flowTask);
    }

    public Task InitializeAsync() {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => {
                builder.ConfigureAppConfiguration((_, config) => {
                    config.AddInMemoryCollection(new Dictionary<string, string?> {
                        ["ConnectionStrings:BackendDemo"] = $"Data Source={_dbPath}"
                    });
                });
            });
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() {
        _client.Dispose();
        await _factory.DisposeAsync();
        if (File.Exists(_dbPath)) {
            File.Delete(_dbPath);
        }
    }
}
