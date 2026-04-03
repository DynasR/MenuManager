using MenuManager.Shared.Enums;

namespace MenuManager.Shared.Entities;

public class Item
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public MeasurementUnit Unit { get; set; }
    public decimal PackageSize { get; set; } = 1;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ItemSupplier> ItemSuppliers { get; set; } = [];
    public ICollection<MealSlotItem> MealSlotItems { get; set; } = [];
}
