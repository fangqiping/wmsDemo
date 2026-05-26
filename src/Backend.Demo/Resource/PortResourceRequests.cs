using FlowEngine.Execution.Resource;

namespace Backend.Demo.Resource;

public sealed class InboundPortRequest : IResourceRequest {
    public int WarehouseId { get; init; }
}

public sealed class OutboundPortRequest : IResourceRequest {
    public int WarehouseId { get; init; }
}
