# Port Resources and Acquire Node Interaction Design

## Goal

Make the backend demo a better validation rig for `FlowView` as a dedicated `FlowEngine` plugin by doing two things together:

- model inbound and outbound ports as first-class resources in the demo warehouse flows
- make acquire-style resource nodes behave like first-class interactive nodes in the execution graph

The target outcome is not just more realistic WMS/WCS flow data. The larger goal is to make `FlowView` better at:

- editing flows that contain resource-acquire nodes
- viewing resource-acquire execution state
- operating on acquire nodes with `Cancel`, `Retry`, and `Skip`

## Product Intent

The backend demo exists to pressure-test the parts of `FlowView` that matter most for a `FlowEngine`-native workflow plugin:

- graph editing against realistic resource-heavy flow definitions
- execution-graph inspection against real runtime data
- node-level control actions against actual `FlowEngine` state transitions

This means the demo should prioritize resource and node-operation realism over broad application surface area. A small backend with correct resource semantics is more valuable than a larger backend that does not expose real acquire-node behavior.

## Scope

This design covers:

- adding a dedicated `Port` resource entity
- seeding inbound and outbound demo ports
- routing inbound and outbound flows through ports explicitly
- preserving clear separation between resource locks and business state
- making acquire nodes in execution graphs clearly understandable and operable
- updating backend smoke tests and `FlowView` summaries around port and acquire behavior

This design does not cover:

- port balancing or routing optimization
- multi-port scheduling policies
- dedicated management pages for ports
- redesigning `FlowEngine` resource semantics
- broad new business workflows unrelated to node interaction validation

## Recommended Approach

Do both tracks in one implementation slice, but in this order:

1. add real `Port` resources to the backend demo flows
2. use those real acquire nodes to strengthen `FlowView` execution-graph behavior
3. only touch `FlowEngine` framework code if the existing runtime action API proves insufficient

This is preferred over doing only one side first:

- only building ports would improve business realism but would delay the plugin-facing payoff
- only polishing `FlowView` against today's flows would risk teaching the UI the wrong shape for future resource nodes

Doing both together keeps the demo aligned with the actual purpose of the system: validating `FlowView` as a first-class `FlowEngine` companion.

## Domain Model

Add a new `Port` entity rather than overloading `Location`.

Required fields:

- `Id`
- `Code`
- `Name`
- `Enabled`
- `Acquired`
- `PortType`
- `Status`
- `CurrentPalletId`
- `CurrentPallet`
- `WarehouseId`

### PortType

- `Inbound`
- `Outbound`

### PortStatus

- `Idle`
- `Reserved`
- `Occupied`

### Resource Semantics

`Port` participates in the same FlowEngine resource lifecycle as `Location` and `Pallet`:

- `Acquired` represents runtime lock state
- `Status` represents business state
- `CurrentPalletId` represents the pallet currently staged at that port

This separation is important:

- a port may be acquired without yet being business-occupied
- a port may transition back to `Idle` before or after lock release depending on business flow

## Inbound Flow Changes

Replace the current inbound chain with an explicit port handoff:

1. `AcquireInboundPort`
2. `ConveyorToInboundPort`
3. `OccupyInboundPort`
4. `AcquireTargetLocation`
5. `ResolveTargetLocation`
6. `StackCraneMoveToRack`
7. `BindLocationPallet`

### Node Responsibilities

#### `AcquireInboundPort`

- acquire an idle inbound port resource
- output `InboundPortId` and `InboundPortCode`

#### `ConveyorToInboundPort`

- physical-only conveyor transfer
- no inventory mutation

#### `OccupyInboundPort`

- explicit business mutation in a `FunctionConsole` task
- marks the inbound port as `Occupied`
- may attach transient staging state needed for later explanation

#### `AcquireTargetLocation`

- acquire a compatible empty rack location
- preserve requested vs resolved target location semantics

#### `ResolveTargetLocation`

- surface resolved target location values as flow variables

#### `StackCraneMoveToRack`

- physical-only crane movement from inbound port to rack

#### `BindLocationPallet`

- clear inbound port business occupancy
- set inbound port pallet reference to `null`
- create or confirm the inbound pallet
- mark target rack `Occupied`
- bind the pallet to the rack

### Inbound End State

After the flow completes:

- inbound port lock released
- inbound port status `Idle`
- inbound port `CurrentPalletId = null`
- target location lock released
- target location status `Occupied`
- target location holds the pallet

## Outbound Flow Changes

Make outbound explicitly symmetric:

1. `AcquireSourceLocation`
2. `ResolveSourceLocation`
3. `AcquireSourcePallet`
4. `AcquireOutboundPort`
5. `StackCraneMoveToOutboundPort`
6. `BindOutboundPort`
7. `ConveyorFromOutboundPort`
8. `ReleaseOutboundPort`

