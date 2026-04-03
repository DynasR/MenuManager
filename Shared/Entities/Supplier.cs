namespace MenuManager.Shared.Entities;

public class Supplier : Party
{
    public string? CompanyName { get; set; }
    public string? Siret { get; set; }
    public ICollection<ItemSupplier> ItemSuppliers { get; set; } = [];
}
