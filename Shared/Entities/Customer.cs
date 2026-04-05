using MenuManager.Shared.Enums;

namespace MenuManager.Shared.Entities;

public class Customer : Party
{
    public required byte[] PasswordHash { get; set; }
    public required byte[] PasswordSalt { get; set; }
    public PaymentType PaymentType { get; set; }
    public ICollection<DailyMenu> DailyMenus { get; set; } = [];
}
