# Port Resource Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add explicit inbound and outbound `Port` resources so the demo warehouse flows stage pallets through ports before rack putaway or shipment handoff.

**Architecture:** Introduce a new `Port` entity that participates in the same FlowEngine acquire/release lifecycle as `Location` and `Pallet`, but keeps its own business state through `PortStatus`. Extend inbound and outbound flow seeds with small `FunctionConsole` tasks that mutate port occupancy explicitly, then update smoke tests and FlowView resource summaries so the new handoff states are visible end to end.

**Tech Stack:** .NET 10, FlowEngine.Execution, FlowEngine.Server, SQLite-backed demo backend, xUnit, React, TypeScript, Vite

---

## Planned File Structure

**Backend domain and API**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Port.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Enums/PortStatus.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Enums/PortType.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Contracts/MasterData/PortModels.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Controllers/PortsController.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoMasterDataSeeds.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Migrations`

**Backend resource logic and flows**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/PortAcquireTasks.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/PortResourceRules.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/OccupyInboundPortTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/BindOutboundPortTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/ReleaseOutboundPortTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Application/OrderFlowService.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/ConveyorTransferOperationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/StackCraneRetrieveOperationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs`

**Backend tests**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/PortResourceRulesTest.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/OutboundPortTasksTest.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs`

**FlowView**
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.test.ts`

These files keep responsibilities separated:
- `Port` and enums define the new resource surface
- acquire tasks and function tasks hold business state mutation
- flow seeds express orchestration only
- backend tests prove lock and status semantics
- FlowView stays in the explanation layer rather than inventing new API contracts

### Task 1: Add the `Port` resource model, seed data, and CRUD surface

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Port.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Enums/PortStatus.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Domain/Enums/PortType.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Contracts/MasterData/PortModels.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Controllers/PortsController.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoMasterDataSeeds.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/EntityRegistrationTest.cs`

- [ ] **Step 1: Write the failing entity registration test**

```csharp
[Fact]
public void DomainEntities_ContainsPort() {
    var entityTypes = typeof(Program).Assembly
        .GetTypes()
        .Where(type => typeof(IEntity<int>).IsAssignableFrom(type))
        .Select(type => type.Name)
        .ToArray();

    Assert.Contains("Port", entityTypes);
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "DomainEntities_ContainsPort"`
Expected: FAIL because the `Port` entity does not exist yet.

- [ ] **Step 3: Add the minimal model, contract, controller, and seed**

```csharp
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

```csharp
public enum PortType {
    Inbound = 1,
    Outbound = 2
}

public enum PortStatus {
    Idle = 1,
    Reserved = 2,
    Occupied = 3
}
```

```csharp
public sealed class PortsController
    : ApiController<int, Port, PortInputModel, PortOutputModel> {
    public PortsController(IManager manager) : base(manager) { }
}
```

Seed one inbound and one outbound port in `BackendDemoMasterDataSeeds`:

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

- [ ] **Step 4: Run focused tests to verify the model is registered**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "DomainEntities_ContainsPort|Get_ReturnsSeededMasterData"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo add \
  src/Backend.Demo/Domain/Port.cs \
  src/Backend.Demo/Domain/Enums/PortStatus.cs \
  src/Backend.Demo/Domain/Enums/PortType.cs \
  src/Backend.Demo.Contracts/MasterData/PortModels.cs \
  src/Backend.Demo/Controllers/PortsController.cs \
  src/Backend.Demo/Seeding/BackendDemoMasterDataSeeds.cs \
  src/Backend.Demo.Tests/EntityRegistrationTest.cs
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo commit -m "feat: add port resource model"
```

### Task 2: Add port acquire rules and explicit inbound port occupancy

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/PortAcquireTasks.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/PortResourceRules.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/OccupyInboundPortTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/PortResourceRulesTest.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Application/OrderFlowService.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/ConveyorTransferOperationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs`

- [ ] **Step 1: Write the failing rule test**

```csharp
[Fact]
public async Task AcquireInboundPort_FindsIdleInboundPortOnly() {
    var task = new AcquireInboundPortTask();
    await task.ProcessAsync(_functionConsole, CancellationToken.None);

    Assert.True(task.InboundPortId > 0);
    Assert.Equal("IN-PORT-01", task.InboundPortCode);
}
```

- [ ] **Step 2: Run the rule test to verify it fails**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "AcquireInboundPort_FindsIdleInboundPortOnly"`
Expected: FAIL because no inbound port acquire task exists yet.

- [ ] **Step 3: Implement the inbound acquire task and occupancy task**

```csharp
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
```

```csharp
public sealed class OccupyInboundPortTask : OperationTask<FunctionConsole> {
    [Input] public int InboundPortId { get; set; }
    [Input] public string OrderCode { get; set; } = string.Empty;
    [Input] public int? RequestedTargetLocationId { get; set; }

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var manager = console.ServiceProvider.GetRequiredService<IManager>();
        await manager.UpdateAsync<int, Port>(InboundPortId, entity => {
            entity.Status = PortStatus.Occupied;
        });
    }
}
```

Update inbound flow nodes in `BackendDemoFlowSeeds.cs`:

```csharp
"AcquireInboundPort",
"ConveyorToInboundPort",
"OccupyInboundPort",
"AcquireTargetLocation",
"ResolveTargetLocation",
"StackCraneMoveToRack",
"BindLocationPallet"
```

Update the inbound smoke test to assert the new node ids and confirm the inbound port returns to `Idle` after completion.

- [ ] **Step 4: Run focused inbound tests**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "AcquireInboundPort_FindsIdleInboundPortOnly|InboundFlow_AcquiresTargetLocationDuringRun_AndReleasesItAsOccupiedWithBoundPallet"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo add \
  src/Backend.Demo/Resource/PortAcquireTasks.cs \
  src/Backend.Demo/Resource/PortResourceRules.cs \
  src/Backend.Demo/Resource/OccupyInboundPortTask.cs \
  src/Backend.Demo/Application/OrderFlowService.cs \
  src/Backend.Demo/Consoles/ConveyorTransferOperationTask.cs \
  src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs \
  src/Backend.Demo.Tests/PortResourceRulesTest.cs \
  src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo commit -m "feat: stage inbound flow through ports"
```

