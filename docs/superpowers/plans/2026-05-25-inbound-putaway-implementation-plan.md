# Inbound Putaway Flow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace `inbound-basic` with a clearer inbound putaway flow that models conveyor transfer, rack acquisition, stack crane movement, pallet/location binding, and automatic lock release.

**Architecture:** Keep physical device actions in `ConveyorConsole` and `StackCraneConsole`, move final inventory mutation into a new `FunctionConsole` task named `BindInboundLocationTask`, and update the flow seed so the inbound graph exposes requested vs resolved location plus pallet outputs. Extend the existing smoke tests to assert the new node order and the location/pallet end state, then add a small FlowView summary tweak for the new binding node.

**Tech Stack:** .NET 10, FlowEngine.Execution, FlowEngine.Server, SQLite-backed demo backend, xUnit, React/TypeScript FlowView

---

## Planned File Structure

**Backend code**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/BindInboundLocationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/StackCraneStoreOperationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/StackCraneConsole.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Application/OrderFlowService.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs`

**Backend tests**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BindInboundLocationTaskTest.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs`

**FlowView**
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.test.ts`

These files keep responsibilities clean:
- `BindInboundLocationTask` owns inbound business mutation
- `StackCraneStoreOperationTask` becomes purely physical
- flow seed and order service define orchestration and variables
- smoke tests cover the full backend story
- FlowView only updates its explanation layer

### Task 1: Add a focused test for inbound binding logic

**Files:**
- Create: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BindInboundLocationTaskTest.cs`
- Reference: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Resource/LoadLocationSnapshotOperationTask.cs`
- Reference: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/EntityRegistrationTest.cs`

- [ ] **Step 1: Write the failing test**

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

public sealed class BindInboundLocationTaskTest {
    [Fact]
    public async Task ProcessAsync_CreatesPallet_BindsSku_AndMarksLocationOccupied() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackendDemoApplication("Data Source=:memory:");

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IManager>();
        var console = scope.ServiceProvider.GetRequiredService<FunctionConsole>();

        var sku = await manager.AddAsync<int, Sku>(new Sku {
            Code = "SKU-UT-001",
            Name = "Unit Test Tote",
            Spec = "Blue"
        });
        var warehouse = await manager.AddAsync<int, Warehouse>(new Warehouse {
            Code = "WH-UT",
            Name = "Unit Test Warehouse"
        });
        var location = await manager.AddAsync<int, Location>(new Location {
            Code = "RACK-UT-01",
            Name = "Unit Rack",
            Enabled = true,
            Acquired = true,
            LocationType = LocationType.Rack,
            Status = LocationStatus.Empty,
            WarehouseId = warehouse.Id
        });

        var task = new BindInboundLocationTask {
            OrderCode = "IN-UT-1001",
            SkuId = sku.Id,
            SkuCode = sku.Code,
            TargetLocationId = location.Id,
            TargetLocationCode = location.Code
        };

        await task.ProcessAsync(console, CancellationToken.None);

        var refreshedLocation = await manager.GetByIdAsync<int, Location>(location.Id);
        var pallet = await manager.GetByIdAsync<int, Pallet>(task.InboundPalletId);

        Assert.NotNull(refreshedLocation);
        Assert.Equal(LocationStatus.Occupied, refreshedLocation!.Status);
        Assert.Equal(task.InboundPalletId, refreshedLocation.CurrentPalletId);
        Assert.NotNull(pallet);
        Assert.Equal($"PLT-{task.OrderCode}", pallet!.Code);
        Assert.Equal(sku.Id, pallet.SkuId);
        Assert.Equal("Inbound pallet bound to target rack.", task.Status);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "BindInboundLocationTaskTest"`
Expected: FAIL because `BindInboundLocationTask` does not exist yet.

- [ ] **Step 3: Create the minimal implementation**

```csharp
using Backend.Demo.Domain;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution.Consoles;
using Microsoft.EntityFrameworkCore;

namespace Backend.Demo.Resource;

public sealed class BindInboundLocationTask : OperationTask<FunctionConsole> {
    [Input]
    public string OrderCode { get; set; } = string.Empty;

    [Input]
    public int SkuId { get; set; }

    [Input]
    public string SkuCode { get; set; } = string.Empty;

    [Input]
    public int TargetLocationId { get; set; }

