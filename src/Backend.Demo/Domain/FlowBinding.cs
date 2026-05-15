using Backend.Demo.Domain.Enums;
using FlowEngine.Data;

namespace Backend.Demo.Domain;

public class FlowBinding : IEntity<int> {
    public int Id { get; set; }
    public BusinessFlowType BusinessType { get; set; }
    public string FlowDefinitionCode { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
