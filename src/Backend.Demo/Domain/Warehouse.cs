using FlowEngine.Data;

namespace Backend.Demo.Domain;

public class Warehouse : IEntity<int> {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<Location> Locations { get; set; } = new();
}
