using FlowEngine.Data;

namespace Backend.Demo.Domain;

public class Sku : IEntity<int> {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Spec { get; set; } = string.Empty;
}
