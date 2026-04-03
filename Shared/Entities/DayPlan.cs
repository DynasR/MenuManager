namespace MenuManager.Shared.Entities;

public class DayPlan
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int MenuPlanId { get; set; }
    public MenuPlan MenuPlan { get; set; } = null!;
    public ICollection<MealSlot> MealSlots { get; set; } = [];
}
