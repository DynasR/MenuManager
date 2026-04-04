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
