# Port Resources and Acquire Node Interaction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add explicit inbound and outbound `Port` resources to the demo warehouse flows and make acquire-style resource nodes fully inspectable and operable in `FlowView`.

**Architecture:** Extend the backend demo with a new `Port` resource entity, small `FunctionConsole` tasks for explicit port state mutation, and updated inbound/outbound flow seeds that stage pallets through ports before rack binding or shipment release. Then strengthen `FlowView` so acquire nodes use real runtime `availableActions`, explain resolved resources clearly, and automatically focus replacement or successor nodes after `Retry` and `Skip`.

**Tech Stack:** .NET 10, FlowEngine.Execution, FlowEngine.Server, SQLite, xUnit, React, TypeScript, Vite

---

## Planned File Structure

**Backend domain and HTTP surface**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Port.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Enums/PortType.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Enums/PortStatus.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Contracts/MasterData/PortModels.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Controllers/PortsController.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoMasterDataSeeds.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Migrations`

**Backend flow/resource logic**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/PortAcquireTasks.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/PortResourceRules.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/OccupyInboundPortTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/BindOutboundPortTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/ReleaseOutboundPortTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/BindInboundLocationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Application/OrderFlowService.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/ConveyorTransferOperationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/StackCraneRetrieveOperationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs`

**Backend tests**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/PortResourceRulesTest.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/OutboundPortTasksTest.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BindInboundLocationTaskTest.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs`

**FlowView**
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.test.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/flowExecution.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/flowExecution.test.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/pages/TaskExecutionPage.tsx`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/types.ts`

These files keep the boundaries clean:
- `Port` owns new resource identity and business state
- acquire and function tasks mutate resource state explicitly
- flow seeds remain orchestration-only
- smoke tests verify runtime behavior over HTTP
- `FlowView` only consumes runtime state and explains it

### Task 1: Add the `Port` resource model and seed it through the demo data layer

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Port.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Enums/PortType.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Enums/PortStatus.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Contracts/MasterData/PortModels.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Controllers/PortsController.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoMasterDataSeeds.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/EntityRegistrationTest.cs`

- [ ] **Step 1: Write the failing backend registration test**

Add a focused assertion to `EntityRegistrationTest.cs`:

```csharp
[Fact]
public void DomainEntities_ContainsPortResource() {
    var entityTypes = typeof(Program).Assembly
        .GetTypes()
        .Where(type => typeof(IEntity<int>).IsAssignableFrom(type))
        .Select(type => type.Name)
        .ToArray();

    Assert.Contains("Port", entityTypes);
}
```

- [ ] **Step 2: Run the registration test to verify it fails**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "DomainEntities_ContainsPortResource"`

Expected: FAIL because `Port` is not defined yet.

- [ ] **Step 3: Add the minimal `Port` domain model, DTOs, controller, and master data**

Create `Port.cs`:

```csharp
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;

namespace Backend.Demo.Domain;

