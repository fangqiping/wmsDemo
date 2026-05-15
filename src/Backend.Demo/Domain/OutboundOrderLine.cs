using FlowEngine.Data;

namespace Backend.Demo.Domain;

public class OutboundOrderLine : IEntity<int> {
    public int Id { get; set; }
    public int OutboundOrderId { get; set; }
    public OutboundOrder? OutboundOrder { get; set; }
    public int SkuId { get; set; }
    public Sku? Sku { get; set; }
    public decimal Quantity { get; set; }
    public int SourceLocationId { get; set; }
    public Location? SourceLocation { get; set; }
}
