using System.Threading;
using FlowEngine.Execution.Consoles;

namespace Backend.Demo;

public sealed class StackCraneRetrieveOperationTask : OperationTask<StackCraneConsole> {
    public static int DefaultDelayMilliseconds { get; set; } = 20000;

    [Input]
    public string OrderCode { get; set; } = string.Empty;

    [Input]
    public string SkuCode { get; set; } = string.Empty;

    [Input]
    public string SourceLocationCode { get; set; } = string.Empty;

    [Input]
    public int DelayMilliseconds { get; set; } = DefaultDelayMilliseconds;

    [Output]
    public string CompletionMessage { get; set; } = string.Empty;

    protected override async Task DoProcessAsync(StackCraneConsole console, CancellationToken cancellationToken) {
        await Task.Delay(DelayMilliseconds, cancellationToken);
        CompletionMessage = $"{OrderCode}: stack crane retrieved {SkuCode} from {SourceLocationCode}.";
    }
}
