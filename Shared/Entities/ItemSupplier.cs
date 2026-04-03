namespace MenuManager.Shared.Entities;

public class ItemSupplier
{
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public decimal UnitPrice { get; set; }
    public string? SupplierReference { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime UpdatedAt { get; set; }
}
