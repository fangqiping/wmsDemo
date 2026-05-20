# Backend Demo

`Backend.Demo` is a WMS + WCS backend sample built on top of `FlowEngine` preview packages. It ships with:

- inbound and outbound order APIs
- SQLite persistence with automatic migrations on startup
- seeded warehouse, location, SKU, and flow definitions
- demo stack crane and conveyor consoles backed by delayed operation tasks
- Swagger and smoke tests for the end-to-end flow lifecycle

## Requirements

- .NET SDK 10.0
- a local checkout of the `FlowEngine` repo

By default, the helper script expects this layout:

```text
work/
  Backend/
  FlowEngine/
```

If your `FlowEngine` checkout lives elsewhere, set `FLOWENGINE_REPO` before running the script.

## Quick Start

1. Build local `FlowEngine` preview packages:

```bash
./scripts/pack-flowengine-preview.sh
```

2. Restore and run the backend:

```bash
dotnet restore
dotnet run --project src/Backend.Demo --urls http://127.0.0.1:5086
```

3. Open Swagger:

```text
http://127.0.0.1:5086/swagger
```

The app creates and migrates `backend-demo.db` automatically, then seeds:

- warehouse `WH-01`
- locations such as `INBOUND-01`, `RACK-A1`, `OUTBOUND-01`
- sample SKUs
- flow definitions `inbound-basic` and `outbound-basic`

## Common Commands

Run the test suite:

```bash
dotnet test Backend.Demo.slnx -v minimal
```

Rebuild local FlowEngine packages after framework changes:

```bash
./scripts/pack-flowengine-preview.sh
dotnet restore
```

## Project Layout

```text
src/
  Backend.Demo/             ASP.NET Core host, controllers, consoles, seeding
  Backend.Demo.Contracts/   API contracts
  Backend.Demo.SampleData/  sample draft and published flow payloads
  Backend.Demo.Tests/       smoke and initialization tests
```

## Frontend Pairing

The companion frontend lives in the `FlowView` repo and talks to this API at `http://127.0.0.1:5086` by default.
