using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Net.Http.Headers;
using Backend.Demo.Contracts.Orders;
using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.FlowEngine;
using FlowEngine.Server.WebApi;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Demo.Tests;

public sealed class BackendDemoApiSmokeTest : IAsyncLifetime {
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"backend-demo-api-{Guid.NewGuid():N}.db");
    private readonly string _defaultDbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Backend.Demo/backend-demo.db"));
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

    [Fact]
    public async Task OutboundOrderStartFlow_ThroughHttp_ProducesFlowTask() {
        await using var scope = _factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var sku = (await manager.GetAsync<int, Sku>()).First();
        var sourceLocation = (await manager.GetAsync<int, Location>()).First(location => location.Code == "RACK-A1");

        var createResponse = await _client.PostAsJsonAsync("/api/OutboundOrders", new OutboundOrderModel {
            Code = "OUT-1001",
            Destination = "OUT-01",
            Remark = "Smoke test",
            Lines = new List<OutboundOrderLineModel> {
                new() {
                    SkuId = sku.Id,
                    Quantity = 1,
                    SourceLocationId = sourceLocation.Id
                }
            }
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<OutboundOrderModel>();
        Assert.NotNull(created);

        var startResponse = await _client.PostAsync($"/api/OutboundOrders/{created!.Id}/start-flow", null);
        startResponse.EnsureSuccessStatusCode();
        var started = await startResponse.Content.ReadFromJsonAsync<OutboundOrderModel>();
        Assert.NotNull(started);
        Assert.NotNull(started!.FlowTaskId);
        Assert.NotNull(started.FlowVersionNumber);

        var flowTask = await manager.GetByIdAsync<long, FlowTaskDetail>(started.FlowTaskId!.Value);
        Assert.NotNull(flowTask);
    }

    [Fact]
    public async Task InboundOrderReadById_SyncsStatusFromCompletedFlowTask() {
        await using var scope = _factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var sku = (await manager.GetAsync<int, Sku>()).First();
        var targetLocation = (await manager.GetAsync<int, Location>()).First(location => location.Code == "RACK-A1");

        var createResponse = await _client.PostAsJsonAsync("/api/InboundOrders", new InboundOrderModel {
            Code = "IN-2001",
            Source = "IN-01",
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

        await manager.UpdateAsync<long, FlowTaskDetail>(started!.FlowTaskId!.Value, entity => {
            entity.Status = ExecutableStatus.Completed;
            entity.FinishedTime = DateTimeOffset.UtcNow;
        });

        var readResponse = await _client.GetAsync($"/api/InboundOrders/{started.Id}");
        readResponse.EnsureSuccessStatusCode();
        var refreshed = await readResponse.Content.ReadFromJsonAsync<InboundOrderModel>();
        Assert.NotNull(refreshed);
        Assert.Equal((int)Domain.Enums.OrderStatus.Completed, refreshed!.Status);
        Assert.NotNull(refreshed.CompletedTime);
    }

    [Fact]
    public async Task InboundOrderReadList_SyncsStatusFromCompletedFlowTask() {
        await using var scope = _factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var sku = (await manager.GetAsync<int, Sku>()).First();
        var targetLocation = (await manager.GetAsync<int, Location>()).First(location => location.Code == "RACK-A1");

        var createResponse = await _client.PostAsJsonAsync("/api/InboundOrders", new InboundOrderModel {
            Code = "IN-2002",
            Source = "IN-01",
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

        await manager.UpdateAsync<long, FlowTaskDetail>(started!.FlowTaskId!.Value, entity => {
            entity.Status = ExecutableStatus.Completed;
            entity.FinishedTime = DateTimeOffset.UtcNow;
        });

        var listResponse = await _client.GetAsync("/api/InboundOrders?ShouldPaginate=false");
        listResponse.EnsureSuccessStatusCode();
        var content = await listResponse.Content.ReadFromJsonAsync<Content<InboundOrderModel>>();
        Assert.NotNull(content);
        var refreshed = content!.Items.Single(order => order.Id == started.Id);
        Assert.Equal((int)Domain.Enums.OrderStatus.Completed, refreshed.Status);
        Assert.NotNull(refreshed.CompletedTime);
    }

    [Fact]
    public async Task SkusOptionsRequest_AllowsFlowViewCorsPreflight() {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/Skus?ShouldPaginate=false");
        request.Headers.Add(HeaderNames.Origin, "http://127.0.0.1:5173");
        request.Headers.Add(HeaderNames.AccessControlRequestMethod, "GET");
        request.Headers.Add(HeaderNames.AccessControlRequestHeaders, "content-type");

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Equal("http://127.0.0.1:5173", response.Headers.GetValues(HeaderNames.AccessControlAllowOrigin).Single());
    }

    [Fact]
    public async Task FlowCatalog_Get_ThroughHttp_Succeeds() {
        var response = await _client.GetAsync("/api/FlowCatalog");

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("subFlowTemplates", out _));
    }

    [Fact]
    public async Task FlowTask_GetById_ThroughHttp_ReturnsExecutionDetails() {
        await using var scope = _factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var sku = (await manager.GetAsync<int, Sku>()).First();
        var targetLocation = (await manager.GetAsync<int, Location>()).First(location => location.Code == "RACK-A1");

        var createResponse = await _client.PostAsJsonAsync("/api/InboundOrders", new InboundOrderModel {
            Code = "IN-3001",
            Source = "IN-01",
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

        var flowTaskResponse = await _client.GetAsync($"/api/FlowTask/{started!.FlowTaskId}");

        flowTaskResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await flowTaskResponse.Content.ReadAsStringAsync());
        Assert.Equal(started.FlowTaskId, document.RootElement.GetProperty("id").GetInt64());
        Assert.True(document.RootElement.TryGetProperty("executableDetailModels", out _));
    }

    [Fact]
    public async Task InboundOrderStartFlow_EventuallyMarksOrderCompleted_WhenTaskFinishesQuickly() {
        await using var scope = _factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var sku = (await manager.GetAsync<int, Sku>()).First();
        var targetLocation = (await manager.GetAsync<int, Location>()).First(location => location.Code == "RACK-A1");

        var createResponse = await _client.PostAsJsonAsync("/api/InboundOrders", new InboundOrderModel {
            Code = "IN-4001",
            Source = "IN-01",
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

        await manager.UpdateAsync<long, FlowTaskDetail>(started!.FlowTaskId!.Value, entity => {
            entity.Status = ExecutableStatus.Completed;
            entity.FinishedTime = DateTimeOffset.UtcNow;
        });

        InboundOrderModel? refreshed = null;
        for (var retry = 0; retry < 10; retry++) {
            var readResponse = await _client.GetAsync($"/api/InboundOrders/{created.Id}");
            readResponse.EnsureSuccessStatusCode();
            refreshed = await readResponse.Content.ReadFromJsonAsync<InboundOrderModel>();
            if (refreshed?.Status == (int)Domain.Enums.OrderStatus.Completed) {
                break;
            }
            await Task.Delay(150);
        }

        Assert.NotNull(refreshed);
        Assert.Equal((int)Domain.Enums.OrderStatus.Completed, refreshed!.Status);
        Assert.NotNull(refreshed.CompletedTime);
    }

    public Task InitializeAsync() {
        if (File.Exists(_defaultDbPath)) {
            File.Delete(_defaultDbPath);
        }
        ConveyorTransferOperationTask.DefaultDelayMilliseconds = 10;
        StackCraneStoreOperationTask.DefaultDelayMilliseconds = 10;
        StackCraneRetrieveOperationTask.DefaultDelayMilliseconds = 10;
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => {
                builder.UseSetting("ConnectionStrings:BackendDemo", $"Data Source={_dbPath}");
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
        if (File.Exists(_defaultDbPath)) {
            File.Delete(_defaultDbPath);
        }
        ConveyorTransferOperationTask.DefaultDelayMilliseconds = 30000;
        StackCraneStoreOperationTask.DefaultDelayMilliseconds = 20000;
        StackCraneRetrieveOperationTask.DefaultDelayMilliseconds = 20000;
    }
}
