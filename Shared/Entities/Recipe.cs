namespace MenuManager.Shared.Entities;

public class Recipe
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int BaseServings { get; set; } = 1;
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
    public ICollection<MealItem> MealItems { get; set; } = [];
}
