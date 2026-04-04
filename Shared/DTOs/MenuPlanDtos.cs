using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;

namespace MenuManager.Shared.DTOs;

// ── Requests ────────────────────────────────────────────────────────────────

public class MenuPlanRequest
{
    public string Name { get; set; } = "";
    public int Month { get; set; }
    public int Year { get; set; }
    public int CustomerId { get; set; }
    public List<DayPlanRequest> DayPlans { get; set; } = [];
}

public class DayPlanRequest
{
    public DateOnly Date { get; set; }
    public List<MealSlotRequest> MealSlots { get; set; } = [];
}

public class MealSlotRequest
{
    public MealType MealType { get; set; }
    public List<MealSlotItemRequest> MealSlotItems { get; set; } = [];
}

public class MealSlotItemRequest
{
    public int ItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

// ── Responses ────────────────────────────────────────────────────────────────

public class MenuPlanResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Month { get; set; }
    public int Year { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool HasData { get; set; }
    public decimal MonthlyCost { get; set; }
    public List<DayPlanResponse> DayPlans { get; set; } = [];
}

public class DayPlanResponse
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int MenuPlanId { get; set; }
    public List<MealSlotResponse> MealSlots { get; set; } = [];
}

public class MealSlotResponse
{
    public int Id { get; set; }
    public MealType MealType { get; set; }
    public int DayPlanId { get; set; }
    public List<MealSlotItemResponse> MealSlotItems { get; set; } = [];
}

public class MealSlotItemResponse
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public int Order { get; set; }
    public int MealSlotId { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal PackageSize { get; set; }
    public MeasurementUnit Unit { get; set; }
}