### Task 3: Add outbound port staging and release tasks

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/BindOutboundPortTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/ReleaseOutboundPortTask.cs`
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/OutboundPortTasksTest.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/StackCraneRetrieveOperationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs`

- [ ] **Step 1: Write the failing function-task test**

```csharp
[Fact]
public async Task BindOutboundPortTask_MovesPalletFromRackToOutboundPort() {
    await _task.ProcessAsync(_functionConsole, CancellationToken.None);

    Assert.Equal(PortStatus.Occupied, _refreshedPort!.Status);
    Assert.Equal(_pallet.Id, _refreshedPort.CurrentPalletId);
    Assert.Equal(LocationStatus.Empty, _refreshedLocation!.Status);
    Assert.Null(_refreshedLocation.CurrentPalletId);
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "BindOutboundPortTask_MovesPalletFromRackToOutboundPort"`
Expected: FAIL because the outbound binding task does not exist yet.

- [ ] **Step 3: Implement outbound binding and release, then wire the flow**

```csharp
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

```csharp
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

Update outbound flow nodes:

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

- [ ] **Step 4: Run focused outbound tests**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "BindOutboundPortTask_MovesPalletFromRackToOutboundPort|OutboundFlow_UsesOutboundPortAndReleasesItAfterShipment"`
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
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo commit -m "feat: stage outbound flow through ports"
```

### Task 4: Expand backend HTTP smoke coverage for port lifecycle

**Files:**
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs`
- Reference: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Controllers/PortsController.cs`

- [ ] **Step 1: Add failing HTTP assertions for port lifecycle**

Add these checks to the inbound and outbound smoke tests after the flow completes:

