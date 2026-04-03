namespace MenuManager.Shared.Entities;

public class Customer : Party
{
    public required byte[] PasswordHash { get; set; }
    public required byte[] PasswordSalt { get; set; }
    public ICollection<MenuPlan> MenuPlans { get; set; } = [];
}
