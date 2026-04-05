namespace MenuManager.Shared.Entities;

public class DailyMenu
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public ICollection<Meal> Meals { get; set; } = [];
}
