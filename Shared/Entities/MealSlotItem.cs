namespace MenuManager.Shared.Entities;

public class MealSlotItem
{
    public int Id { get; set; }

    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public decimal Servings { get; set; } = 1;
    public int Order { get; set; }
    public int MealSlotId { get; set; }
    public MealSlot MealSlot { get; set; } = null!;
    public int? ItemId { get; set; }
    public Item? Item { get; set; }
    public int? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }
}
