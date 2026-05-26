# Port Resource Design

## Goal

Model inbound and outbound ports as first-class FlowEngine resources so the demo warehouse execution chain can treat ports with the same rigor as rack locations and pallets.

The target outcome is:

- inbound and outbound ports are explicit domain entities
- flows can acquire and release ports
- ports have independent business state and resource lock state
- inbound and outbound flows both show realistic handoff through a port

For this slice, port occupancy changes remain explicit business mutations in function tasks. Conveyor and stack crane tasks stay physical-only.

## Scope

This design covers:

- adding a new `Port` resource entity
- defining `PortType` and `PortStatus`
- seeding inbound and outbound demo ports
- extending both inbound and outbound flows to use ports explicitly
- adding or adjusting function logic that mutates port business state
- updating smoke tests and FlowView summary logic

This design does not cover:

- multiple-port routing optimization
- queue depth or scheduling heuristics
- dedicated port management pages
- advanced WCS dispatch logic

## Recommended Approach

Use a single `Port` entity with:

- `PortType = Inbound | Outbound`
- `PortStatus = Idle | Reserved | Occupied`

Treat `Port` as a `[Resource]` so it participates in the same acquire/release mechanism already used by `Location` and `Pallet`.

This is preferred over reusing `Location` because ports and rack locations do not carry the same business meaning:

- rack locations are storage
- ports are handoff or staging positions

Keeping them separate preserves semantic clarity and avoids stuffing too many competing meanings into `LocationType`.

## Domain Model

Add a new `Port` entity with these fields:

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

### Resource semantics

- `Acquired`
  - transient FlowEngine lock state
- `Status`
  - business state for the port itself
- `CurrentPalletId`
  - pallet currently staged at the port, if any

### Enums

`PortType`
- `Inbound`
- `Outbound`

`PortStatus`
- `Idle`
- `Reserved`
- `Occupied`

## Resource Rules

Add two minimal acquire rules:

### Inbound port rule

Eligible when:

- `PortType == Inbound`
- `Status == Idle`
- `Enabled == true`

### Outbound port rule

Eligible when:

- `PortType == Outbound`
- `Status == Idle`
- `Enabled == true`

The first slice does not need priority or load balancing. One available port is enough.

## Inbound Flow Changes

Replace the current inbound chain with an explicit port handoff:

1. `AcquireInboundPort`
2. `ConveyorToInboundPort`
3. `AcquireTargetLocation`
4. `ResolveTargetLocation`
5. `StackCraneMoveToRack`
6. `BindLocationPallet`

### Node semantics

#### `AcquireInboundPort`

- `FunctionConsole`
- acquires an `Inbound` port resource
- outputs resolved `InboundPortId`

#### `ConveyorToInboundPort`

- `ConveyorConsole`
- physically moves goods from source station to the acquired inbound port
- business effect:
  - inbound port becomes `Occupied`
  - inbound port binds the staged pallet or placeholder handoff state

The status mutation can happen either in a dedicated function node after the conveyor task or by a tiny function helper immediately adjacent to it. For this demo slice, keeping it explicit as business logic is preferred if code stays manageable.
In this slice, use a dedicated function helper immediately after the conveyor step so port occupancy remains explicit and testable.

#### `AcquireTargetLocation`

- same empty-rack selection rule as today
- still separates requested vs resolved rack location

#### `StackCraneMoveToRack`

- `StackCraneConsole`
- physical movement from inbound port to rack
- no direct inventory mutation

#### `BindLocationPallet`

- `FunctionConsole`
- clears inbound port occupancy
- creates or confirms the inbound pallet
- marks location `Occupied`
- binds pallet to rack location

### Inbound end state

After flow completion:

- inbound port lock released
- inbound port status `Idle`
- inbound port `CurrentPalletId = null`
- target location lock released
- target location status `Occupied`
- target location holds the pallet

## Outbound Flow Changes

Make outbound symmetric with inbound:

