using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Data.EntityFramework.ReaderWriter;

namespace Backend.Demo.Domain;

public class InboundOrder : IEntity<int> {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? FlowDefinitionCode { get; set; }
    public int? FlowVersionNumber { get; set; }
    public long? FlowTaskId { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
    public DateTimeOffset? CompletedTime { get; set; }

    [Own]
    public List<InboundOrderLine> Lines { get; set; } = new();
}