### Node Responsibilities

#### `AcquireOutboundPort`

- acquire an idle outbound port resource
- output `OutboundPortId` and `OutboundPortCode`

#### `StackCraneMoveToOutboundPort`

- physical-only crane movement from rack to outbound port

#### `BindOutboundPort`

- clear source rack occupancy
- unbind pallet from source location
- mark outbound port `Occupied`
- assign the pallet to the outbound port

#### `ConveyorFromOutboundPort`

- physical-only conveyor transfer from outbound port to the shipping side

#### `ReleaseOutboundPort`

- clear outbound port business state
- set port status back to `Idle`
- remove `CurrentPalletId`
- disable the pallet or mark it out of active warehouse scope

### Outbound End State

After the flow completes:

- source location lock released
- source location status `Empty`
- outbound port lock released
- outbound port status `Idle`
- outbound port no longer references a pallet

## Acquire Node Interaction Design

This slice treats acquire nodes as first-class interactive nodes in `FlowView`.

The important node families are:

- `AcquireTargetLocation`
- `AcquireSourceLocation`
- `AcquireInboundPort`
- `AcquireOutboundPort`
- `AcquireSourcePallet`

### Execution Graph Expectations

For acquire nodes, `FlowView` should:

- clearly identify them as resource-acquire nodes
- show which resource type they target
- show resolved resource code or identifier when available
- explain why the node can or cannot currently be acted on

### Actions

The UI should continue to trust `FlowEngine` `availableActions` first. The frontend may still apply narrow safety guards when the parent flow is already terminal or the node has clearly been acknowledged.

Acquire nodes should support the same interaction model as other executable nodes when runtime data permits:

- `Cancel`
- `Retry`
- `Skip`

### Post-Action Navigation

After actions, the execution graph should guide the operator to the next useful place:

- after `Retry`, auto-focus the replacement acquire node
- after `Skip`, auto-focus the next unacknowledged successor node
- after `Cancel`, collapse stale actions and show terminal guidance if no further action is possible

If a skipped acquire node was the last actionable node for a subflow or flow, `FlowView` should show the resulting completed state instead of searching for a nonexistent next node.

## Backend Demo Testing Strategy

The backend must prove both resource correctness and node-operability.

### Resource and Rule Tests

Add focused tests for:

- acquiring idle inbound ports
- acquiring idle outbound ports
- rejecting disabled or non-idle ports
- explicit function tasks that occupy, bind, and release port state

### Flow Smoke Tests

Inbound smoke coverage should prove:

- inbound port is acquired
- inbound port becomes occupied after conveyor handoff
- target location is still resolved correctly
- final rack binding clears the inbound port and occupies the rack

Outbound smoke coverage should prove:

- outbound port is acquired
- source rack is cleared only after outbound binding
- outbound port becomes occupied before final conveyor release
- final release returns the port to `Idle`

### Acquire Action Smoke Coverage

Add HTTP smoke tests around acquire-node control operations where runtime timing allows:

- cancel acquire node
- retry canceled acquire node
- skip canceled acquire node

The purpose is not only to prove backend control APIs. It is to guarantee that `FlowView` is exercising real resource-node transitions rather than mock semantics.

## FlowView Testing Strategy

The frontend should prove both explanation quality and action continuity.

### Resource Summary Tests

Update summary logic so it can explain:

- inbound port acquisition and occupancy
- outbound port acquisition and occupancy
- outbound port release
- acquire-node requested vs resolved resource values where applicable

### Node Action Tests

Extend helper tests to prove:

- acquire nodes honor server-provided `availableActions`
- retry focuses the correct replacement acquire node
- skip focuses the correct successor node
- stale acquire-node actions disappear once the parent flow is terminal

## FlowEngine Touch Points

The default assumption is that existing `FlowEngine` runtime action support is enough.

Only modify `FlowEngine` itself if implementation reveals a concrete gap such as:

- insufficient `availableActions` detail for acquire nodes
- unstable runtime action errors that prevent accurate frontend behavior
- missing detail fields required to distinguish resource-acquire nodes from generic nodes

If framework changes are needed, keep them as small as possible and aligned with the existing runtime-action API shape.

## Success Criteria

This slice is successful when all of the following are true:

- inbound and outbound flows both stage through explicit `Port` resources
- locks and business state remain clearly separated
- execution graphs contain real acquire nodes for ports, locations, and pallets
- acquire nodes can be inspected and acted on in `FlowView`
- `Retry` and `Skip` navigation behaves correctly for acquire nodes
- backend smoke tests prove the resource lifecycle
- `FlowView` becomes a better environment for iterating on `FlowEngine` flow editing and runtime-node control
