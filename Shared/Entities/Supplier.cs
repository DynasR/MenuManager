using MenuManager.Shared.Enums;

namespace MenuManager.Shared.Entities;

public class Supplier : Party
{
    public string? CompanyName { get; set; }
    public string? Siret { get; set; }
    public PaymentType PaymentType { get; set; }
    public ICollection<ItemSupplier> ItemSuppliers { get; set; } = [];
}
