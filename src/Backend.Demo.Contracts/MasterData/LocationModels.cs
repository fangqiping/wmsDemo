namespace Backend.Demo.Contracts.MasterData;

public class LocationModel {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool Acquired { get; set; }
    public int LocationType { get; set; }
    public int Status { get; set; }
    public int WarehouseId { get; set; }
}