1. `AcquireSourceLocation`
2. `ResolveSourceLocation`
3. `AcquireSourcePallet`
4. `AcquireOutboundPort`
5. `StackCraneMoveToOutboundPort`
6. `BindOutboundPort`
7. `ConveyorFromOutboundPort`
8. `ReleaseOutboundPort`

### Node semantics

#### `AcquireOutboundPort`

- `FunctionConsole`
- acquires an `Outbound` port resource
- outputs resolved `OutboundPortId`

#### `StackCraneMoveToOutboundPort`

- `StackCraneConsole`
- physical movement from rack location to outbound port

#### `BindOutboundPort`

- `FunctionConsole`
- clears source rack occupancy
- unbinds pallet from location
- marks outbound port `Occupied`
- assigns pallet to outbound port

#### `ConveyorFromOutboundPort`

- `ConveyorConsole`
- moves goods from outbound port to the shipping side

#### `ReleaseOutboundPort`

- `FunctionConsole`
- clears outbound port state
- sets status back to `Idle`
- drops `CurrentPalletId`
- disables the pallet or marks it out of warehouse scope

### Outbound end state

After flow completion:

- source location lock released
- source location status `Empty`
- outbound port lock released
- outbound port status `Idle`
- outbound port `CurrentPalletId = null`
- pallet is no longer active in warehouse storage

## Flow Variables

### Inbound additions

- `InboundPortId`
- `InboundPortCode`

### Outbound additions

- `OutboundPortId`
- `OutboundPortCode`

These sit alongside the existing requested/resolved location and pallet variables.

## Seed Data

Seed at least:

- one inbound port: `IP-01`
- one outbound port: `OP-01`

Each should start as:

- `Enabled = true`
- `Acquired = false`
- `Status = Idle`
- `CurrentPalletId = null`

They should belong to the same demo warehouse as the rest of the sample data.

## Function Logic

This slice will add small function tasks such as:

- `AcquireInboundPortTask`
- `AcquireOutboundPortTask`
- `BindOutboundPortTask`
- `ReleaseOutboundPortTask`
- `ReserveInboundPortTask`

The preferred rule is:

- device tasks stay physical
- function tasks own business-state mutation

## FlowView Impact

FlowView should remain structurally the same, but its summary logic should learn the new node ids and port vocabulary.

Expected updates:

- order resource summary can show inbound or outbound port when relevant
- execution summary can explain:
  - `AcquireInboundPort`
  - `ConveyorToInboundPort`
  - `AcquireOutboundPort`
  - `StackCraneMoveToOutboundPort`
  - `BindOutboundPort`
  - `ReleaseOutboundPort`

This is an explanation-layer change, not a layout change.

## Testing Strategy

### Backend registration and rules

Add coverage for:

- `Port` entity registration in the FlowEngine data layer
- inbound port acquire rule
- outbound port acquire rule

### Backend flow smoke tests

Inbound:

- port is acquired during flow
- port becomes occupied during handoff
- port returns to idle after binding completes
- target location ends as occupied with pallet bound

Outbound:

- outbound port is acquired during flow
- port becomes occupied after crane handoff
- source location becomes empty
- outbound port returns to idle after conveyor release

### Frontend regression tests

FlowView tests should validate:

- resource summaries can describe port-related node transitions
- inbound and outbound execution summaries mention port state when appropriate

## Success Criteria

This slice is successful when:

- `Port` exists as a resource object distinct from `Location`
- inbound and outbound flows both explicitly hand off through ports
- port business state and resource lock state remain separate
- ports return to `Idle` after successful completion
- FlowView can explain the new port transitions

## Risks and Mitigations

### Risk: too much port mutation leaks into device tasks

Mitigation:

- keep port occupancy and pallet binding in function tasks
- keep conveyor and stack crane tasks focused on movement

### Risk: inbound and outbound become asymmetric again

Mitigation:

- implement both sides in the same slice
- use mirrored node naming and end-state assertions

### Risk: resource summary logic becomes brittle

Mitigation:

- extend the existing node-id-based summary mapping with explicit port cases
- cover those cases with unit tests
