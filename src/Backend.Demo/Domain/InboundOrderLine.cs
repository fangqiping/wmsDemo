using FlowEngine.Data;

namespace Backend.Demo.Domain;

public class InboundOrderLine : IEntity<int> {
    public int Id { get; set; }
    public int InboundOrderId { get; set; }
    public InboundOrder? InboundOrder { get; set; }
    public int SkuId { get; set; }
    public Sku? Sku { get; set; }
    public decimal Quantity { get; set; }
    public int TargetLocationId { get; set; }
    public Location? TargetLocation { get; set; }
}
