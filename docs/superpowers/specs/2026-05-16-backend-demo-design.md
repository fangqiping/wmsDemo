# Backend Demo Design

Date: 2026-05-16

## Goal

Build a standalone backend demo in `/Users/qiping/Desktop/codes/work/Backend` that consumes local preview `FlowEngine` NuGet packages and proves that FlowEngine can serve as the application foundation for a small WMS/WCS-style service.

The first deliverable must:

- consume `FlowEngine` packages through a local NuGet source
- run on SQLite
- auto-create schema and seed demo data on startup
- model inbound and outbound order domains
- reuse FlowEngine data, manager, and controller infrastructure wherever practical
- provide simple flow execution for inbound and outbound orders
- simulate equipment through FlowEngine consoles and delayed operation tasks

## Scope

### In scope

- A demo ASP.NET Core backend
- Local NuGet consumption of `FlowEngine 0.1.0-preview.1`
- SQLite persistence
- Seeded demo master data and demo flows
- CRUD APIs for core master data and orders
- Order submission and flow-start actions
- Two simulated equipment consoles:
  - stack crane
  - conveyor
- Simple inbound and outbound flows driven by operation tasks
- Mapping order data into flow variables
- Swagger for local inspection and manual testing

### Out of scope

- Real PLC or WCS communication
- Inventory ledger accuracy and reservation math
- Wave planning, routing, optimization, or scheduler sophistication
- Multi-tenant support
- Frontend pages
- Production-grade authentication and authorization

## Workspace Layout

The backend demo workspace will be created under `/Users/qiping/Desktop/codes/work/Backend`.

Planned structure:

- `NuGet.config`
  - local package source pointing to `/private/tmp/flowengine-pack-verify`
- `Backend.Demo.sln`
- `src/Backend.Demo`
  - ASP.NET Core Web API host
- `src/Backend.Demo.Contracts`
  - request and response models for API surfaces
- `src/Backend.Demo.SampleData`
  - seed entities, draft flow payloads, and startup initialization helpers

This keeps the demo single-workspace and easy to run, while preserving clean module boundaries.

## Package Consumption Strategy

The demo backend must consume FlowEngine as if it were an external application, not by project reference.

`NuGet.config` will include a local source:

- `/private/tmp/flowengine-pack-verify`

The backend host will reference:

- `FlowEngine`
- `FlowEngine.Execution`
- `FlowEngine.Server`

Pinned package version:

- `0.1.0-preview.1`

This validates the preview packaging path and catches integration problems early.

## Technology Stack

- .NET 10
- ASP.NET Core Web API
- SQLite
- Swagger / OpenAPI
- Local NuGet source
- FlowEngine data, manager, execution, and server packages

## Domain Model

The first backend demo will define these entities.

### Master data

- `Warehouse`
  - `Id`
  - `Code`
  - `Name`

- `Location`
  - `Id`
  - `Code`
  - `Name`
  - `WarehouseId`
  - `LocationType`

- `Sku`
  - `Id`
  - `Code`
  - `Name`
  - `Spec`

### Orders

- `InboundOrder`
  - `Id`
  - `Code`
  - `Status`
  - `Source`
  - `FlowDefinitionCode`
  - `FlowVersionNumber`
  - `FlowTaskId`
  - `Remark`
  - `CreatedTime`
  - `UpdatedTime`
  - `CompletedTime`

- `InboundOrderLine`
  - `Id`
  - `InboundOrderId`
  - `SkuId`
  - `Quantity`
  - `TargetLocationId`

- `OutboundOrder`
  - `Id`
  - `Code`
  - `Status`
  - `Destination`
  - `FlowDefinitionCode`
  - `FlowVersionNumber`
  - `FlowTaskId`
  - `Remark`
  - `CreatedTime`
  - `UpdatedTime`
  - `CompletedTime`

- `OutboundOrderLine`
  - `Id`
  - `OutboundOrderId`
  - `SkuId`
  - `Quantity`
  - `SourceLocationId`

### Flow binding

- `FlowBinding`
  - `Id`
  - `BusinessType`
  - `FlowDefinitionCode`
  - `Enabled`

`BusinessType` will initially cover:

- `InboundOrder`
- `OutboundOrder`

## Entity and Persistence Strategy

The backend must lean on FlowEngine infrastructure rather than reinventing generic CRUD plumbing.

The demo will:

- define its domain entities in the backend assembly
- register SQLite through FlowEngine data extensions
- let FlowEngine discover and register entity managers
- consume `IManager<TKey, TEntity>` for business logic

This is the core architectural validation target: the app should mainly define entities and business actions, while FlowEngine provides the foundation beneath them.

