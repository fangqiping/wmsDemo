# Backend Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a SQLite-backed backend demo that consumes local `FlowEngine` preview NuGet packages, defines inbound and outbound WMS/WCS entities, exposes CRUD and business APIs, seeds simple inbound and outbound flows, and executes those flows through simulated stack-crane and conveyor consoles.

**Architecture:** Keep `Backend.Demo` thin and let `FlowEngine` carry the heavy base layers: data registration, managers, generic CRUD controllers, flow publishing, flow runtime, and execution orchestration. The backend adds domain entities, contract models, business-specific controllers and actions, seed data, and simulated consoles that run delayed `OperationTask` instances.

**Tech Stack:** .NET 10, ASP.NET Core Web API, SQLite, Swagger, AutoMapper, local NuGet source, FlowEngine preview packages, xUnit

---

### Task 1: Bootstrap The Demo Workspace And Local Package Consumption

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/NuGet.config`
- Create: `/Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Backend.Demo.csproj`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Contracts/Backend.Demo.Contracts.csproj`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.SampleData/Backend.Demo.SampleData.csproj`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Program.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/README.md`

- [ ] **Step 1: Initialize the backend repository and write the failing package-resolution smoke check**

Create the workspace and a tiny smoke test file:

```bash
mkdir -p /Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo
mkdir -p /Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Contracts
mkdir -p /Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.SampleData
git -C /Users/qiping/Desktop/codes/work/Backend init
```

Create `/Users/qiping/Desktop/codes/work/Backend/README.md` with:

```md
# Backend Demo

WMS/WCS backend demo powered by FlowEngine preview packages.
```

- [ ] **Step 2: Add the local NuGet source and project package references**

Create `/Users/qiping/Desktop/codes/work/Backend/NuGet.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-flowengine" value="/private/tmp/flowengine-pack-verify" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Backend.Demo.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FlowEngine" Version="0.1.0-preview.1" />
    <PackageReference Include="FlowEngine.Execution" Version="0.1.0-preview.1" />
    <PackageReference Include="FlowEngine.Server" Version="0.1.0-preview.1" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="10.1.7" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Backend.Demo.Contracts\Backend.Demo.Contracts.csproj" />
    <ProjectReference Include="..\Backend.Demo.SampleData\Backend.Demo.SampleData.csproj" />
  </ItemGroup>
</Project>
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Contracts/Backend.Demo.Contracts.csproj` and `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.SampleData/Backend.Demo.SampleData.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FlowEngine" Version="0.1.0-preview.1" />
    <PackageReference Include="FlowEngine.Execution" Version="0.1.0-preview.1" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create the solution and the minimal host**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
```

Run:

```bash
dotnet new sln -n Backend.Demo -o /Users/qiping/Desktop/codes/work/Backend
dotnet sln /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln add \
  /Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Backend.Demo.csproj \
  /Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Contracts/Backend.Demo.Contracts.csproj \
  /Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.SampleData/Backend.Demo.SampleData.csproj
```

- [ ] **Step 4: Run restore and verify package consumption**

Run:

```bash
dotnet restore /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln -v minimal
dotnet build /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln -v minimal
```

Expected: restore and build succeed while resolving `FlowEngine.* 0.1.0-preview.1` from `/private/tmp/flowengine-pack-verify`.

- [ ] **Step 5: Commit the bootstrap**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend add .
git -C /Users/qiping/Desktop/codes/work/Backend commit -m "chore: bootstrap backend demo workspace"
```

### Task 2: Add Domain Entities, Enums, And Contracts

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Warehouse.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Location.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Sku.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/InboundOrder.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/InboundOrderLine.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/OutboundOrder.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/OutboundOrderLine.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/FlowBinding.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Enums/OrderStatus.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Enums/BusinessFlowType.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Enums/LocationType.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Contracts/Orders/InboundOrderModels.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Contracts/Orders/OutboundOrderModels.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Contracts/MasterData/WarehouseModels.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Contracts/MasterData/LocationModels.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Contracts/MasterData/SkuModels.cs`