    [Input]
    public string TargetLocationCode { get; set; } = string.Empty;

    [Output]
    public int InboundPalletId { get; set; }

    [Output]
    public string InboundPalletCode { get; set; } = string.Empty;

    [Output]
    public string Status { get; set; } = string.Empty;

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var managerFactory = console.ServiceProvider.GetRequiredService<IManagerFactory>();
        using var scopedManager = managerFactory.Create();
        var transaction = scopedManager.Service.CreateTransaction();

        var location = await scopedManager.Service.GetByIdAsync<int, Location>(
            TargetLocationId,
            include: query => query.Include(entity => entity.CurrentPallet));
        if (location == null) {
            throw new InvalidOperationException($"Location-{TargetLocationId} not found.");
        }

        InboundPalletCode = $"PLT-{OrderCode}";
        var pallet = await transaction.AddAsync<int, Pallet>(new Pallet {
            Code = InboundPalletCode,
            Enabled = true,
            Acquired = false,
            SkuId = SkuId,
            Quantity = 1
        });
        await transaction.UpdateAsync<int, Location>(TargetLocationId, entity => {
            entity.Status = LocationStatus.Occupied;
            entity.CurrentPalletId = pallet.Id;
        });
        await transaction.CommitAsync();

        InboundPalletId = pallet.Id;
        Status = "Inbound pallet bound to target rack.";
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "BindInboundLocationTaskTest"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo add \
  src/Backend.Demo/Resource/BindInboundLocationTask.cs \
  src/Backend.Demo.Tests/BindInboundLocationTaskTest.cs
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo commit -m "feat: add inbound pallet binding task"
```

### Task 2: Make stack crane store physical-only and rewrite the inbound flow seed

**Files:**
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/StackCraneStoreOperationTask.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Consoles/StackCraneConsole.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Application/OrderFlowService.cs`
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs`
- Test: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs`

- [ ] **Step 1: Write the failing smoke assertions first**

In `InboundFlow_AcquiresTargetLocationDuringRun_AndReleasesItAsOccupiedWithBoundPallet`, add assertions for the new node ids after loading the completed task document:

```csharp
var nodeIds = flowTaskDocument.RootElement.GetProperty("executableDetailModels")
    .EnumerateArray()
    .Select(node => node.GetProperty("nodeId").GetString())
    .Where(nodeId => !string.IsNullOrWhiteSpace(nodeId))
    .ToArray();
Assert.Contains("ConveyorToInboundPort", nodeIds);
Assert.Contains("AcquireTargetLocation", nodeIds);
Assert.Contains("StackCraneMoveToRack", nodeIds);
Assert.Contains("BindLocationPallet", nodeIds);
```

Also assert that the pallet code follows the new `PLT-{OrderCode}` format:

```csharp
Assert.Equal("PLT-IN-RESOURCE-1001", pallet!.Code);
```

- [ ] **Step 2: Run the smoke test to verify it fails**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "InboundFlow_AcquiresTargetLocationDuringRun_AndReleasesItAsOccupiedWithBoundPallet"`
Expected: FAIL because the old flow still emits `Receive` and `Store`, and the pallet code still includes the target location suffix.

- [ ] **Step 3: Make the stack crane store task physical-only**

Update `StackCraneStoreOperationTask` to stop mutating business state and rename its completion message around movement instead of storage:

```csharp
protected override async Task DoProcessAsync(StackCraneConsole console, CancellationToken cancellationToken) {
    await Task.Delay(DelayMilliseconds, cancellationToken);
    await console.MoveToRackAsync(TargetLocationId);
    CompletionMessage = $"{OrderCode}: stack crane moved {SkuCode} to {TargetLocationCode}.";
}
```

Update `StackCraneConsole` to provide the physical method without creating pallets:

```csharp
public async Task MoveToRackAsync(int locationId) {
    using var scopedManager = _managerFactory.Create();
    var location = await scopedManager.Service.GetByIdAsync<int, Location>(locationId)
        ?? throw new InvalidOperationException($"Location-{locationId} not found.");
    if (!location.Enabled) {
        throw new InvalidOperationException($"Location-{locationId} is disabled.");
    }
}
```

- [ ] **Step 4: Rewrite the inbound seed flow and input variables**

In `OrderFlowService.StartInboundFlowAsync`, add the requested target id/code inputs:

```csharp
.WithInput(targetLocation.Code, "RequestedTargetLocationCode")
.WithInput(targetLocation.Id, "RequestedTargetLocationId")
```

In `BackendDemoFlowSeeds.BuildInboundDraftDocumentJson`, replace the old node list with:

```csharp
variables = new object[] {
    Variable("OrderCode", "string", "input", string.Empty),
    Variable("SourceLocationCode", "string", "input", string.Empty),
    Variable("WarehouseId", "int", "input", 0),
    Variable("RequestedTargetLocationCode", "string", "input", string.Empty),
    Variable("RequestedTargetLocationId", "int", "input", 0),
    Variable("TargetLocationCode", "string", "input", string.Empty),
    Variable("TargetLocationId", "int", "input", 0),
    Variable("SkuId", "int", "input", 0),
    Variable("SkuCode", "string", "input", string.Empty),
    Variable("InboundPalletId", "int", "output", 0),
    Variable("InboundPalletCode", "string", "output", string.Empty),
    Variable("Status", "string", "output", "Draft")
},
nodes = new object[] {
    Operation(
        id: "ConveyorToInboundPort",
        consoleId: ConveyorConsole.NAME,
        operationTaskType: typeof(ConveyorTransferOperationTask).FullName!,
        inputs: new object[] {
            Input("OrderCode", nameof(ConveyorTransferOperationTask.OrderCode)),
            Input("SourceLocationCode", nameof(ConveyorTransferOperationTask.FromLocationCode)),
            Input("RequestedTargetLocationCode", nameof(ConveyorTransferOperationTask.ToLocationCode))
        },
        outputs: Array.Empty<object>()),
    Operation(
        id: "AcquireTargetLocation",
        consoleId: FunctionConsole.NAME,
        operationTaskType: typeof(AcquireEmptyRackLocationTask).FullName!,
        inputs: new object[] {
            Input("WarehouseId", nameof(AcquireEmptyRackLocationTask.WarehouseId)),
            Input("RequestedTargetLocationId", nameof(AcquireEmptyRackLocationTask.PreferredLocationId))
        },
        outputs: new object[] {
            Output(nameof(AcquireEmptyRackLocationTask.Acquired), "TargetLocationId")
        },
        resourceOutputs: new object[] {
            ResourceOutput(nameof(AcquireEmptyRackLocationTask.Acquired), typeof(Location).FullName!)
        }),
    Operation(
        id: "ResolveTargetLocation",
        consoleId: FunctionConsole.NAME,
        operationTaskType: typeof(LoadLocationSnapshotOperationTask).FullName!,
        inputs: new object[] {
            Input("TargetLocationId", nameof(LoadLocationSnapshotOperationTask.LocationId))
        },
        outputs: new object[] {
            Output(nameof(LoadLocationSnapshotOperationTask.LocationCode), "TargetLocationCode")
        }),
    Operation(
        id: "StackCraneMoveToRack",
        consoleId: StackCraneConsole.NAME,
        operationTaskType: typeof(StackCraneStoreOperationTask).FullName!,
        inputs: new object[] {
            Input("OrderCode", nameof(StackCraneStoreOperationTask.OrderCode)),
            Input("SkuId", nameof(StackCraneStoreOperationTask.SkuId)),
            Input("SkuCode", nameof(StackCraneStoreOperationTask.SkuCode)),
            Input("TargetLocationCode", nameof(StackCraneStoreOperationTask.TargetLocationCode)),
            Input("TargetLocationId", nameof(StackCraneStoreOperationTask.TargetLocationId))
        },
        outputs: Array.Empty<object>()),
    Operation(
        id: "BindLocationPallet",
        consoleId: FunctionConsole.NAME,
        operationTaskType: typeof(BindInboundLocationTask).FullName!,
        inputs: new object[] {
            Input("OrderCode", nameof(BindInboundLocationTask.OrderCode)),
            Input("SkuId", nameof(BindInboundLocationTask.SkuId)),
            Input("SkuCode", nameof(BindInboundLocationTask.SkuCode)),
            Input("TargetLocationId", nameof(BindInboundLocationTask.TargetLocationId)),
            Input("TargetLocationCode", nameof(BindInboundLocationTask.TargetLocationCode))
        },
        outputs: new object[] {
            Output(nameof(BindInboundLocationTask.InboundPalletId), "InboundPalletId"),
            Output(nameof(BindInboundLocationTask.InboundPalletCode), "InboundPalletCode"),
            Output(nameof(BindInboundLocationTask.Status), "Status")
        })
},
routes = new object[] {
    Path("Root", new[] { "ConveyorToInboundPort" }),
    Path("ConveyorToInboundPort", new[] { "AcquireTargetLocation" }),
    Path("AcquireTargetLocation", new[] { "ResolveTargetLocation" }),
    Path("ResolveTargetLocation", new[] { "StackCraneMoveToRack" }),
    Path("StackCraneMoveToRack", new[] { "BindLocationPallet" })
}
```

- [ ] **Step 5: Run the smoke test to verify it passes**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "InboundFlow_AcquiresTargetLocationDuringRun_AndReleasesItAsOccupiedWithBoundPallet"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo add \
  src/Backend.Demo/Consoles/StackCraneStoreOperationTask.cs \
  src/Backend.Demo/Consoles/StackCraneConsole.cs \
  src/Backend.Demo/Application/OrderFlowService.cs \
  src/Backend.Demo/Seeding/BackendDemoFlowSeeds.cs \
  src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo commit -m "feat: replace inbound flow with putaway stages"
```

### Task 3: Broaden backend regression coverage for the new inbound story

**Files:**
- Modify: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs`
- Reference: `/Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/BackendDemoInitializationTest.cs`

- [ ] **Step 1: Write an additional failing smoke test for requested vs resolved tracking**

Add this test near the other inbound resource tests:

```csharp
[Fact]
public async Task InboundFlow_PreservesRequestedLocationVariables_WhenFallbackOccurs() {
    using var _ = UseObservableResourceOperationTaskDelays();
    var flowTaskId = await CreateAndStartInboundOrderAsync("IN-FALLBACK-1001", "RACK-A2");

    using var flowTaskDocument = await WaitForFlowTaskStatusAsync(flowTaskId, 4);
    var variables = flowTaskDocument.RootElement.GetProperty("variableEntities")
        .EnumerateArray()
        .ToDictionary(
            item => item.GetProperty("id").GetString()!,
            item => item.GetProperty("value").GetString());

    Assert.Equal("\"RACK-A2\"", variables["RequestedTargetLocationCode"]);
    Assert.Equal("\"RACK-A1\"", variables["TargetLocationCode"]);
    Assert.Equal("\"PLT-IN-FALLBACK-1001\"", variables["InboundPalletCode"]);
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "InboundFlow_PreservesRequestedLocationVariables_WhenFallbackOccurs"`
Expected: FAIL until the seed flow exposes the new pallet outputs and requested target id/code reliably.

- [ ] **Step 3: Adjust any missing variable outputs or helper waits**

If the test fails because `InboundPalletCode` is not yet persisted or because the flow finishes before the variable projection becomes observable, fix it in the smallest way possible:

```csharp
// Example helper pattern in BackendDemoApiSmokeTest.cs
private static Dictionary<string, string?> ReadVariables(JsonDocument document) {
    return document.RootElement.GetProperty("variableEntities")
        .EnumerateArray()
        .Where(item => item.ValueKind != JsonValueKind.Null)
        .ToDictionary(
            item => item.GetProperty("id").GetString()!,
            item => item.GetProperty("value").GetString());
}
```

Use the helper in both fallback and happy-path inbound assertions rather than duplicating the parsing logic.

- [ ] **Step 4: Run the focused tests to verify they pass**

Run:
```bash
dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "InboundFlow_AcquiresTargetLocationDuringRun_AndReleasesItAsOccupiedWithBoundPallet|InboundFlow_PreservesRequestedLocationVariables_WhenFallbackOccurs|InboundFlow_FallsBackToEmptyLocation_WhenPreferredRackIsOccupied"
```
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo add src/Backend.Demo.Tests/BackendDemoApiSmokeTest.cs
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo commit -m "test: cover inbound putaway variables"
```

### Task 4: Teach FlowView to explain the new inbound binding node

**Files:**
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.ts`
- Modify: `/Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel/src/lib/resourceSummary.test.ts`

- [ ] **Step 1: Write the failing FlowView test**

Add this test to `resourceSummary.test.ts`:

```ts
it('describes the inbound binding node as the occupancy transition', () => {
  const summary = buildExecutionResourceSummary(
    {
      id: 30,
      executableType: 1,
      flowId: 'db:inbound-basic:v1',
      acknowledged: true,
      status: 4,
      variableEntities: [
        { id: 'TargetLocationCode', value: '"RACK-A1"' },
        { id: 'InboundPalletCode', value: '"PLT-IN-1001"' },
        { id: 'SkuCode', value: '"SKU-001"' },
      ],
      resourceDetails: [],
      executableDetailModels: [],
    },
    { nodeId: 'BindLocationPallet' },
    locations,
    pallets,
    skus,
  )

  expect(summary?.transition?.before).toContain('RACK-A1 empty')
  expect(summary?.transition?.after).toContain('RACK-A1 occupied')
  expect(summary?.transition?.after).toContain('PLT-IN-1001')
})
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel && npm test -- --run src/lib/resourceSummary.test.ts`
Expected: FAIL because `BindLocationPallet` has no transition case yet.

- [ ] **Step 3: Add the minimal summary case**

Extend `toTransition(...)` in `resourceSummary.ts`:

```ts
case 'BindLocationPallet':
  return {
    before: context.resolvedLocationCode
      ? `${context.resolvedLocationCode} empty`
      : 'Target location empty',
    after: [
      context.resolvedLocationCode ? `${context.resolvedLocationCode} occupied` : 'Location occupied',
      context.palletCode ? `pallet ${context.palletCode} bound` : 'pallet bound',
    ].join(', '),
  }
```

Also make sure the inbound branch reads `InboundPalletId` / `InboundPalletCode` first when present:

```ts
const palletId = isInbound
  ? readNumberVariable(task, 'InboundPalletId') ?? resolvedLocation?.currentPalletId ?? null
  : readNumberVariable(task, 'SourcePalletId')
const palletCode = isInbound
  ? readStringVariable(task, 'InboundPalletCode') ?? pallet?.code ?? null
  : pallet?.code ?? null
```

Use `palletCode` in both the field list and transition context.

- [ ] **Step 4: Run the test to verify it passes**

Run: `cd /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel && npm test -- --run src/lib/resourceSummary.test.ts`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git -C /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel add \
  src/lib/resourceSummary.ts \
  src/lib/resourceSummary.test.ts
git -C /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel commit -m "feat: explain inbound binding resource transitions"
```

### Task 5: Run full verification

**Files:**
- No code changes expected

- [ ] **Step 1: Run backend targeted tests**

Run:
```bash
dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/src/Backend.Demo.Tests/Backend.Demo.Tests.csproj --no-restore -v minimal -m:1 /nr:false --filter "BindInboundLocationTaskTest|InboundFlow_AcquiresTargetLocationDuringRun_AndReleasesItAsOccupiedWithBoundPallet|InboundFlow_PreservesRequestedLocationVariables_WhenFallbackOccurs|InboundFlow_FallsBackToEmptyLocation_WhenPreferredRackIsOccupied"
```
Expected: PASS

- [ ] **Step 2: Run full backend build and tests**

Run:
```bash
dotnet build /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/Backend.Demo.slnx --no-restore -v minimal -m:1 /nr:false
dotnet test /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo/Backend.Demo.slnx --no-restore -v minimal -m:1 /nr:false
```
Expected: build succeeds, tests all pass

- [ ] **Step 3: Run FlowView verification**

Run:
```bash
cd /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel
npm test
npm run lint
npm run build
```
Expected: all commands succeed

- [ ] **Step 4: Commit any final fixups if verification forced them**

```bash
git -C /Users/qiping/Desktop/codes/work/Backend/.worktrees/feature-backend-demo status --short
git -C /Users/qiping/Desktop/codes/work/FlowView/.worktrees/feature-resource-summary-panel status --short
```
If either tree is dirty because of verification-only fixes, create a small final commit with a precise message before handing off.

## Self-Review

- Spec coverage: the plan covers the new node sequence, requested/resolved variables, function-console binding logic, automatic lock release assertions, and FlowView interpretation for `BindLocationPallet`.
- Placeholder scan: no `TODO`, `TBD`, or deferred code steps remain.
- Type consistency: all new names are consistent across tasks: `BindInboundLocationTask`, `RequestedTargetLocationId`, `RequestedTargetLocationCode`, `InboundPalletId`, `InboundPalletCode`, `ConveyorToInboundPort`, `StackCraneMoveToRack`, `BindLocationPallet`.
