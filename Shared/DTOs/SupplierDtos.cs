using MenuManager.Shared.Enums;

namespace MenuManager.Shared.DTOs;

public class SupplierResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? Siret { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public PaymentType PaymentType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSupplierRequest
{
    public string Name { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? Siret { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public PaymentType PaymentType { get; set; }
}

public class UpdateSupplierRequest
{
    public string Name { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? Siret { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public PaymentType PaymentType { get; set; }
}
