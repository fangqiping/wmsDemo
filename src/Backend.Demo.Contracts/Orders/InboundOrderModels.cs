namespace Backend.Demo.Contracts.Orders;

public class InboundOrderLineModel {
    public int Id { get; set; }
    public int SkuId { get; set; }
    public decimal Quantity { get; set; }
    public int TargetLocationId { get; set; }
}

public class InboundOrderModel {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? FlowDefinitionCode { get; set; }
    public int? FlowVersionNumber { get; set; }
    public long? FlowTaskId { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset? CreatedTime { get; set; }
    public DateTimeOffset? UpdatedTime { get; set; }
    public DateTimeOffset? CompletedTime { get; set; }
    public List<InboundOrderLineModel> Lines { get; set; } = new();
}
