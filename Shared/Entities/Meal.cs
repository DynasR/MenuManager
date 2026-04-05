namespace MenuManager.Shared.Entities;

public class Meal
{
    public int Id { get; set; }
    public MealType MealType { get; set; }
    public int DailyMenuId { get; set; }
    public DailyMenu DailyMenu { get; set; } = null!;
    public ICollection<MealItem> MealItems { get; set; } = [];
}
