namespace MenuManager.Shared.Entities;

public class MealSlot
{
    public int Id { get; set; }
    public MealType MealType { get; set; }
    public int DayPlanId { get; set; }
    public DayPlan DayPlan { get; set; } = null!;
    public ICollection<MealSlotItem> MealSlotItems { get; set; } = [];
}
