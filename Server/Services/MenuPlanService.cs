using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IMenuPlanService
{
    Task<List<MenuPlanResponse>> GetAllAsync();
    Task<List<MenuPlanResponse>> GetByCustomerAsync(int customerId);
    Task<MenuPlanResponse?> GetByIdAsync(int id);
    Task<MenuPlanResponse?> CreateAsync(MenuPlanRequest request);
    Task<MenuPlanResponse?> UpdateAsync(int id, MenuPlanRequest request);
    Task<bool> DeleteAsync(int id);
}

public class MenuPlanService : IMenuPlanService
{
    private readonly AppDbContext _db;

    public MenuPlanService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<MenuPlanResponse>> GetAllAsync()
    {
        var plans = await QueryWithIncludes()
            .AsNoTracking()
            .ToListAsync();

        return plans.Select(MapToResponse).ToList();
    }

    public async Task<List<MenuPlanResponse>> GetByCustomerAsync(int customerId)
    {
        var plans = await QueryWithIncludes()
            .AsNoTracking()
            .Where(mp => mp.CustomerId == customerId)
            .ToListAsync();

        return plans.Select(MapToResponse).ToList();
    }

    public async Task<MenuPlanResponse?> GetByIdAsync(int id)
    {
        var plan = await QueryWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(mp => mp.Id == id);

        return plan is null ? null : MapToResponse(plan);
    }

    public async Task<MenuPlanResponse?> CreateAsync(MenuPlanRequest request)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId);
        if (!customerExists) return null;

        var plan = new MenuPlan
        {
            Name = request.Name,
            Month = request.Month,
            Year = request.Year,
            CustomerId = request.CustomerId,
            CreatedAt = DateTime.UtcNow
        };

        _db.MenuPlans.Add(plan);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(plan.Id);
    }

    public async Task<MenuPlanResponse?> UpdateAsync(int id, MenuPlanRequest request)
    {
        var plan = await _db.MenuPlans
            .Include(mp => mp.DayPlans)
                .ThenInclude(dp => dp.MealSlots)
                    .ThenInclude(ms => ms.MealSlotItems)
            .FirstOrDefaultAsync(mp => mp.Id == id);

        if (plan is null) return null;

        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId);
        if (!customerExists) return null;

        plan.Name = request.Name;
        plan.Month = request.Month;
        plan.Year = request.Year;
        plan.CustomerId = request.CustomerId;

        _db.DayPlans.RemoveRange(plan.DayPlans.ToList());
        plan.DayPlans.Clear();

        foreach (var dpReq in request.DayPlans)
            plan.DayPlans.Add(MapRequestToDayPlan(dpReq));

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var plan = await _db.MenuPlans.FirstOrDefaultAsync(mp => mp.Id == id);
        if (plan is null) return false;

        _db.MenuPlans.Remove(plan);
        await _db.SaveChangesAsync();

        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IQueryable<MenuPlan> QueryWithIncludes() =>
        _db.MenuPlans
            .Include(mp => mp.Customer)
            .Include(mp => mp.DayPlans)
                .ThenInclude(dp => dp.MealSlots)
                    .ThenInclude(ms => ms.MealSlotItems)
                        .ThenInclude(msi => msi.Item)
                            .ThenInclude(i => i.ItemSuppliers);

    private static DayPlan MapRequestToDayPlan(DayPlanRequest req) => new()
    {
        Date = req.Date,
        MealSlots = req.MealSlots.Select(MapRequestToMealSlot).ToList()
    };

    private static MealSlot MapRequestToMealSlot(MealSlotRequest req) => new()
    {
        MealType = req.MealType,
        MealSlotItems = req.MealSlotItems.Select(MapRequestToMealSlotItem).ToList()
    };

    private static MealSlotItem MapRequestToMealSlotItem(MealSlotItemRequest req) => new()
    {
        ItemId = req.ItemId,
        Quantity = req.Quantity,
        Notes = req.Notes
    };

    private static MenuPlanResponse MapToResponse(MenuPlan mp) => new()
    {
        Id = mp.Id,
        Name = mp.Name,
        Month = mp.Month,
        Year = mp.Year,
        CustomerId = mp.CustomerId,
        CustomerName = mp.Customer.Name,
        CreatedAt = mp.CreatedAt,
        HasData = mp.DayPlans.Any(dp => dp.MealSlots.Any(ms => ms.MealSlotItems.Count > 0)),
        DayPlans = mp.DayPlans.Select(MapDayPlanToResponse).ToList()
    };

    private static DayPlanResponse MapDayPlanToResponse(DayPlan dp) => new()
    {
        Id = dp.Id,
        Date = dp.Date,
        MenuPlanId = dp.MenuPlanId,
        MealSlots = dp.MealSlots.Select(MapMealSlotToResponse).ToList()
    };

    private static MealSlotResponse MapMealSlotToResponse(MealSlot ms) => new()
    {
        Id = ms.Id,
        MealType = ms.MealType,
        DayPlanId = ms.DayPlanId,
        MealSlotItems = ms.MealSlotItems.Select(MapMealSlotItemToResponse).ToList()
    };

    private static MealSlotItemResponse MapMealSlotItemToResponse(MealSlotItem msi) => new()
    {
        Id = msi.Id,
        ItemId = msi.ItemId ?? 0,
        ItemName = msi.Item?.Name ?? "",
        Quantity = msi.Quantity,
        Notes = msi.Notes,
        Order = msi.Order,
        MealSlotId = msi.MealSlotId,
        UnitPrice = msi.Item?.ItemSuppliers
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.SupplierId)
            .Select(s => (decimal?)s.UnitPrice)
            .FirstOrDefault(),
        PackageSize = msi.Item?.PackageSize ?? 1,
        Unit = msi.Item?.Unit ?? default
    };
}
