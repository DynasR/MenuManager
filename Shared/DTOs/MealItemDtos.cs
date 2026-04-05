using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;

namespace MenuManager.Shared.DTOs;

public class MealItemResponse
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int Order { get; set; }
    public int MealId { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal PackageSize { get; set; }
    public MeasurementUnit Unit { get; set; }
}

public class CreateMealItemRequest
{
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int MealId { get; set; }
    public int ItemId { get; set; }
}

public class UpdateMealItemRequest
{
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int MealId { get; set; }
    public int ItemId { get; set; }
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