[Resource]
public sealed class Port : Entity<int>, IResource<int> {
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool Acquired { get; set; }
    public PortType PortType { get; set; }
    public PortStatus Status { get; set; }
    public int? CurrentPalletId { get; set; }
    public Pallet? CurrentPallet { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
}
```

Create `PortType.cs` and `PortStatus.cs`:

```csharp
namespace Backend.Demo.Domain.Enums;

public enum PortType {
    Inbound = 1,
    Outbound = 2
}
```

```csharp
namespace Backend.Demo.Domain.Enums;

public enum PortStatus {
    Idle = 1,
    Reserved = 2,
    Occupied = 3
}
```

Create `PortModels.cs`:

```csharp
using Backend.Demo.Domain.Enums;

namespace Backend.Demo.Contracts.MasterData;

public sealed class PortInputModel {
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public PortType PortType { get; set; }
    public PortStatus Status { get; set; }
    public int? CurrentPalletId { get; set; }
    public int WarehouseId { get; set; }
}

public sealed class PortOutputModel : PortInputModel {
    public int Id { get; set; }
    public bool Acquired { get; set; }
}
```

Create `PortsController.cs`:

```csharp
using Backend.Demo.Contracts.MasterData;
using Backend.Demo.Domain;
using FlowEngine.Data;
using FlowEngine.Server.WebApi;

namespace Backend.Demo.Controllers;

public sealed class PortsController : ApiController<int, Port, PortInputModel, PortOutputModel> {
    public PortsController(IManager manager) : base(manager) { }
}
```

Seed ports in `BackendDemoMasterDataSeeds.cs`:

```csharp
new Port {
    Code = "IN-PORT-01",
    Name = "Inbound Port 01",
    Enabled = true,
    PortType = PortType.Inbound,
    Status = PortStatus.Idle,
    WarehouseId = warehouse.Id
},
new Port {
    Code = "OUT-PORT-01",
    Name = "Outbound Port 01",
    Enabled = true,
    PortType = PortType.Outbound,
    Status = PortStatus.Idle,
    WarehouseId = warehouse.Id
}
```

- [ ] **Step 4: Run focused backend tests**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "DomainEntities_ContainsPortResource|BackendDemoInitializationTest"`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo add \
  src/Backend.Demo/Domain/Port.cs \
  src/Backend.Demo/Domain/Enums/PortType.cs \
  src/Backend.Demo/Domain/Enums/PortStatus.cs \
  src/Backend.Demo.Contracts/MasterData/PortModels.cs \
  src/Backend.Demo/Controllers/PortsController.cs \
  src/Backend.Demo/Seeding/BackendDemoMasterDataSeeds.cs \
  src/Backend.Demo.Tests/EntityRegistrationTest.cs
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo commit -m "feat: add port resource model"
```

### Task 2: Route the inbound flow through an explicit inbound port

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/PortAcquireTasks.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/PortResourceRules.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/OccupyInboundPortTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/BindInboundLocationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Application/OrderFlowService.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/ConveyorTransferOperationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BindInboundLocationTaskTest.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/PortResourceRulesTest.cs`

- [ ] **Step 1: Write the failing inbound port rule test**

Create `PortResourceRulesTest.cs`:

```csharp
using Backend.Demo.DependencyInjection;
using Backend.Demo.Resource;
using FlowEngine.Execution.Consoles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Demo.Tests;

public sealed class PortResourceRulesTest {
    [Fact]
    public async Task AcquireInboundPortTask_ResolvesIdleInboundPort() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication("Data Source=:memory:");

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IBackendDemoInitializer>().InitializeAsync();

        var console = ActivatorUtilities.CreateInstance<FunctionConsole>(scope.ServiceProvider);
        var task = new AcquireInboundPortTask();

        await task.ProcessAsync(console, CancellationToken.None);

        Assert.True(task.InboundPortId > 0);
        Assert.Equal("IN-PORT-01", task.InboundPortCode);
    }
}
```

- [ ] **Step 2: Run the inbound port rule test to verify it fails**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "AcquireInboundPortTask_ResolvesIdleInboundPort"`

Expected: FAIL because no inbound port acquire task exists yet.

- [ ] **Step 3: Implement inbound port acquire, occupancy, and clearing**

Create `PortAcquireTasks.cs`:

```csharp
using Backend.Demo.Domain.Enums;
using FlowEngine.Execution.Consoles;

namespace Backend.Demo.Resource;

public sealed class AcquireInboundPortTask : OperationTask<FunctionConsole> {
    [Output]
    public int InboundPortId { get; set; }

    [Output]
    public string InboundPortCode { get; set; } = string.Empty;

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var port = await PortResourceRules.AcquireAsync(console.ServiceProvider, PortType.Inbound, cancellationToken);
        InboundPortId = port.Id;
        InboundPortCode = port.Code;
    }
}

public sealed class AcquireOutboundPortTask : OperationTask<FunctionConsole> {
    [Output]
    public int OutboundPortId { get; set; }

    [Output]
    public string OutboundPortCode { get; set; } = string.Empty;

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var port = await PortResourceRules.AcquireAsync(console.ServiceProvider, PortType.Outbound, cancellationToken);
        OutboundPortId = port.Id;
        OutboundPortCode = port.Code;
    }
}
```

Create `PortResourceRules.cs`:

```csharp
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Demo.Resource;

internal static class PortResourceRules {
    public static async Task<Port> AcquireAsync(IServiceProvider serviceProvider, PortType portType, CancellationToken cancellationToken) {
        var manager = serviceProvider.GetRequiredService<IManager>();
        var port = await manager.Query<Port>()
            .Where(entity => entity.Enabled && !entity.Acquired && entity.Status == PortStatus.Idle && entity.PortType == portType)
            .OrderBy(entity => entity.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"No idle {portType} port is available.");

        await manager.UpdateAsync<int, Port>(port.Id, entity => {
            entity.Acquired = true;
        });

        return port;
    }
}
```

Create `OccupyInboundPortTask.cs`:

```csharp
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution.Consoles;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Demo.Resource;

