namespace MenuManager.Shared.DTOs;

public class MonthlySummaryResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public bool HasMeals { get; set; }
    public decimal MonthlyCost { get; set; }
}

public class DailyMenuResponse
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int CustomerId { get; set; }
    public List<MealResponse> Meals { get; set; } = [];
}

public class CreateDailyMenuRequest
{
    public DateOnly Date { get; set; }
    public int CustomerId { get; set; }
}

public class UpdateDailyMenuRequest
{
    public DateOnly Date { get; set; }
    public int CustomerId { get; set; }
}
