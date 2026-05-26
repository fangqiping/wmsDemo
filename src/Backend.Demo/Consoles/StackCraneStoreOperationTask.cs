using System.Threading;
using FlowEngine.Execution.Consoles;

namespace Backend.Demo;

public sealed class StackCraneStoreOperationTask : OperationTask<StackCraneConsole> {
    public static int DefaultDelayMilliseconds { get; set; } = 20000;

    [Input]
    public string OrderCode { get; set; } = string.Empty;

    [Input]
    public string SkuCode { get; set; } = string.Empty;

    [Input]
    public int SkuId { get; set; }

    [Input]
    public string TargetLocationCode { get; set; } = string.Empty;

    [Input]
    public int TargetLocationId { get; set; }

    [Input]
    public int DelayMilliseconds { get; set; } = DefaultDelayMilliseconds;

    [Output]
    public string CompletionMessage { get; set; } = string.Empty;

    protected override async Task DoProcessAsync(StackCraneConsole console, CancellationToken cancellationToken) {
        await Task.Delay(DelayMilliseconds, cancellationToken);
        await console.MoveToRackAsync(TargetLocationId);
        CompletionMessage = $"{OrderCode}: stack crane moved {SkuCode} to {TargetLocationCode}.";
    }
}
