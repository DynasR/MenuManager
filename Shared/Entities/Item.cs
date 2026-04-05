using MenuManager.Shared.Enums;

namespace MenuManager.Shared.Entities;

public class Item
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public MeasurementUnit PurchaseUnit { get; set; }
    public decimal ContentQuantity { get; set; } = 1;
    public MeasurementUnit ContentUnit { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ItemSupplier> ItemSuppliers { get; set; } = [];
    public ICollection<MealItem> MealItems { get; set; } = [];
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
}
