# Inbound Putaway Flow Design

## Goal

Replace the current `inbound-basic` demo flow with a warehouse flow that more closely matches a real inbound putaway sequence:

1. conveyor transfer to the inbound port
2. acquire an empty target rack location
3. stack crane transfer from the inbound port to the rack
4. bind the pallet and location through business logic
5. finish the flow and release the location lock

The design keeps **resource locking** and **business state** separate:

- the flow acquires and holds a `Location` resource lock while work is in progress
- the location becomes `Occupied` only when the business binding step succeeds
- the lock is released automatically when the flow completes

## Scope

This design covers:

- replacing the existing `inbound-basic` flow definition
- adding one new `FunctionConsole` business node for inbound binding
- adding any input/output variables required to make the flow observable and stable
- updating backend tests that assert inbound behavior
- keeping FlowView resource summaries compatible with the new flow

This design does not cover:

- changes to outbound flow behavior
- new inventory accounting tables
- device communication beyond the existing simulated consoles
- front-end page redesign

## Recommended Approach

Use a **four-node inbound flow** that separates physical transfer from business state mutation:

1. `ConveyorToInboundPort`
2. `AcquireTargetLocation`
3. `StackCraneMoveToRack`
4. `BindLocationPallet`

This approach is preferred over folding business state into the stack crane operation because it keeps the system behavior legible:

- conveyor and stack crane nodes represent physical actions
- function nodes represent business resolution and state mutation
- resource lock acquisition stays explicit and inspectable
- FlowView can show a clearer execution narrative

## Flow Shape

### Node 1: `ConveyorToInboundPort`

- console: `ConveyorConsole`
- purpose: transfer the inbound load from the source station to the inbound port
- effect on warehouse state: none
- effect on resources: none

This node exists to make the physical sequence explicit. It does not acquire a rack location and does not mutate location occupancy.

### Node 2: `AcquireTargetLocation`

- console: `FunctionConsole`
- operation task: existing empty rack acquire rule
- purpose: lock an empty rack location for the inbound order
- selection logic:
  - prefer the requested target location from the order line
  - if the requested location is not eligible, fall back to the empty-rack selection rule

Outputs:

- `TargetLocationId`

Resource outputs:

- acquired `Location`

This node is the point where the flow takes temporary ownership of the target rack.

### Node 3: `StackCraneMoveToRack`

- console: `StackCraneConsole`
- purpose: move the inbound load from the inbound port to the resolved rack location
- effect on warehouse state: none
- effect on resources: continues using the previously acquired location lock

This node should represent only the physical move. It should not create pallets or mark the location occupied.

### Node 4: `BindLocationPallet`

- console: `FunctionConsole`
- purpose: commit the inbound business state after physical transfer completes

Responsibilities:

- validate that the resolved target location still exists
- create a pallet for the inbound order
- bind the pallet to the SKU
- set the location status to `Occupied`
- assign `CurrentPalletId`
- write back pallet-related output variables

Outputs:

- `InboundPalletId`
- `InboundPalletCode`
- `Status`

The flow then ends naturally. Because the acquired `Location` is a flow resource, the framework releases the lock automatically after completion.

## Variable Model

### Existing variables kept

- `OrderCode`
- `SourceLocationCode`
- `WarehouseId`
- `SkuId`
- `SkuCode`
- `TargetLocationCode`
- `TargetLocationId`
- `Status`

### New or clarified variables

- `RequestedTargetLocationCode`
- `RequestedTargetLocationId`
- `InboundPalletId`
- `InboundPalletCode`

### Semantics

- `RequestedTargetLocation*` captures the business request from the order
- `TargetLocation*` captures the resolved location after acquire
- pallet variables are empty until `BindLocationPallet` succeeds

This distinction is important for both debugging and UI summary rendering, especially when the flow falls back from the requested location to a different eligible rack.

## Function Console Logic

Add a dedicated function task for the final inbound binding step. The task name for this slice is `BindInboundLocationTask`.

### Inputs

- `OrderCode`
- `TargetLocationId`
- `TargetLocationCode`
- `SkuId`
- `SkuCode`

### Outputs

- `InboundPalletId`
- `InboundPalletCode`
- `Status`

### Behavior

1. Load the target location by `TargetLocationId`
2. Validate that the location exists
3. Create a pallet using a stable demo code format:
   - `PLT-{OrderCode}`
4. Set pallet fields:
   - `Enabled = true`
   - `SkuId = input SkuId`
   - `Quantity = 1`
5. Update the location:
   - `Status = Occupied`
   - `CurrentPalletId = new pallet id`
6. Return the new pallet data and a user-facing completion status

This node is intentionally idempotent only within the normal happy-path flow. It does not need to solve full replay semantics for this demo slice.

## Resource Semantics

The desired behavior is:

- during `AcquireTargetLocation`, the chosen rack location is locked
- during conveyor and stack crane execution, that lock remains held
- during `BindLocationPallet`, the location business state changes from `Empty` to `Occupied`
- after the flow finishes, the lock is released
- after the flow finishes, the location remains `Occupied`

That means:

- **lock released** does not mean **location empty**
- **occupied** describes inventory state
- **acquired** describes transient execution ownership

This is the same separation already used in the outbound path and should remain consistent across the demo.

## FlowView Impact

FlowView should not need a structural redesign for this slice, but the new variable names and node names should remain consumable by the existing resource summary logic.

Expected effects:

- orders page continues to show requested vs resolved location
- execution graph shows a clearer final business node:
  - `BindLocationPallet`
- resource summaries can show:
  - pallet created during inbound
  - location transitions to `Occupied`
  - lock released after flow completion

If the UI currently keys resource explanations off node ids, it should add one case for `BindLocationPallet`.

## Testing Strategy

### Backend unit/integration tests

Add coverage for the new function task:

- creates a pallet
- binds the pallet to the expected SKU
- updates the location to `Occupied`
- writes `CurrentPalletId`
- returns pallet output variables

### Backend API / flow smoke tests

Update inbound flow smoke tests to assert the new behavior:

- the inbound task graph contains:
  - `ConveyorToInboundPort`
  - `AcquireTargetLocation`
  - `StackCraneMoveToRack`
  - `BindLocationPallet`
- after completion:
  - target location status is `Occupied`
  - location lock is released
  - a pallet exists and is associated with the target location

### Frontend regression tests

Keep or extend resource summary tests so they continue to validate:

- requested vs resolved location
- lock state
- pallet visibility
- inbound completion summary after the final binding node

## Success Criteria

This slice is successful when:

- `inbound-basic` follows the new four-step sequence
- the target rack location is locked before the crane move
- the location becomes `Occupied` only in the final function node
- a pallet is created and bound to the inbound SKU
- the location lock is released when the flow finishes
- FlowView can still explain the resource story for inbound orders and execution nodes

## Risks and Mitigations

### Risk: old tests assume the previous node list

Mitigation:

- update tests to assert the new flow order explicitly

### Risk: resource summaries stop recognizing the new final node

Mitigation:

- add one new summary case keyed to `BindLocationPallet`

### Risk: binding logic accidentally duplicates crane business work

Mitigation:

- keep stack crane tasks purely physical
- keep all location occupancy mutation in the function task