```csharp
var inboundPort = await manager.Query<Port>()
    .SingleAsync(entity => entity.Code == "IN-PORT-01");
Assert.Equal(PortStatus.Idle, inboundPort.Status);
Assert.Null(inboundPort.CurrentPalletId);

var outboundPort = await manager.Query<Port>()
    .SingleAsync(entity => entity.Code == "OUT-PORT-01");
Assert.Equal(PortStatus.Idle, outboundPort.Status);
Assert.Null(outboundPort.CurrentPalletId);
```

- [ ] **Step 2: Run the full backend smoke suite to verify the new assertions fail first**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "InboundFlow_|OutboundFlow_"`
Expected: FAIL until inbound and outbound tasks update port state consistently.

- [ ] **Step 3: Tighten the polling helpers once the flow logic exists**

Use helper methods that wait for both flow completion and final port state before asserting:

```csharp
await WaitForAsync(async () => {
    var port = await manager.Query<Port>().SingleAsync(entity => entity.Code == "OUT-PORT-01");
    return port.Status == PortStatus.Idle && port.CurrentPalletId == null;
}, TimeSpan.FromSeconds(10));
```

- [ ] **Step 4: Run the backend suite**

Run: `dotnet build /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/Backend.Demo.slnx --no-restore -v minimal -m:1 /nr:false`
Expected: `Build succeeded.`

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/Backend.Demo.slnx --no-build --no-restore -v minimal -m:1 /nr:false`
Expected: all backend tests PASS.

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo add \
  src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo commit -m "test: cover port lifecycle in backend smoke tests"
```

### Task 5: Update FlowView resource summaries for inbound and outbound ports

**Files:**
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.test.ts`

- [ ] **Step 1: Write failing FlowView summary tests**

Add summary expectations for these nodes:

```ts
it("describes inbound port staging before rack putaway", () => {
  expect(toTransition("OccupyInboundPort", vars)).toEqual({
    before: "IN-PORT-01 idle",
    after: "IN-PORT-01 occupied with pallet staged",
  });
});

it("describes outbound port release after shipment", () => {
  expect(toTransition("ReleaseOutboundPort", vars)).toEqual({
    before: "OUT-PORT-01 occupied with pallet staged",
    after: "OUT-PORT-01 idle and pallet released",
  });
});
```

- [ ] **Step 2: Run the targeted test file to verify it fails**

Run: `cd /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel && npm test -- --run src/lib/resourceSummary.test.ts`
Expected: FAIL because the summary layer does not know about port nodes yet.

- [ ] **Step 3: Extend the summary helpers minimally**

```ts
case "AcquireInboundPort":
  return makeSummary({ portCode: vars.InboundPortCode, ruleMatch: "idle-inbound-port" })
case "OccupyInboundPort":
  return toTransitionResult("idle", "occupied with pallet staged")
case "AcquireOutboundPort":
  return makeSummary({ portCode: vars.OutboundPortCode, ruleMatch: "idle-outbound-port" })
case "BindOutboundPort":
  return toTransitionResult("rack occupied", "outbound port occupied")
case "ReleaseOutboundPort":
  return toTransitionResult("outbound port occupied", "outbound port idle")
```

- [ ] **Step 4: Run FlowView verification**

Run: `cd /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel && npm test`
Expected: all tests PASS.

Run: `cd /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel && npm run lint && npm run build`
Expected: lint and build PASS.

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel add \
  src/lib/resourceSummary.ts \
  src/lib/resourceSummary.test.ts
git -C /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel commit -m "feat: explain port staging in resource summaries"
```

## Self-Review

- Spec coverage: inbound and outbound ports, acquire rules, function-based status mutation, smoke coverage, and FlowView summary updates are each mapped to a task.
- Placeholder scan: no `TODO`, `TBD`, or “implement later” placeholders remain.
- Type consistency: the plan uses a single `Port` entity, `PortType`, `PortStatus`, `AcquireInboundPortTask`, `OccupyInboundPortTask`, `BindOutboundPortTask`, and `ReleaseOutboundPortTask` consistently across all tasks.
