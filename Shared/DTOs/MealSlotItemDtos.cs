using MenuManager.Shared.Entities;

namespace MenuManager.Shared.DTOs;

public class CreateMealSlotItemRequest
{
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int MealSlotId { get; set; }
    public int ItemId { get; set; }
}

public class UpdateMealSlotItemRequest
{
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int MealSlotId { get; set; }
    public int ItemId { get; set; }
}

public class MoveMealSlotItemRequest
{
    public DateOnly TargetDate { get; set; }
    public MealType TargetMealType { get; set; }
    public int NewOrder { get; set; }
}
