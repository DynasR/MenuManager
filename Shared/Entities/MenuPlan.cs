namespace MenuManager.Shared.Entities;

public class MenuPlan
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public ICollection<DayPlan> DayPlans { get; set; } = [];
}
