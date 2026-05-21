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
    public async Task OperationTaskCancel_ThroughHttp_ExposesRestartAndSkipActions() {
        using var _ = UseExtendedOperationTaskDelays();
        var flowTaskId = await CreateAndStartInboundOrderAsync("IN-CANCEL-1001");

        var operationTaskId = await WaitForCancelableOperationTaskAsync(flowTaskId);

        var cancelResponse = await _client.PostAsync($"/api/OperationTask/Cancel/{operationTaskId}", null);
        cancelResponse.EnsureSuccessStatusCode();

        var flowTaskResponse = await _client.GetAsync($"/api/FlowTask/{flowTaskId}");
        flowTaskResponse.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await flowTaskResponse.Content.ReadAsStringAsync());
        var canceledNode = document.RootElement.GetProperty("executableDetailModels")
            .EnumerateArray()
            .Single(node => node.GetProperty("id").GetInt64() == operationTaskId);

        Assert.Equal(8, canceledNode.GetProperty("status").GetInt32());
        var actions = canceledNode.GetProperty("availableActions").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("restart", actions);
        Assert.Contains("skip", actions);
    }

    [Fact]
    public async Task OperationTaskRestart_ThroughHttp_AcknowledgesCanceledNodeAndCreatesReplacement() {
        using var _ = UseExtendedOperationTaskDelays();
        var flowTaskId = await CreateAndStartInboundOrderAsync("IN-RESTART-1001");

        var operationTaskId = await WaitForCancelableOperationTaskAsync(flowTaskId);

        var cancelResponse = await _client.PostAsync($"/api/OperationTask/Cancel/{operationTaskId}", null);
        cancelResponse.EnsureSuccessStatusCode();

        var restartResponse = await _client.PostAsync($"/api/OperationTask/Restart/{operationTaskId}", null);
        restartResponse.EnsureSuccessStatusCode();

        var replacementNode = await WaitForReplacementOperationTaskAsync(flowTaskId, operationTaskId);
        Assert.Equal(3, replacementNode.GetProperty("status").GetInt32());

        var oldNode = await GetExecutableNodeAsync(flowTaskId, operationTaskId);
        Assert.True(oldNode.GetProperty("acknowledged").GetBoolean());
        var oldActions = oldNode.GetProperty("availableActions").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Empty(oldActions);

        var newActions = replacementNode.GetProperty("availableActions").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Contains("cancel", newActions);
    }

    [Fact]
    public async Task OperationTaskSkip_ThroughHttp_AcknowledgesCanceledNodeAndAdvancesFlow() {
        using var _ = UseExtendedOperationTaskDelays();
        var flowTaskId = await CreateAndStartInboundOrderAsync("IN-SKIP-1001");

        var operationTaskId = await WaitForCancelableOperationTaskAsync(flowTaskId);

        var cancelResponse = await _client.PostAsync($"/api/OperationTask/Cancel/{operationTaskId}", null);
        cancelResponse.EnsureSuccessStatusCode();

        var skipResponse = await _client.PostAsync($"/api/OperationTask/Skip/{operationTaskId}", null);
        skipResponse.EnsureSuccessStatusCode();

        var skippedNode = await GetExecutableNodeAsync(flowTaskId, operationTaskId);
        Assert.True(skippedNode.GetProperty("acknowledged").GetBoolean());
        var actions = skippedNode.GetProperty("availableActions").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Empty(actions);

        using var document = await WaitForFlowTaskDocumentAsync(flowTaskId, rootStatus: 3, expectedExecutableCountAtLeast: 3);
        var nodeIds = document.RootElement.GetProperty("executableDetailModels")
            .EnumerateArray()
            .Select(node => node.GetProperty("id").GetInt64())
            .ToArray();
        Assert.DoesNotContain(operationTaskId, nodeIds.Where(id => id != operationTaskId));
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

    private IDisposable UseExtendedOperationTaskDelays() {
        var originalConveyorDelay = ConveyorTransferOperationTask.DefaultDelayMilliseconds;
        var originalStoreDelay = StackCraneStoreOperationTask.DefaultDelayMilliseconds;
        var originalRetrieveDelay = StackCraneRetrieveOperationTask.DefaultDelayMilliseconds;
        ConveyorTransferOperationTask.DefaultDelayMilliseconds = 400;
        StackCraneStoreOperationTask.DefaultDelayMilliseconds = 400;
        StackCraneRetrieveOperationTask.DefaultDelayMilliseconds = 400;
        return new CallbackDisposable(() => {
            ConveyorTransferOperationTask.DefaultDelayMilliseconds = originalConveyorDelay;
            StackCraneStoreOperationTask.DefaultDelayMilliseconds = originalStoreDelay;
            StackCraneRetrieveOperationTask.DefaultDelayMilliseconds = originalRetrieveDelay;
        });
    }

    private async Task<long> CreateAndStartInboundOrderAsync(string code) {
        await using var scope = _factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var sku = (await manager.GetAsync<int, Sku>()).First();
        var targetLocation = (await manager.GetAsync<int, Location>()).First(location => location.Code == "RACK-A1");

        var createResponse = await _client.PostAsJsonAsync("/api/InboundOrders", new InboundOrderModel {
            Code = code,
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
        Assert.NotNull(started!.FlowTaskId);
        return started.FlowTaskId.Value;
    }

    private async Task<long> WaitForCancelableOperationTaskAsync(long flowTaskId) {
        for (var retry = 0; retry < 60; retry++) {
            using var document = await GetFlowTaskDocumentAsync(flowTaskId);
            var node = document.RootElement.GetProperty("executableDetailModels")
                .EnumerateArray()
                .FirstOrDefault(item => item.GetProperty("executableType").GetInt32() == 0
                    && item.GetProperty("availableActions").EnumerateArray().Any(action => action.GetString() == "cancel"));
            if (node.ValueKind != JsonValueKind.Undefined) {
                return node.GetProperty("id").GetInt64();
            }
            await Task.Delay(50);
        }

        throw new Xunit.Sdk.XunitException($"Timed out waiting for a cancelable operation task for flow {flowTaskId}.");
    }

    private async Task<JsonElement> WaitForReplacementOperationTaskAsync(long flowTaskId, long originalOperationTaskId) {
        for (var retry = 0; retry < 40; retry++) {
            using var document = await GetFlowTaskDocumentAsync(flowTaskId);
            var node = document.RootElement.GetProperty("executableDetailModels")
                .EnumerateArray()
                .FirstOrDefault(item => item.GetProperty("executableType").GetInt32() == 0
                    && item.GetProperty("id").GetInt64() != originalOperationTaskId
                    && item.GetProperty("status").GetInt32() == 3);
            if (node.ValueKind != JsonValueKind.Undefined) {
                return node.Clone();
            }
            await Task.Delay(50);
        }

        throw new Xunit.Sdk.XunitException($"Timed out waiting for replacement operation task for flow {flowTaskId}.");
    }

    private async Task<JsonElement> GetExecutableNodeAsync(long flowTaskId, long executableId) {
        using var document = await GetFlowTaskDocumentAsync(flowTaskId);
        return document.RootElement.GetProperty("executableDetailModels")
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetInt64() == executableId)
            .Clone();
    }

    private async Task<JsonDocument> WaitForFlowTaskDocumentAsync(long flowTaskId, int rootStatus, int expectedExecutableCountAtLeast) {
        for (var retry = 0; retry < 40; retry++) {
            var document = await GetFlowTaskDocumentAsync(flowTaskId);
            var root = document.RootElement;
            if (root.GetProperty("status").GetInt32() == rootStatus
                && root.GetProperty("executableDetailModels").GetArrayLength() >= expectedExecutableCountAtLeast) {
                return document;
            }
            document.Dispose();
            await Task.Delay(50);
        }

        throw new Xunit.Sdk.XunitException($"Timed out waiting for flow {flowTaskId} root status {rootStatus} and executable count {expectedExecutableCountAtLeast}.");
    }

    private async Task<JsonDocument> GetFlowTaskDocumentAsync(long flowTaskId) {
        var response = await _client.GetAsync($"/api/FlowTask/{flowTaskId}");
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private sealed class CallbackDisposable : IDisposable {
        private readonly Action _callback;
        private bool _disposed;

        public CallbackDisposable(Action callback) {
            _callback = callback;
        }

        public void Dispose() {
            if (_disposed) {
                return;
            }

            _callback();
            _disposed = true;
        }
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
