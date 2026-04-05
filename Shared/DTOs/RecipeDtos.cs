using MenuManager.Shared.Enums;

namespace MenuManager.Shared.DTOs;

public class RecipeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int BaseServings { get; set; }
    public decimal EstimatedCost { get; set; }
    public List<RecipeIngredientResponse> Ingredients { get; set; } = [];
}

public class CreateRecipeRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int BaseServings { get; set; } = 1;
}

public class UpdateRecipeRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int BaseServings { get; set; } = 1;
}

public class RecipeIngredientResponse
{
    public int RecipeId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public decimal Quantity { get; set; }
    public MeasurementUnit Unit { get; set; }
    public int Order { get; set; }
    public MeasurementUnit PurchaseUnit { get; set; }
    public MeasurementUnit ContentUnit { get; set; }
    public decimal ContentQuantity { get; set; }
    public decimal? UnitPrice { get; set; }
}

public class RecipeIngredientRequest
{
    public int RecipeId { get; set; }
    public int ItemId { get; set; }
    public decimal Quantity { get; set; }
    public MeasurementUnit Unit { get; set; }
    public int Order { get; set; }
}
