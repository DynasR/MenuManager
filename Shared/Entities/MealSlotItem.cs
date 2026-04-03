namespace MenuManager.Shared.Entities;

public class MealSlotItem
{
    public int Id { get; set; }

    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int MealSlotId { get; set; }
    public MealSlot MealSlot { get; set; } = null!;
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
}
