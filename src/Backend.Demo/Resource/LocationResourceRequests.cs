using FlowEngine.Execution.Resource;

namespace Backend.Demo.Resource;

public sealed class EmptyRackLocationRequest : IResourceRequest {
    public int WarehouseId { get; init; }
    public int PreferredLocationId { get; init; }
}

public sealed class OccupiedRackLocationRequest : IResourceRequest {
    public int WarehouseId { get; init; }
    public int SkuId { get; init; }
    public int PreferredLocationId { get; init; }
}
