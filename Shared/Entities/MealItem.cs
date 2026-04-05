using MenuManager.Shared.Enums;

namespace MenuManager.Shared.Entities;

public class MealItem
{
    public int Id { get; set; }

    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int Order { get; set; }
    public MeasurementUnit Unit { get; set; }
    public int MealId { get; set; }
    public Meal Meal { get; set; } = null!;
    public int? ItemId { get; set; }
    public Item? Item { get; set; }
    public int? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }
}
