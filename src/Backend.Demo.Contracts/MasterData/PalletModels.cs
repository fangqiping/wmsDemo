namespace Backend.Demo.Contracts.MasterData;

public class PalletModel {
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool Acquired { get; set; }
    public int SkuId { get; set; }
    public decimal Quantity { get; set; }
}
