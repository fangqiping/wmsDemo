using System.Threading;
using FlowEngine.Execution.Consoles;

namespace Backend.Demo;

public sealed class StackCraneStoreOperationTask : OperationTask<StackCraneConsole> {
    [Input]
    public string OrderCode { get; set; } = string.Empty;

    [Input]
    public string SkuCode { get; set; } = string.Empty;

    [Input]
    public string TargetLocationCode { get; set; } = string.Empty;

    [Input]
    public int DelayMilliseconds { get; set; } = 400;

    [Output]
    public string CompletionMessage { get; set; } = string.Empty;

    protected override async Task DoProcessAsync(StackCraneConsole console, CancellationToken cancellationToken) {
        await Task.Delay(DelayMilliseconds, cancellationToken);
        CompletionMessage = $"{OrderCode}: stack crane stored {SkuCode} to {TargetLocationCode}.";
    }
}
