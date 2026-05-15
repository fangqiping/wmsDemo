namespace Backend.Demo.Contracts.Orders;

public class OutboundOrderLineModel {
    public int Id { get; set; }
    public int SkuId { get; set; }
    public decimal Quantity { get; set; }
    public int SourceLocationId { get; set; }
}

public class OutboundOrderModel {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string? FlowDefinitionCode { get; set; }
    public int? FlowVersionNumber { get; set; }
    public long? FlowTaskId { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset? CreatedTime { get; set; }
    public DateTimeOffset? UpdatedTime { get; set; }
    public DateTimeOffset? CompletedTime { get; set; }
    public List<OutboundOrderLineModel> Lines { get; set; } = new();
}