public sealed class OccupyInboundPortTask : OperationTask<FunctionConsole> {
    [Input]
    public int InboundPortId { get; set; }

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var manager = console.ServiceProvider.GetRequiredService<IManager>();
        await manager.UpdateAsync<int, Port>(InboundPortId, entity => {
            entity.Status = PortStatus.Occupied;
        });
    }
}
```

Update `BindInboundLocationTask.cs` inputs and side effects:

```csharp
[Input]
public int InboundPortId { get; set; }
```

Then clear the port while binding the rack:

```csharp
await scopedManager.Service.UpdateAsync<int, Port>(InboundPortId, entity => {
    entity.Status = PortStatus.Idle;
    entity.CurrentPalletId = null;
});
```

Update inbound flow node order in `BackendDemoFlowSeeds.cs`:

```csharp
"AcquireInboundPort",
"ConveyorToInboundPort",
"OccupyInboundPort",
"AcquireTargetLocation",
"ResolveTargetLocation",
"StackCraneMoveToRack",
"BindLocationPallet"
```

Update `OrderFlowService.cs` to pass `RequestedTargetLocationId` and port outputs into later nodes when building runtime variables.

- [ ] **Step 4: Run focused inbound tests**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "AcquireInboundPortTask_ResolvesIdleInboundPort|BindInboundLocationTaskTest|InboundFlow_AcquiresTargetLocationDuringRun_AndReleasesItAsOccupiedWithBoundPallet"`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo add \
  src/Backend.Demo/Resource/PortAcquireTasks.cs \
  src/Backend.Demo/Resource/PortResourceRules.cs \
  src/Backend.Demo/Resource/OccupyInboundPortTask.cs \
  src/Backend.Demo/Resource/BindInboundLocationTask.cs \
  src/Backend.Demo/Application/OrderFlowService.cs \
  src/Backend.Demo/Consoles/ConveyorTransferOperationTask.cs \
  src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs \
  src/Backend.Demo.Tests/BindInboundLocationTaskTest.cs \
  src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs \
  src/Backend.Demo.Tests/PortResourceRulesTest.cs
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo commit -m "feat: route inbound flow through port staging"
```

### Task 3: Route the outbound flow through an explicit outbound port

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/BindOutboundPortTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/ReleaseOutboundPortTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/OutboundPortTasksTest.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/StackCraneRetrieveOperationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs`

- [ ] **Step 1: Write the failing outbound bind/release test**

Create `OutboundPortTasksTest.cs`:

```csharp
using Backend.Demo.DependencyInjection;
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using Backend.Demo.Resource;
using FlowEngine.Data;
using FlowEngine.Execution.Consoles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Demo.Tests;

public sealed class OutboundPortTasksTest {
    [Fact]
    public async Task BindOutboundPortTask_MovesPalletFromRackToOutboundPort() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication("Data Source=:memory:");

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IBackendDemoInitializer>().InitializeAsync();

        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var console = ActivatorUtilities.CreateInstance<FunctionConsole>(scope.ServiceProvider);
        var location = await manager.Query<Location>().FirstAsync(entity => entity.Code == "RACK-A1");
        var port = await manager.Query<Port>().FirstAsync(entity => entity.Code == "OUT-PORT-01");
        var pallet = await manager.Query<Pallet>().FirstAsync(entity => entity.Code == "PLT-OUT-SEED-01");

        var task = new BindOutboundPortTask {
            SourceLocationId = location.Id,
            SourcePalletId = pallet.Id,
            OutboundPortId = port.Id
        };

        await task.ProcessAsync(console, CancellationToken.None);

        var refreshedLocation = await manager.GetByIdAsync<int, Location>(location.Id);
        var refreshedPort = await manager.GetByIdAsync<int, Port>(port.Id);

        Assert.Equal(LocationStatus.Empty, refreshedLocation!.Status);
        Assert.Null(refreshedLocation.CurrentPalletId);
        Assert.Equal(PortStatus.Occupied, refreshedPort!.Status);
        Assert.Equal(pallet.Id, refreshedPort.CurrentPalletId);
    }
}
```

- [ ] **Step 2: Run the outbound task test to verify it fails**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "BindOutboundPortTask_MovesPalletFromRackToOutboundPort"`

Expected: FAIL because outbound port tasks and seed data do not exist yet.

- [ ] **Step 3: Implement outbound bind/release and flow nodes**

Create `BindOutboundPortTask.cs`:

```csharp
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution.Consoles;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Demo.Resource;

