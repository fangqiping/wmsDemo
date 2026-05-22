using Backend.Demo.Domain;
using FlowEngine.Execution.Consoles;
using FlowEngine.Execution.Resource;

namespace Backend.Demo.Resource;

public sealed class AcquireEmptyRackLocationTask
    : AcquireResourceTask<int, Location, EmptyRackLocationRequest> {
    public const string RULE_NAME = "empty-rack-location";

    [Input]
    public int WarehouseId { get; set; }

    [Input]
    public int PreferredLocationId { get; set; }

    protected override string DoGetRuleName() => RULE_NAME;

    protected override EmptyRackLocationRequest DoCreateRequest() => new() {
        WarehouseId = WarehouseId,
        PreferredLocationId = PreferredLocationId
    };
}

public sealed class AcquireOccupiedRackLocationTask
    : AcquireResourceTask<int, Location, OccupiedRackLocationRequest> {
    public const string RULE_NAME = "occupied-rack-location";

    [Input]
    public int WarehouseId { get; set; }

    [Input]
    public int SkuId { get; set; }

    [Input]
    public int PreferredLocationId { get; set; }

    protected override string DoGetRuleName() => RULE_NAME;

    protected override OccupiedRackLocationRequest DoCreateRequest() => new() {
        WarehouseId = WarehouseId,
        SkuId = SkuId,
        PreferredLocationId = PreferredLocationId
    };
}
