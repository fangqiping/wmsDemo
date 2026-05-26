using Backend.Demo.Domain;
using FlowEngine.Execution.Consoles;
using FlowEngine.Execution.Resource;

namespace Backend.Demo.Resource;

public sealed class AcquireInboundPortTask : OperationTask<FunctionConsole> {
    public const string RULE_NAME = "idle-inbound-port";

    [Input]
    public int WarehouseId { get; set; }

    [Output]
    public int InboundPortId { get; set; }

    [Output]
    public string InboundPortCode { get; set; } = string.Empty;

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var resourceManagerProvider = console.ServiceProvider.GetRequiredService<IResourceManagerProvider>();
        var resourceManager = resourceManagerProvider.Get<int, Port, InboundPortRequest>(RULE_NAME);
        var acquired = await resourceManager.AcquireAsync(new InboundPortRequest {
            WarehouseId = WarehouseId
        }, cancellationToken) ?? throw new InvalidOperationException("No idle inbound port is available.");

        InboundPortId = acquired.Id;
        InboundPortCode = acquired.Code;
    }
}

public sealed class AcquireOutboundPortTask : OperationTask<FunctionConsole> {
    public const string RULE_NAME = "idle-outbound-port";

    [Input]
    public int WarehouseId { get; set; }

    [Output]
    public int OutboundPortId { get; set; }

    [Output]
    public string OutboundPortCode { get; set; } = string.Empty;

    protected override async Task DoProcessAsync(FunctionConsole console, CancellationToken cancellationToken) {
        var resourceManagerProvider = console.ServiceProvider.GetRequiredService<IResourceManagerProvider>();
        var resourceManager = resourceManagerProvider.Get<int, Port, OutboundPortRequest>(RULE_NAME);
        var acquired = await resourceManager.AcquireAsync(new OutboundPortRequest {
            WarehouseId = WarehouseId
        }, cancellationToken) ?? throw new InvalidOperationException("No idle outbound port is available.");

        OutboundPortId = acquired.Id;
        OutboundPortCode = acquired.Code;
    }
}