public sealed class BindOutboundPortTask : OperationTask<FunctionConsole> {
    [Input] public int SourceLocationId { get; set; }
    [Input] public int SourcePalletId { get; set; }
    [Input] public int OutboundPortId { get; set; }

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var manager = console.ServiceProvider.GetRequiredService<IManager>();

        await manager.UpdateAsync<int, Location>(SourceLocationId, entity => {
            entity.Status = LocationStatus.Empty;
            entity.CurrentPalletId = null;
        });

        await manager.UpdateAsync<int, Port>(OutboundPortId, entity => {
            entity.Status = PortStatus.Occupied;
            entity.CurrentPalletId = SourcePalletId;
        });
    }
}
```

Create `ReleaseOutboundPortTask.cs`:

```csharp
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution.Consoles;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Demo.Resource;

public sealed class ReleaseOutboundPortTask : OperationTask<FunctionConsole> {
    [Input] public int OutboundPortId { get; set; }
    [Input] public int SourcePalletId { get; set; }

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var manager = console.ServiceProvider.GetRequiredService<IManager>();

        await manager.UpdateAsync<int, Port>(OutboundPortId, entity => {
            entity.Status = PortStatus.Idle;
            entity.CurrentPalletId = null;
        });

        await manager.UpdateAsync<int, Pallet>(SourcePalletId, entity => {
            entity.Enabled = false;
        });
    }
}
```

Update `BackendDemoFlowSeeds.cs` outbound node order:

```csharp
"AcquireSourceLocation",
"ResolveSourceLocation",
"AcquireSourcePallet",
"AcquireOutboundPort",
"StackCraneMoveToOutboundPort",
"BindOutboundPort",
"ConveyorFromOutboundPort",
"ReleaseOutboundPort"
```

Update `StackCraneRetrieveOperationTask.cs` so its completion message references the outbound port instead of shipment completion.

- [ ] **Step 4: Run focused outbound tests**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "BindOutboundPortTask_MovesPalletFromRackToOutboundPort|OutboundFlow_"`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo add \
  src/Backend.Demo/Resource/BindOutboundPortTask.cs \
  src/Backend.Demo/Resource/ReleaseOutboundPortTask.cs \
  src/Backend.Demo/Consoles/StackCraneRetrieveOperationTask.cs \
  src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs \
  src/Backend.Demo.Tests/OutboundPortTasksTest.cs \
  src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo commit -m "feat: route outbound flow through port staging"
```

### Task 4: Add acquire-node HTTP smoke coverage for cancel, retry, and skip

**Files:**
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs`
- Reference: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/PortAcquireTasks.cs`

- [ ] **Step 1: Write the failing acquire-node smoke tests**

Add three tests around a running acquire node:

```csharp
[Fact]
public async Task AcquireNodeCancel_ThroughHttp_ExposesRetryAndSkipActions() { }

[Fact]
public async Task AcquireNodeRetry_ThroughHttp_CreatesReplacementAcquireNode() { }

[Fact]
public async Task AcquireNodeSkip_ThroughHttp_AdvancesToSuccessorNode() { }
```

Inside each test, reuse the existing host/bootstrap helpers and assert against `availableActions` on the acquire executable:

```csharp
Assert.Contains("cancel", acquireExecutable.GetProperty("availableActions").EnumerateArray().Select(x => x.GetString()));
```

- [ ] **Step 2: Run the smoke tests to verify they fail**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "AcquireNode"`

Expected: FAIL because the tests do not yet know how to identify acquire nodes in the new runtime graphs.

- [ ] **Step 3: Add helper methods and assertions around acquire executables**

Add a helper to select acquire nodes by `nodeId` prefix:

```csharp
private static JsonElement FindAcquireExecutable(JsonDocument document, params string[] nodeIds) {
    return document.RootElement.GetProperty("executableDetailModels")
        .EnumerateArray()
        .First(node => nodeIds.Contains(node.GetProperty("nodeId").GetString()));
}
```

Then assert action transitions:

```csharp
Assert.Contains("restart", canceledAcquire.GetProperty("availableActions").EnumerateArray().Select(x => x.GetString()));
Assert.Contains("skip", canceledAcquire.GetProperty("availableActions").EnumerateArray().Select(x => x.GetString()));
```

Use the existing retry/skip polling pattern to confirm:
- retry creates a new unacknowledged acquire executable with the same `nodeId`
- skip yields a new unacknowledged successor executable or a completed flow

