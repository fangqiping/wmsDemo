using System.Threading;
using FlowEngine.Execution.Consoles;

namespace Backend.Demo;

public sealed class ConveyorTransferOperationTask : OperationTask<ConveyorConsole> {
    [Input]
    public string OrderCode { get; set; } = string.Empty;

    [Input]
    public string FromLocationCode { get; set; } = string.Empty;

    [Input]
    public string ToLocationCode { get; set; } = string.Empty;

    [Input]
    public int DelayMilliseconds { get; set; } = 30000;

    [Output]
    public string CompletionMessage { get; set; } = string.Empty;

    protected override async Task DoProcessAsync(ConveyorConsole console, CancellationToken cancellationToken) {
        await Task.Delay(DelayMilliseconds, cancellationToken);
        CompletionMessage = $"{OrderCode}: conveyor moved from {FromLocationCode} to {ToLocationCode}.";
    }
}
