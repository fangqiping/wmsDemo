using System.ComponentModel.DataAnnotations;
using FlowEngine.Data;
using FlowEngine.Execution;
using FlowEngine.Execution.Resource;

namespace Backend.Demo.Domain;

[Resource]
public class Pallet : IResource<int> {
    public int Id { get; set; }

    // Human-readable pallet code used by warehouse operators.
    public string Code { get; set; } = string.Empty;

    [ConcurrencyCheck]
    public bool Enabled { get; set; }

    [ConcurrencyCheck]
    public bool Acquired { get; set; }

    // The SKU physically carried by the pallet.
    public int SkuId { get; set; }
    public Sku? Sku { get; set; }

    public decimal Quantity { get; set; }
}