- [ ] **Step 4: Run backend verification**

Run: `dotnet build /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/Backend.Demo.slnx --no-restore -v minimal -m:1 /nr:false`

Expected: `Build succeeded.`

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/Backend.Demo.slnx --no-build --no-restore -v minimal -m:1 /nr:false`

Expected: all backend tests PASS.

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo add \
  src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo commit -m "test: cover acquire node control flows"
```

### Task 5: Make `FlowView` explain and follow acquire-node interactions correctly

**Files:**
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.test.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/flowExecution.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/flowExecution.test.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/pages/TaskExecutionPage.tsx`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/types.ts`

- [ ] **Step 1: Write the failing FlowView tests**

Add resource summary tests:

```ts
it("describes inbound port acquisition and occupancy", () => {
  expect(toTransition("OccupyInboundPort", vars)).toEqual({
    before: "IN-PORT-01 idle",
    after: "IN-PORT-01 occupied with pallet staged",
  });
});

it("describes outbound port release", () => {
  expect(toTransition("ReleaseOutboundPort", vars)).toEqual({
    before: "OUT-PORT-01 occupied with pallet staged",
    after: "OUT-PORT-01 idle and pallet released",
  });
});
```

Add acquire interaction tests:

```ts
it("prefers server availableActions for acquire nodes", () => {
  expect(getNodeActions(acquireExecutable, parentTask)).toEqual(["cancel"]);
});

it("finds the replacement acquire executable after retry", () => {
  expect(findRetryReplacementExecutable(snapshot, acquireExecutable)?.nodeId).toBe("AcquireInboundPort");
});

it("finds the successor executable after skipping an acquire node", () => {
  expect(findNextExecutableAfterSkip(snapshot, acquireExecutable)?.nodeId).toBe("ConveyorToInboundPort");
});
```

- [ ] **Step 2: Run the targeted FlowView tests to verify they fail**

Run: `cd /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel && npm test -- --run src/lib/resourceSummary.test.ts src/lib/flowExecution.test.ts`

Expected: FAIL because the summary and helper layers do not know about port or acquire-node semantics yet.

- [ ] **Step 3: Update the summary and acquire-node helper logic**

Extend `resourceSummary.ts` with node-specific handling:

```ts
case "AcquireInboundPort":
  return {
    ruleMatch: "idle-inbound-port",
    resolvedResourceCode: vars.InboundPortCode,
  };
case "OccupyInboundPort":
  return {
    before: `${vars.InboundPortCode} idle`,
    after: `${vars.InboundPortCode} occupied with pallet staged`,
  };
case "AcquireOutboundPort":
  return {
    ruleMatch: "idle-outbound-port",
    resolvedResourceCode: vars.OutboundPortCode,
  };
case "ReleaseOutboundPort":
  return {
    before: `${vars.OutboundPortCode} occupied with pallet staged`,
    after: `${vars.OutboundPortCode} idle and pallet released`,
  };
```

Update `flowExecution.ts` so acquire-node action helpers:
- still prefer server `availableActions`
- hide stale acquire actions when the parent flow is terminal
- reuse existing retry/successor resolution by `nodeId` and `parentId` for acquire nodes

Update `TaskExecutionPage.tsx` to show resource-target explanation for acquire nodes using the same summary metadata rather than a separate UI model.

- [ ] **Step 4: Run FlowView verification**

Run: `cd /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel && npm test`

Expected: all tests PASS.

Run: `cd /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel && npm run lint && npm run build`

Expected: lint and build PASS.

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel add \
  src/lib/resourceSummary.ts \
  src/lib/resourceSummary.test.ts \
  src/lib/flowExecution.ts \
  src/lib/flowExecution.test.ts \
  src/pages/TaskExecutionPage.tsx \
  src/types.ts
git -C /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel commit -m "feat: explain and follow acquire node interactions"
```

## Self-Review

- Spec coverage: `Port` model, inbound/outbound port staging, acquire-node control flows, backend smoke coverage, and `FlowView` acquire behavior each map directly to one task.
- Placeholder scan: no `TODO`, `TBD`, or “handle later” placeholders remain.
- Type consistency: the plan consistently uses `Port`, `PortType`, `PortStatus`, `AcquireInboundPortTask`, `AcquireOutboundPortTask`, `OccupyInboundPortTask`, `BindOutboundPortTask`, and `ReleaseOutboundPortTask`.
