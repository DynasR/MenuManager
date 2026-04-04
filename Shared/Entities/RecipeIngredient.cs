namespace MenuManager.Shared.Entities;

public class RecipeIngredient
{
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public decimal Quantity { get; set; }
}
