using System.Text.Json;
using Backend.Demo.Domain;
using Backend.Demo.Resource;
using FlowEngine.Execution.Design;
using FlowEngine.Execution.Consoles;
using FlowEngine.Execution.Resource;

namespace Backend.Demo.Seeding;

internal static class BackendDemoFlowSeeds {
    public const string InboundFlowCode = "inbound-basic";
    public const string OutboundFlowCode = "outbound-basic";

    public static SaveFlowDraftRequest CreateInboundDraft(int revision) {
        return new SaveFlowDraftRequest {
            Code = InboundFlowCode,
            Name = "Inbound Basic Flow",
            Description = "Receive by conveyor, then store by stack crane.",
            Revision = revision,
            DraftDocumentJson = BuildInboundDraftDocumentJson(),
            UpdatedBy = "backend-demo"
        };
    }

    public static SaveFlowDraftRequest CreateOutboundDraft(int revision) {
        return new SaveFlowDraftRequest {
            Code = OutboundFlowCode,
            Name = "Outbound Basic Flow",
            Description = "Retrieve by stack crane, then deliver by conveyor.",
            Revision = revision,
            DraftDocumentJson = BuildOutboundDraftDocumentJson(),
            UpdatedBy = "backend-demo"
        };
    }

    private static string BuildInboundDraftDocumentJson() {
        return JsonSerializer.Serialize(new {
            id = "InboundBasicFlow",
            variables = new object[] {
                Variable("OrderCode", "string", "input", string.Empty),
                Variable("SourceLocationCode", "string", "input", string.Empty),
                Variable("WarehouseId", "int", "input", 0),
                Variable("InboundPortCode", "string", "input", string.Empty),
                Variable("InboundPortId", "int", "output", 0),
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
                    id: "AcquireInboundPort",
                    consoleId: FunctionConsole.NAME,
                    operationTaskType: typeof(AcquireInboundPortTask).FullName!,
                    inputs: new object[] {
                        Input("WarehouseId", nameof(AcquireInboundPortTask.WarehouseId))
                    },
                    outputs: new object[] {
                        Output(nameof(AcquireInboundPortTask.InboundPortId), "InboundPortId"),
                        Output(nameof(AcquireInboundPortTask.InboundPortCode), "InboundPortCode")
                    },
                    resourceOutputs: new object[] {
                        ResourceOutput(nameof(AcquireInboundPortTask.InboundPortId), typeof(Port).FullName!)
                    }),
                Operation(
                    id: "ConveyorToInboundPort",
                    consoleId: ConveyorConsole.NAME,
                    operationTaskType: typeof(ConveyorTransferOperationTask).FullName!,
                    inputs: new object[] {
                        Input("OrderCode", nameof(ConveyorTransferOperationTask.OrderCode)),
                        Input("SourceLocationCode", nameof(ConveyorTransferOperationTask.FromLocationCode)),
                        Input("InboundPortCode", nameof(ConveyorTransferOperationTask.ToLocationCode))
                    },
                    outputs: Array.Empty<object>()),
                Operation(
                    id: "OccupyInboundPort",
                    consoleId: FunctionConsole.NAME,
                    operationTaskType: typeof(OccupyInboundPortTask).FullName!,
                    inputs: new object[] {
                        Input("InboundPortId", nameof(OccupyInboundPortTask.InboundPortId))
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
                        Input("InboundPortId", nameof(BindInboundLocationTask.InboundPortId)),
                        Input("SkuId", nameof(BindInboundLocationTask.SkuId)),
                        Input("SkuCode", nameof(BindInboundLocationTask.SkuCode)),
                        Input("TargetLocationId", nameof(BindInboundLocationTask.TargetLocationId)),
                        Input("TargetLocationCode", nameof(BindInboundLocationTask.TargetLocationCode))
                    },
                    outputs: new object[] {
                        Output(nameof(BindInboundLocationTask.InboundPalletId), "InboundPalletId"),
                        Output(nameof(BindInboundLocationTask.InboundPalletCode), "InboundPalletCode"),
                        Output(nameof(BindInboundLocationTask.CompletionStatus), "Status")
                    })
            },
            routes = new object[] {
                Path("Root", new[] { "AcquireInboundPort" }),
                Path("AcquireInboundPort", new[] { "ConveyorToInboundPort" }),
                Path("ConveyorToInboundPort", new[] { "OccupyInboundPort" }),
                Path("OccupyInboundPort", new[] { "AcquireTargetLocation" }),
                Path("AcquireTargetLocation", new[] { "ResolveTargetLocation" }),
                Path("ResolveTargetLocation", new[] { "StackCraneMoveToRack" }),
                Path("StackCraneMoveToRack", new[] { "BindLocationPallet" })
            }
        });
    }

    private static string BuildOutboundDraftDocumentJson() {
        return JsonSerializer.Serialize(new {
            id = "OutboundBasicFlow",
            variables = new object[] {
                Variable("OrderCode", "string", "input", string.Empty),
                Variable("WarehouseId", "int", "input", 0),
                Variable("OutboundPortCode", "string", "input", string.Empty),
                Variable("OutboundPortId", "int", "output", 0),
                Variable("RequestedSourceLocationCode", "string", "input", string.Empty),
                Variable("SourceLocationCode", "string", "input", string.Empty),
                Variable("SourceLocationId", "int", "input", 0),
                Variable("TargetLocationCode", "string", "input", string.Empty),
                Variable("SourcePalletId", "int", "input", 0),
                Variable("SkuId", "int", "input", 0),
                Variable("SkuCode", "string", "input", string.Empty),
                Variable("Status", "string", "output", "Draft")
            },
            nodes = new object[] {
                Operation(
                    id: "AcquireSourceLocation",
                    consoleId: FunctionConsole.NAME,
                    operationTaskType: typeof(AcquireOccupiedRackLocationTask).FullName!,
                    inputs: new object[] {
                        Input("WarehouseId", nameof(AcquireOccupiedRackLocationTask.WarehouseId)),
                        Input("SkuId", nameof(AcquireOccupiedRackLocationTask.SkuId)),
                        Input("SourceLocationId", nameof(AcquireOccupiedRackLocationTask.PreferredLocationId))
                    },
                    outputs: new object[] {
                        Output(nameof(AcquireOccupiedRackLocationTask.Acquired), "SourceLocationId")
                    },
                    resourceOutputs: new object[] {
                        ResourceOutput(nameof(AcquireOccupiedRackLocationTask.Acquired), typeof(Location).FullName!)
                    }),
                Operation(
                    id: "ResolveSourceLocation",
                    consoleId: FunctionConsole.NAME,
                    operationTaskType: typeof(LoadLocationSnapshotOperationTask).FullName!,
                    inputs: new object[] {
                        Input("SourceLocationId", nameof(LoadLocationSnapshotOperationTask.LocationId))
                    },
                    outputs: new object[] {
                        Output(nameof(LoadLocationSnapshotOperationTask.LocationCode), "SourceLocationCode"),
                        Output(nameof(LoadLocationSnapshotOperationTask.CurrentPalletId), "SourcePalletId")
                    }),
                Operation(
                    id: "AcquireSourcePallet",
                    consoleId: FunctionConsole.NAME,
                    operationTaskType: typeof(IdAcquireResourceTask<int, Pallet>).FullName!,
                    inputs: new object[] {
                        Input("SourcePalletId", nameof(IdAcquireResourceTask<int, Pallet>.ResourceId))
                    },
                    outputs: new object[] {
                        Output(nameof(IdAcquireResourceTask<int, Pallet>.Acquired), "SourcePalletId")
                    },
                    resourceOutputs: new object[] {
                        ResourceOutput(nameof(IdAcquireResourceTask<int, Pallet>.Acquired), typeof(Pallet).FullName!)
                    }),
                Operation(
                    id: "AcquireOutboundPort",
                    consoleId: FunctionConsole.NAME,
                    operationTaskType: typeof(AcquireOutboundPortTask).FullName!,
                    inputs: new object[] {
                        Input("WarehouseId", nameof(AcquireOutboundPortTask.WarehouseId))
                    },
                    outputs: new object[] {
                        Output(nameof(AcquireOutboundPortTask.OutboundPortId), "OutboundPortId"),
                        Output(nameof(AcquireOutboundPortTask.OutboundPortCode), "OutboundPortCode")
                    },
                    resourceOutputs: new object[] {
                        ResourceOutput(nameof(AcquireOutboundPortTask.OutboundPortId), typeof(Port).FullName!)
                    }),
                Operation(
                    id: "StackCraneMoveToOutboundPort",
                    consoleId: StackCraneConsole.NAME,
                    operationTaskType: typeof(StackCraneRetrieveOperationTask).FullName!,
                    inputs: new object[] {
                        Input("OrderCode", nameof(StackCraneRetrieveOperationTask.OrderCode)),
                        Input("SkuCode", nameof(StackCraneRetrieveOperationTask.SkuCode)),
                        Input("SourceLocationCode", nameof(StackCraneRetrieveOperationTask.SourceLocationCode)),
                        Input("SourceLocationId", nameof(StackCraneRetrieveOperationTask.SourceLocationId)),
                        Input("SourcePalletId", nameof(StackCraneRetrieveOperationTask.SourcePalletId)),
                        Input("OutboundPortCode", nameof(StackCraneRetrieveOperationTask.OutboundPortCode))
                    },
                    outputs: Array.Empty<object>()),
                Operation(
                    id: "BindOutboundPort",
                    consoleId: FunctionConsole.NAME,
                    operationTaskType: typeof(BindOutboundPortTask).FullName!,
                    inputs: new object[] {
                        Input("SourceLocationId", nameof(BindOutboundPortTask.SourceLocationId)),
                        Input("SourcePalletId", nameof(BindOutboundPortTask.SourcePalletId)),
                        Input("OutboundPortId", nameof(BindOutboundPortTask.OutboundPortId))
                    },
                    outputs: Array.Empty<object>()),
                Operation(
                    id: "ConveyorFromOutboundPort",
                    consoleId: ConveyorConsole.NAME,
                    operationTaskType: typeof(ConveyorTransferOperationTask).FullName!,
                    inputs: new object[] {
                        Input("OrderCode", nameof(ConveyorTransferOperationTask.OrderCode)),
                        Input("OutboundPortCode", nameof(ConveyorTransferOperationTask.FromLocationCode)),
                        Input("TargetLocationCode", nameof(ConveyorTransferOperationTask.ToLocationCode))
                    },
                    outputs: new object[] {
                        Output(nameof(ConveyorTransferOperationTask.CompletionMessage), "Status")
                    }),
                Operation(
                    id: "ReleaseOutboundPort",
                    consoleId: FunctionConsole.NAME,
                    operationTaskType: typeof(ReleaseOutboundPortTask).FullName!,
                    inputs: new object[] {
                        Input("OutboundPortId", nameof(ReleaseOutboundPortTask.OutboundPortId)),
                        Input("SourcePalletId", nameof(ReleaseOutboundPortTask.SourcePalletId))
                    },
                    outputs: Array.Empty<object>())
            },
            routes = new object[] {
                Path("Root", new[] { "AcquireSourceLocation" }),
                Path("AcquireSourceLocation", new[] { "ResolveSourceLocation" }),
                Path("ResolveSourceLocation", new[] { "AcquireSourcePallet" }),
                Path("AcquireSourcePallet", new[] { "AcquireOutboundPort" }),
                Path("AcquireOutboundPort", new[] { "StackCraneMoveToOutboundPort" }),
                Path("StackCraneMoveToOutboundPort", new[] { "BindOutboundPort" }),
                Path("BindOutboundPort", new[] { "ConveyorFromOutboundPort" }),
                Path("ConveyorFromOutboundPort", new[] { "ReleaseOutboundPort" })
            }
        });
    }

    private static object Variable(string id, string type, string usage, object initialValue) => new {
        id,
        type,
        usage,
        initialValue
    };

    private static object Operation(string id, string consoleId, string operationTaskType, object[] inputs, object[] outputs, object[]? resourceOutputs = null) => new {
        id,
        nodeType = "Operation",
        description = id,
        estimatedDurationMilliseconds = GetEstimatedDurationMilliseconds(id),
        shouldThrowOnFailed = false,
        shouldThrowOnCanceled = false,
        inputs,
        outputs,
        resourceOutputs = resourceOutputs ?? Array.Empty<object>(),
        consoleId,
        operationTaskType
    };

    private static long GetEstimatedDurationMilliseconds(string nodeId) {
        return nodeId switch {
            "ConveyorToInboundPort" or "ConveyorFromOutboundPort" => 45_000,
            "StackCraneMoveToRack" or "StackCraneMoveToOutboundPort" => 120_000,
            "AcquireInboundPort" or "AcquireTargetLocation" or "AcquireSourceLocation" or "AcquireOutboundPort" => 30_000,
            "ResolveTargetLocation" or "ResolveSourceLocation" => 10_000,
            "OccupyInboundPort"
                or "BindLocationPallet"
                or "AcquireSourcePallet"
                or "BindOutboundPort"
                or "ReleaseOutboundPort" => 15_000,
            _ => 20_000
        };
    }

    private static object Input(string source, string destination) => new {
        source,
        destination
    };

    private static object Output(string source, string destination) => new {
        source,
        destination
    };

    private static object ResourceOutput(string source, string resourceType) => new {
        source,
        resourceType
    };

    private static object Path(string source, string[] targets) => new {
        type = 0,
        source,
        targets,
        kind = 0
    };
}
