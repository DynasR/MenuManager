namespace MenuManager.Shared.DTOs;

public enum CreateItemSupplierError { ItemNotFound, SupplierNotFound, AlreadyExists }

public record CreateItemSupplierResult(
    ItemSupplierResponse? Response,
    CreateItemSupplierError? Error
);

public class ItemSupplierResponse
{
    public int ItemId { get; set; }
    public int SupplierId { get; set; }
    public string ItemName { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public string? SupplierReference { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateItemSupplierRequest
{
    public int ItemId { get; set; }
    public int SupplierId { get; set; }
    public decimal UnitPrice { get; set; }
    public string? SupplierReference { get; set; }
    public bool IsAvailable { get; set; }
}

public class UpdateItemSupplierRequest
{
    public decimal UnitPrice { get; set; }
    public string? SupplierReference { get; set; }
    public bool IsAvailable { get; set; }
}
