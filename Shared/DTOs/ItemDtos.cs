using MenuManager.Shared.Enums;

namespace MenuManager.Shared.DTOs;

public class ItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public MeasurementUnit Unit { get; set; }
    public decimal PackageSize { get; set; } = 1;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateItemRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public MeasurementUnit Unit { get; set; }
    public decimal PackageSize { get; set; } = 1;
    public int CategoryId { get; set; }
}

public class UpdateItemRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public MeasurementUnit Unit { get; set; }
    public decimal PackageSize { get; set; } = 1;
    public int CategoryId { get; set; }
}
