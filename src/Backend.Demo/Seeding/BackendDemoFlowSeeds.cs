using System.Text.Json;
using Backend.Demo.Domain;
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
                Variable("TargetLocationCode", "string", "input", string.Empty),
                Variable("TargetLocationId", "int", "input", 0),
                Variable("SkuCode", "string", "input", string.Empty),
                Variable("Status", "string", "output", "Draft")
            },
            nodes = new object[] {
                Operation(
                    id: "AcquireTargetLocation",
                    consoleId: FunctionConsole.NAME,
                    operationTaskType: typeof(IdAcquireResourceTask<int, Location>).FullName!,
                    inputs: new object[] {
                        Input("TargetLocationId", nameof(IdAcquireResourceTask<int, Location>.ResourceId))
                    },
                    outputs: Array.Empty<object>(),
                    resourceOutputs: new object[] {
                        ResourceOutput(nameof(IdAcquireResourceTask<int, Location>.ResourceId), typeof(Location).FullName!)
                    }),
                Operation(
                    id: "Receive",
                    consoleId: ConveyorConsole.NAME,
                    operationTaskType: typeof(ConveyorTransferOperationTask).FullName!,
                    inputs: new object[] {
                        Input("OrderCode", nameof(ConveyorTransferOperationTask.OrderCode)),
                        Input("SourceLocationCode", nameof(ConveyorTransferOperationTask.FromLocationCode)),
                        Input("TargetLocationCode", nameof(ConveyorTransferOperationTask.ToLocationCode))
                    },
                    outputs: Array.Empty<object>()),
                Operation(
                    id: "Store",
                    consoleId: StackCraneConsole.NAME,
                    operationTaskType: typeof(StackCraneStoreOperationTask).FullName!,
                    inputs: new object[] {
                        Input("OrderCode", nameof(StackCraneStoreOperationTask.OrderCode)),
                        Input("SkuCode", nameof(StackCraneStoreOperationTask.SkuCode)),
                        Input("TargetLocationCode", nameof(StackCraneStoreOperationTask.TargetLocationCode)),
                        Input("TargetLocationId", nameof(StackCraneStoreOperationTask.TargetLocationId))
                    },
                    outputs: new object[] {
                        Output(nameof(StackCraneStoreOperationTask.CompletionMessage), "Status")
                    })
            },
            routes = new object[] {
                Path("Root", new[] { "AcquireTargetLocation" }),
                Path("AcquireTargetLocation", new[] { "Receive" }),
                Path("Receive", new[] { "Store" })
            }
        });
    }

    private static string BuildOutboundDraftDocumentJson() {
        return JsonSerializer.Serialize(new {
            id = "OutboundBasicFlow",
            variables = new object[] {
                Variable("OrderCode", "string", "input", string.Empty),
                Variable("SourceLocationCode", "string", "input", string.Empty),
                Variable("SourceLocationId", "int", "input", 0),
                Variable("TargetLocationCode", "string", "input", string.Empty),
                Variable("SkuCode", "string", "input", string.Empty),
                Variable("Status", "string", "output", "Draft")
            },
            nodes = new object[] {
                Operation(
                    id: "AcquireSourceLocation",
                    consoleId: FunctionConsole.NAME,
                    operationTaskType: typeof(IdAcquireResourceTask<int, Location>).FullName!,
                    inputs: new object[] {
                        Input("SourceLocationId", nameof(IdAcquireResourceTask<int, Location>.ResourceId))
                    },
                    outputs: Array.Empty<object>(),
                    resourceOutputs: new object[] {
                        ResourceOutput(nameof(IdAcquireResourceTask<int, Location>.ResourceId), typeof(Location).FullName!)
                    }),
                Operation(
                    id: "Retrieve",
                    consoleId: StackCraneConsole.NAME,
                    operationTaskType: typeof(StackCraneRetrieveOperationTask).FullName!,
                    inputs: new object[] {
                        Input("OrderCode", nameof(StackCraneRetrieveOperationTask.OrderCode)),
                        Input("SkuCode", nameof(StackCraneRetrieveOperationTask.SkuCode)),
                        Input("SourceLocationCode", nameof(StackCraneRetrieveOperationTask.SourceLocationCode)),
                        Input("SourceLocationId", nameof(StackCraneRetrieveOperationTask.SourceLocationId))
                    },
                    outputs: Array.Empty<object>()),
                Operation(
                    id: "Deliver",
                    consoleId: ConveyorConsole.NAME,
                    operationTaskType: typeof(ConveyorTransferOperationTask).FullName!,
                    inputs: new object[] {
                        Input("OrderCode", nameof(ConveyorTransferOperationTask.OrderCode)),
                        Input("SourceLocationCode", nameof(ConveyorTransferOperationTask.FromLocationCode)),
                        Input("TargetLocationCode", nameof(ConveyorTransferOperationTask.ToLocationCode))
                    },
                    outputs: new object[] {
                        Output(nameof(ConveyorTransferOperationTask.CompletionMessage), "Status")
                    })
            },
            routes = new object[] {
                Path("Root", new[] { "AcquireSourceLocation" }),
                Path("AcquireSourceLocation", new[] { "Retrieve" }),
                Path("Retrieve", new[] { "Deliver" })
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
        shouldThrowOnFailed = false,
        shouldThrowOnCanceled = false,
        inputs,
        outputs,
        resourceOutputs = resourceOutputs ?? Array.Empty<object>(),
        consoleId,
        operationTaskType
    };

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
