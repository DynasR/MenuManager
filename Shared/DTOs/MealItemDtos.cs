using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;

namespace MenuManager.Shared.DTOs;

public class MealItemResponse
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public int? RecipeId { get; set; }
    public string? RecipeName { get; set; }
    public decimal? RecipeEstimatedCost { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int Order { get; set; }
    public MeasurementUnit Unit { get; set; }
    public int MealId { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal ContentQuantity { get; set; }
    public MeasurementUnit PurchaseUnit { get; set; }
    public MeasurementUnit ContentUnit { get; set; }
}

public class CreateMealItemRequest
{
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int MealId { get; set; }
    public int? ItemId { get; set; }
    public int? RecipeId { get; set; }
    public MeasurementUnit Unit { get; set; }
}

public class UpdateMealItemRequest
{
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int MealId { get; set; }
    public int ItemId { get; set; }
    public MeasurementUnit Unit { get; set; }
}

public class MoveMealItemRequest
{
    public DateOnly TargetDate { get; set; }
    public MealType TargetMealType { get; set; }
    public int NewOrder { get; set; }
}

public class ReorderMealItemsRequest
{
    public int MealId { get; set; }
    public List<int> OrderedItemIds { get; set; } = [];
}
