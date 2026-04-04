namespace MenuManager.Shared.DTOs;

public class CreateDayPlanRequest
{
    public DateOnly Date { get; set; }
    public int MenuPlanId { get; set; }
}

public class UpdateDayPlanRequest
{
    public DateOnly Date { get; set; }
    public int MenuPlanId { get; set; }
}
