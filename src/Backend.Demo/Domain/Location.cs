using System.ComponentModel.DataAnnotations;
using Backend.Demo.Domain.Enums;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.Resource;

namespace Backend.Demo.Domain;

[Resource]
public class Location : IResource<int> {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [ConcurrencyCheck]
    public bool Enabled { get; set; }

    [ConcurrencyCheck]
    public bool Acquired { get; set; }

    public LocationType LocationType { get; set; }
    public LocationStatus Status { get; set; }
    public int? CurrentPalletId { get; set; }
    public Pallet? CurrentPallet { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
}