## API Strategy

### CRUD controllers

These controllers should inherit from `FlowEngine.Server.WebApi.ApiController<...>` where possible:

- `WarehousesController`
- `LocationsController`
- `SkusController`
- `InboundOrdersController`
- `OutboundOrdersController`

Master-data controllers should be mostly standard CRUD.

### Order actions

Order controllers will extend beyond CRUD with business actions:

- `submit`
- `start-flow`
- `complete`
- `cancel`

For the first iteration, `submit` and `start-flow` are the most important.

### Contract separation

The backend will expose dedicated request and response models through `Backend.Demo.Contracts`.

Entity types should not be treated as public API contracts by default.

## Flow Integration

The demo backend will translate business orders into flow execution inputs.

Two order-to-flow conversions are required:

- `InboundOrder -> InboundFlow`
- `OutboundOrder -> OutboundFlow`

Order data that should become flow variables includes:

- order code
- order type
- sku lines
- quantities
- source and target locations
- warehouse and destination metadata

The backend will:

1. resolve the bound flow definition for the business type
2. ensure a published version exists
3. map order data into flow variables
4. start execution through FlowEngine runtime services
5. store resulting flow task ids back on the order

## Simulated Equipment Design

The first demo will simulate two device surfaces through FlowEngine consoles.

### `StackCraneConsole`

Responsibilities:

- inbound putaway
- outbound retrieval

Operation tasks:

- `PutawayOperationTask`
- `RetrieveOperationTask`

### `ConveyorConsole`

Responsibilities:

- receive inbound transfer
- move outbound transfer to dispatch

Operation tasks:

- `ReceiveOperationTask`
- `TransferOperationTask`

### Execution behavior

The first version will simulate device activity using `Task.Delay`.

Each operation task should:

- enter scheduled and started states through FlowEngine
- wait for a configurable delay
- complete successfully with lightweight output data

This is intentionally simple, but it preserves the correct FlowEngine execution shape and leaves room for later replacement with `FlowEngine.Execution.Equipment` communication-backed consoles.

## Demo Flows

The sample data project will seed two simple flows.

### Inbound flow

Minimal path:

1. receive inbound order
2. conveyor receives goods
3. stack crane performs putaway
4. mark inbound order completed

### Outbound flow

Minimal path:

1. receive outbound order
2. stack crane retrieves goods
3. conveyor transfers goods to outbound station
4. mark outbound order completed

These flows are intentionally small. The point is to demonstrate the business-to-flow bridge, runtime execution, and console orchestration.

## Startup and Seed Strategy

On application startup, the backend should:

1. ensure the SQLite database exists
2. apply schema creation or migrations
3. seed master data:
   - one warehouse
   - several locations
   - several skus
4. seed flow definitions and drafts
5. publish demo flow versions if they do not already exist
6. seed flow bindings

Startup should be idempotent so the service can be restarted without corrupting the demo environment.

## Error Handling

The backend should keep the first version straightforward:

- CRUD and business-action errors should use standard controller exception handling
- flow-start failures should return actionable API errors
- missing flow bindings or missing published flows should be reported clearly
- startup seed failures should fail fast with useful logs

Equipment simulation failures are not required in the first pass unless needed by the flow structure.

## Testing Strategy

The implementation should include focused tests for:

- order-to-flow mapping
- simulated console operation task execution
- startup seed idempotency where practical
- at least one inbound order happy path
- at least one outbound order happy path

Build and smoke verification should also include:

- restore
- build
- backend startup
- Swagger availability

## First Deliverable Definition of Done

The first backend demo is done when:

- the workspace builds cleanly
- the backend consumes local `FlowEngine` preview packages
- SQLite auto-initializes on startup
- seeded master data and flows exist
- inbound and outbound order APIs are available
- order submission can start a published flow
- simulated stack crane and conveyor tasks execute through FlowEngine
- Swagger can exercise the main demo path

## Risks and Mitigations

### Risk: FlowEngine package integration exposes registration gaps

Mitigation:

- consume packages through local NuGet from day one
- keep the first host thin and focused

### Risk: generic FlowEngine data discovery may not pick up demo entities as expected

Mitigation:

- align entity definitions with FlowEngine patterns
- validate manager resolution early with a minimal boot test

### Risk: flow execution startup may be too tightly coupled to seeded design-time entities

Mitigation:

- seed design-time tables together with business tables
- make missing flow-definition storage a startup concern, not a runtime surprise

### Risk: device simulation becomes too detailed too early

Mitigation:

- keep operation tasks delay-based
- preserve only the interfaces and task lifecycle shape needed for later growth
