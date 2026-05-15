using Backend.Demo.Domain.Enums;
using FlowEngine.Data;

namespace Backend.Demo.Domain;

public class Location : IEntity<int> {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public LocationType LocationType { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
}
