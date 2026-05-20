#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"

if [[ -n "${FLOWENGINE_REPO:-}" ]]; then
  FLOWENGINE_REPO="$FLOWENGINE_REPO"
elif [[ -f "$ROOT_DIR/../FlowEngine/src/FlowEngine/src/FlowEngine.csproj" ]]; then
  FLOWENGINE_REPO="$ROOT_DIR/../FlowEngine"
else
  FLOWENGINE_REPO="$ROOT_DIR/../../../FlowEngine"
fi
PACKAGE_DIR="$ROOT_DIR/.nupkg/flowengine"

if [[ ! -f "$FLOWENGINE_REPO/src/FlowEngine/src/FlowEngine.csproj" ]]; then
  echo "FlowEngine repo not found at: $FLOWENGINE_REPO" >&2
  echo "Set FLOWENGINE_REPO=/absolute/path/to/FlowEngine and try again." >&2
  exit 1
fi

mkdir -p "$PACKAGE_DIR"
rm -f "$PACKAGE_DIR"/*.nupkg "$PACKAGE_DIR"/*.snupkg

projects=(
  "$FLOWENGINE_REPO/src/FlowEngine/src/FlowEngine.csproj"
  "$FLOWENGINE_REPO/src/FlowEngine.Execution/src/FlowEngine.Execution.csproj"
  "$FLOWENGINE_REPO/src/FlowEngine.Execution.Equipment/src/FlowEngine.Execution.Equipment.csproj"
  "$FLOWENGINE_REPO/src/FlowEngine.Server/src/FlowEngine.Server.csproj"
)

for project in "${projects[@]}"; do
  echo "Packing $(basename "$project")..."
  dotnet pack "$project" -c Release -o "$PACKAGE_DIR"
done

echo "FlowEngine preview packages written to $PACKAGE_DIR"