- [ ] **Step 1: Write a failing entity-registration smoke test**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Tests/EntityRegistrationTest.cs`:

```csharp
[Fact]
public void DomainEntities_AreDiscoverableByFlowEngineDataLayer() {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddBackendDemoData("Data Source=:memory:");

    using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();

    scope.ServiceProvider.GetRequiredService<IManager<int, Warehouse>>();
    scope.ServiceProvider.GetRequiredService<IManager<int, Location>>();
    scope.ServiceProvider.GetRequiredService<IManager<int, Sku>>();
    scope.ServiceProvider.GetRequiredService<IManager<int, InboundOrder>>();
    scope.ServiceProvider.GetRequiredService<IManager<int, OutboundOrder>>();
    scope.ServiceProvider.GetRequiredService<IManager<int, FlowBinding>>();
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```bash
dotnet test /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln --filter "FullyQualifiedName~EntityRegistrationTest" -v minimal
```

Expected: fail because `AddBackendDemoData` and the entity types do not exist yet.

- [ ] **Step 3: Define the domain model**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Enums/OrderStatus.cs`:

```csharp
namespace Backend.Demo.Domain.Enums;

public enum OrderStatus {
    Draft = 0,
    Submitted = 1,
    Running = 2,
    Completed = 3,
    Canceled = 4,
    Failed = 5,
}
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Enums/BusinessFlowType.cs`:

```csharp
namespace Backend.Demo.Domain.Enums;

public enum BusinessFlowType {
    InboundOrder = 0,
    OutboundOrder = 1,
}
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Enums/LocationType.cs`:

```csharp
namespace Backend.Demo.Domain.Enums;

public enum LocationType {
    InboundStation = 0,
    Rack = 1,
    OutboundStation = 2,
}
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Warehouse.cs`:

```csharp
using FlowEngine.Data;

namespace Backend.Demo.Domain;

public class Warehouse : IEntity<int> {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<Location> Locations { get; set; } = new();
}
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Location.cs`:

```csharp
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;

namespace Backend.Demo.Domain;

public class Location : IEntity<int> {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public LocationType LocationType { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
}
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/Sku.cs`:

```csharp
using FlowEngine.Data;

namespace Backend.Demo.Domain;

public class Sku : IEntity<int> {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Spec { get; set; } = string.Empty;
}
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/InboundOrder.cs` and `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Domain/OutboundOrder.cs` following this shape:

```csharp
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;

namespace Backend.Demo.Domain;

public class InboundOrder : IEntity<int> {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? FlowDefinitionCode { get; set; }
    public int? FlowVersionNumber { get; set; }
    public long? FlowTaskId { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
    public DateTimeOffset? CompletedTime { get; set; }
    public List<InboundOrderLine> Lines { get; set; } = new();
}
```

Use the same pattern for:
- `InboundOrderLine`
- `OutboundOrder`
- `OutboundOrderLine`
- `FlowBinding`

- [ ] **Step 4: Add input and output contract models**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Contracts/Orders/InboundOrderModels.cs`:

```csharp
namespace Backend.Demo.Contracts.Orders;

public sealed class InboundOrderLineModel {
    public int SkuId { get; set; }
    public decimal Quantity { get; set; }
    public int TargetLocationId { get; set; }
}

public sealed class InboundOrderInputModel {
    public string Code { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public List<InboundOrderLineModel> Lines { get; set; } = new();
}

public sealed class InboundOrderOutputModel : InboundOrderInputModel {
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FlowDefinitionCode { get; set; }
    public int? FlowVersionNumber { get; set; }
    public long? FlowTaskId { get; set; }
}
```

Mirror the same pattern for outbound, warehouse, location, and sku models in their respective files.

- [ ] **Step 5: Re-run the failing test and verify it still fails for missing DI only**

Run:

```bash
dotnet test /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln --filter "FullyQualifiedName~EntityRegistrationTest" -v minimal
```

Expected: fail because data registration is not wired yet, but the project now compiles with the domain types present.

- [ ] **Step 6: Commit the domain model**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend add .
git -C /Users/qiping/Desktop/codes/work/Backend commit -m "feat: add backend demo domain model"
```

### Task 3: Wire FlowEngine Data, AutoMapper, And Host Startup

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/DependencyInjection/BackendDemoDataServiceCollectionExtensions.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/DependencyInjection/BackendDemoServiceCollectionExtensions.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Mapping/OrderMappingProfile.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Program.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Tests/HostStartupTest.cs`

- [ ] **Step 1: Write the failing host startup test**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Tests/HostStartupTest.cs`:

```csharp
[Fact]
public void BuildHost_RegistersFlowEngineManagersAndControllers() {
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
        EnvironmentName = Environments.Development
    });

    builder.Services.AddBackendDemoApplication("Data Source=:memory:");

    using var app = builder.Build();
    using var scope = app.Services.CreateScope();

    scope.ServiceProvider.GetRequiredService<IManager<int, InboundOrder>>();
    scope.ServiceProvider.GetRequiredService<IFlowPublisher>();
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```bash
dotnet test /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln --filter "FullyQualifiedName~HostStartupTest" -v minimal
```

Expected: fail because `AddBackendDemoApplication` does not exist yet.

- [ ] **Step 3: Add the SQLite and FlowEngine registration**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/DependencyInjection/BackendDemoDataServiceCollectionExtensions.cs`:

```csharp
using System.Reflection;
using FlowEngine.Data.DependencyInjection;
using FlowEngine.Data.EntityFramework.Storage;

namespace Backend.Demo.DependencyInjection;

public static class BackendDemoDataServiceCollectionExtensions {
    public static IServiceCollection AddBackendDemoData(this IServiceCollection services, string connectionString) {
        services.AddDataDbContext(Providers.SQLITE, connectionString, Assembly.GetExecutingAssembly());
        return services;
    }
}
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/DependencyInjection/BackendDemoServiceCollectionExtensions.cs`:

```csharp
using AutoMapper;
using Backend.Demo.Mapping;
using FlowEngine.Execution.DependencyInjection;
using FlowEngine.Server.DependencyInjection;

namespace Backend.Demo.DependencyInjection;

public static class BackendDemoServiceCollectionExtensions {
    public static IServiceCollection AddBackendDemoApplication(this IServiceCollection services, string connectionString) {
        services.AddLogging();
        services.AddBackendDemoData(connectionString);
        services.AddFlowExecution();
        services.AddFlowEngineServer();
        services.AddAutoMapper(cfg => cfg.AddProfile<OrderMappingProfile>());
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }
}
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Mapping/OrderMappingProfile.cs`:

```csharp
using AutoMapper;
using Backend.Demo.Contracts.Orders;
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;

namespace Backend.Demo.Mapping;

public class OrderMappingProfile : Profile {
    public OrderMappingProfile() {
        CreateMap<InboundOrderInputModel, InboundOrder>()
            .ForMember(d => d.Status, c => c.MapFrom(_ => OrderStatus.Draft))
            .ForMember(d => d.CreatedTime, c => c.MapFrom(_ => DateTimeOffset.UtcNow))
            .ForMember(d => d.UpdatedTime, c => c.MapFrom(_ => DateTimeOffset.UtcNow));
        CreateMap<InboundOrder, InboundOrderOutputModel>()
            .ForMember(d => d.Status, c => c.MapFrom(s => s.Status.ToString()));
    }
}
```

- [ ] **Step 4: Update the host entry point**

Replace `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Program.cs` with:

```csharp
using Backend.Demo.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("BackendDemo")
    ?? "Data Source=backend-demo.db";

builder.Services.AddBackendDemoApplication(connectionString);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
```

- [ ] **Step 5: Run the host startup test and verify it passes**

Run:

```bash
dotnet test /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln --filter "FullyQualifiedName~HostStartupTest" -v minimal
```

Expected: pass.

- [ ] **Step 6: Commit the host wiring**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend add .
git -C /Users/qiping/Desktop/codes/work/Backend commit -m "feat: wire backend demo host to FlowEngine"
```

### Task 4: Add Generic CRUD Controllers And Order Business Actions

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Controllers/WarehousesController.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Controllers/LocationsController.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Controllers/SkusController.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Controllers/InboundOrdersController.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Controllers/OutboundOrdersController.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Application/IOrderFlowApplicationService.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Application/OrderFlowApplicationService.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Tests/Controllers/InboundOrdersControllerTest.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Tests/Controllers/OutboundOrdersControllerTest.cs`

- [ ] **Step 1: Write the failing controller tests**

Create tests that assert:

```csharp
[Fact]
public async Task SubmitAsync_UpdatesInboundOrderStatus_ToSubmitted() { }

[Fact]
public async Task StartFlowAsync_PersistsFlowTaskId_OnInboundOrder() { }

[Fact]
public async Task StartFlowAsync_PersistsFlowTaskId_OnOutboundOrder() { }
```

The tests should create orders through `IManager<int, InboundOrder>` or `IManager<int, OutboundOrder>`, call the controller action, and then re-read the entity to verify state changes.

- [ ] **Step 2: Run the tests and verify they fail**

Run:

```bash
dotnet test /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln --filter "FullyQualifiedName~InboundOrdersControllerTest|FullyQualifiedName~OutboundOrdersControllerTest" -v minimal
```

Expected: fail because the controllers and application service do not exist yet.

- [ ] **Step 3: Implement generic CRUD controllers**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Controllers/WarehousesController.cs`:

```csharp
using AutoMapper;
using Backend.Demo.Contracts.MasterData;
using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowEngine.Server.WebApi;

[Route("api/[controller]")]
public sealed class WarehousesController : ApiController<int, Warehouse, WarehouseInputModel, WarehouseOutputModel> {
    public WarehousesController(
        ILogger<ApiController<int, Warehouse, WarehouseInputModel, WarehouseOutputModel>> logger,
        IManager<int, Warehouse> manager,
        IMapper mapper) : base(logger, manager, mapper) { }
}
```

Repeat the same pattern for:
- `LocationsController`
- `SkusController`

- [ ] **Step 4: Implement order business actions**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Application/IOrderFlowApplicationService.cs`:

```csharp
public interface IOrderFlowApplicationService {
    Task SubmitInboundAsync(int orderId);
    Task SubmitOutboundAsync(int orderId);
    Task<long> StartInboundFlowAsync(int orderId);
    Task<long> StartOutboundFlowAsync(int orderId);
}
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Controllers/InboundOrdersController.cs`:

```csharp
[Route("api/[controller]")]
public sealed class InboundOrdersController : ApiController<int, InboundOrder, InboundOrderInputModel, InboundOrderOutputModel> {
    private readonly IOrderFlowApplicationService _applicationService;

    public InboundOrdersController(
        ILogger<ApiController<int, InboundOrder, InboundOrderInputModel, InboundOrderOutputModel>> logger,
        IManager<int, InboundOrder> manager,
        IMapper mapper,
        IOrderFlowApplicationService applicationService) : base(logger, manager, mapper) {
        _applicationService = applicationService;
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitAsync(int id) {
        await _applicationService.SubmitInboundAsync(id);
        return Ok();
    }

    [HttpPost("{id}/start-flow")]
    public async Task<IActionResult> StartFlowAsync(int id) {
        return Ok(await _applicationService.StartInboundFlowAsync(id));
    }
}
```

Mirror the same pattern for `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Controllers/OutboundOrdersController.cs`.

- [ ] **Step 5: Run the controller tests and verify they pass**

Run the same `dotnet test` command from Step 2.

Expected: pass.

- [ ] **Step 6: Commit the API layer**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend add .
git -C /Users/qiping/Desktop/codes/work/Backend commit -m "feat: add backend demo CRUD and order actions"
```

### Task 5: Add Simulated Consoles, Operation Tasks, And Order-To-Flow Mapping

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Equipment/StackCraneConsole.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Equipment/ConveyorConsole.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Equipment/Tasks/PutawayOperationTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Equipment/Tasks/RetrieveOperationTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Equipment/Tasks/ReceiveOperationTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Equipment/Tasks/TransferOperationTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Flows/InboundOrderFlowFactory.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Flows/OutboundOrderFlowFactory.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Application/OrderFlowMapper.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Tests/Flows/InboundFlowExecutionTest.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Tests/Flows/OutboundFlowExecutionTest.cs`

- [ ] **Step 1: Write the failing flow execution tests**

Create inbound and outbound tests that:

```csharp
[Fact]
public async Task StartInboundFlowAsync_RunsReceiveAndPutawayTasks() { }

[Fact]
public async Task StartOutboundFlowAsync_RunsRetrieveAndTransferTasks() { }
```

Each test should:
- create master data
- create an order
- start the flow through `IOrderFlowApplicationService`
- wait for delayed tasks to complete
- assert the order status becomes `Completed`

- [ ] **Step 2: Run the tests and verify they fail**

Run:

```bash
dotnet test /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln --filter "FullyQualifiedName~InboundFlowExecutionTest|FullyQualifiedName~OutboundFlowExecutionTest" -v minimal
```

Expected: fail because the consoles, tasks, and flow factories do not exist yet.

- [ ] **Step 3: Implement delayed operation tasks**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Equipment/Tasks/ReceiveOperationTask.cs`:

```csharp
using FlowEngine.Execution.Consoles;

namespace Backend.Demo.Equipment.Tasks;

public sealed class ReceiveOperationTask : OperationTask {
    public string OrderCode { get; set; } = string.Empty;
    public int DelayMilliseconds { get; set; } = 100;
}
```

Repeat the same shape for:
- `PutawayOperationTask`
- `RetrieveOperationTask`
- `TransferOperationTask`

- [ ] **Step 4: Implement simulated consoles**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Equipment/ConveyorConsole.cs`:

```csharp
using Backend.Demo.Equipment.Tasks;
using FlowEngine.Execution.Consoles;

namespace Backend.Demo.Equipment;

public sealed class ConveyorConsole : FunctionConsole {
    public const string Name = "conveyor";

    public ConveyorConsole(...) : base(...) {
        On<ReceiveOperationTask>(async task => {
            await Task.Delay(task.DelayMilliseconds);
        });

        On<TransferOperationTask>(async task => {
            await Task.Delay(task.DelayMilliseconds);
        });
    }
}
```

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Equipment/StackCraneConsole.cs` with the same delayed-task pattern for `PutawayOperationTask` and `RetrieveOperationTask`.

- [ ] **Step 5: Implement simple order-to-flow mapping and flow factories**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Application/OrderFlowMapper.cs`:

```csharp
public sealed class OrderFlowMapper {
    public Dictionary<string, object?> MapInbound(InboundOrder order) => new() {
        ["orderCode"] = order.Code,
        ["source"] = order.Source,
        ["lineCount"] = order.Lines.Count,
    };

    public Dictionary<string, object?> MapOutbound(OutboundOrder order) => new() {
        ["orderCode"] = order.Code,
        ["destination"] = order.Destination,
        ["lineCount"] = order.Lines.Count,
    };
}
```

Create `InboundOrderFlowFactory` and `OutboundOrderFlowFactory` to build simple flows whose nodes dispatch:
- inbound: `ReceiveOperationTask` then `PutawayOperationTask`
- outbound: `RetrieveOperationTask` then `TransferOperationTask`

- [ ] **Step 6: Run the flow execution tests and verify they pass**

Run the same `dotnet test` command from Step 2.

Expected: pass with both flow paths completing through delayed operation tasks.

- [ ] **Step 7: Commit the simulated equipment slice**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend add .
git -C /Users/qiping/Desktop/codes/work/Backend commit -m "feat: add simulated consoles and order flows"
```

### Task 6: Add Startup Seed Data, Demo Flows, And End-To-End Verification

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.SampleData/BackendDemoSeeder.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.SampleData/SeedData/SeedWarehouses.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.SampleData/SeedData/SeedLocations.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.SampleData/SeedData/SeedSkus.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.SampleData/SeedData/SeedFlowBindings.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Program.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/appsettings.json`
- Create: `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Tests/Startup/BackendDemoSeederTest.cs`

- [ ] **Step 1: Write the failing seeder test**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.Tests/Startup/BackendDemoSeederTest.cs`:

```csharp
[Fact]
public async Task SeedAsync_IsIdempotent_AndCreatesBindings() {
    var services = new ServiceCollection();
    services.AddBackendDemoApplication("Data Source=:memory:");
    services.AddSingleton<BackendDemoSeeder>();

    using var provider = services.BuildServiceProvider();
    var seeder = provider.GetRequiredService<BackendDemoSeeder>();

    await seeder.SeedAsync();
    await seeder.SeedAsync();

    using var scope = provider.CreateScope();
    var inboundManager = scope.ServiceProvider.GetRequiredService<IManager<int, InboundOrder>>();
    var bindingManager = scope.ServiceProvider.GetRequiredService<IManager<int, FlowBinding>>();

    Assert.True(await bindingManager.AnyAsync(source => source.Where(x => x.BusinessType == BusinessFlowType.InboundOrder)));
    Assert.False(await inboundManager.AnyAsync());
}
```

- [ ] **Step 2: Run the seeder test and verify it fails**

Run:

```bash
dotnet test /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln --filter "FullyQualifiedName~BackendDemoSeederTest" -v minimal
```

Expected: fail because the seeder does not exist yet.

- [ ] **Step 3: Implement idempotent startup seeding**

Create `/Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo.SampleData/BackendDemoSeeder.cs`:

```csharp
public sealed class BackendDemoSeeder {
    public async Task SeedAsync() {
        await SeedWarehousesAsync();
        await SeedLocationsAsync();
        await SeedSkusAsync();
        await SeedFlowBindingsAsync();
        await EnsurePublishedDemoFlowsAsync();
    }
}
```

Populate the `SeedData` helpers with exact records:
- warehouse: `WH-01`
- locations: `IN-01`, `RACK-A01`, `RACK-A02`, `OUT-01`
- skus: `SKU-001`, `SKU-002`, `SKU-003`
- bindings:
  - inbound -> `inbound-basic`
  - outbound -> `outbound-basic`

- [ ] **Step 4: Run the seeder test and then run the full backend test suite**

Run:

```bash
dotnet test /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln --filter "FullyQualifiedName~BackendDemoSeederTest" -v minimal
dotnet test /Users/qiping/Desktop/codes/work/Backend/Backend.Demo.sln -v minimal
```

Expected: all backend demo tests pass.

- [ ] **Step 5: Start the backend and verify Swagger is reachable**

Run:

```bash
dotnet run --project /Users/qiping/Desktop/codes/work/Backend/src/Backend.Demo/Backend.Demo.csproj
```

Expected: the app starts, creates `backend-demo.db`, seeds the database, and serves Swagger on the default ASP.NET Core development URL.

- [ ] **Step 6: Commit the seeded end-to-end demo**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend add .
git -C /Users/qiping/Desktop/codes/work/Backend commit -m "feat: seed backend demo and verify end-to-end flow"
```
