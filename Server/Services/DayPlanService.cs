using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IDayPlanService
{
    Task<List<DayPlanResponse>> GetAllAsync();
    Task<List<DayPlanResponse>> GetByMenuPlanAsync(int menuPlanId);
    Task<DayPlanResponse?> GetByIdAsync(int id);
    Task<DayPlanResponse?> CreateAsync(CreateDayPlanRequest request);
    Task<DayPlanResponse?> UpdateAsync(int id, UpdateDayPlanRequest request);
    Task<bool> DeleteAsync(int id);
}

public class DayPlanService : IDayPlanService
{
    private readonly AppDbContext _db;

    public DayPlanService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DayPlanResponse>> GetAllAsync()
    {
        var dayPlans = await _db.DayPlans
            .Include(dp => dp.MealSlots)
                .ThenInclude(ms => ms.MealSlotItems)
                    .ThenInclude(msi => msi.Item)
                        .ThenInclude(i => i.ItemSuppliers)
            .AsNoTracking()
            .ToListAsync();

        return dayPlans.Select(MapToResponse).ToList();
    }

    public async Task<List<DayPlanResponse>> GetByMenuPlanAsync(int menuPlanId)
    {
        var dayPlans = await _db.DayPlans
            .Where(dp => dp.MenuPlanId == menuPlanId)
            .Include(dp => dp.MealSlots)
                .ThenInclude(ms => ms.MealSlotItems)
                    .ThenInclude(msi => msi.Item)
                        .ThenInclude(i => i.ItemSuppliers)
            .AsNoTracking()
            .ToListAsync();

        return dayPlans.Select(MapToResponse).ToList();
    }

    public async Task<DayPlanResponse?> GetByIdAsync(int id)
    {
        var dayPlan = await _db.DayPlans
            .Include(dp => dp.MealSlots)
                .ThenInclude(ms => ms.MealSlotItems)
                    .ThenInclude(msi => msi.Item)
                        .ThenInclude(i => i.ItemSuppliers)
            .AsNoTracking()
            .FirstOrDefaultAsync(dp => dp.Id == id);

        return dayPlan is null ? null : MapToResponse(dayPlan);
    }

    public async Task<DayPlanResponse?> CreateAsync(CreateDayPlanRequest request)
    {
        var menuPlanExists = await _db.MenuPlans.AnyAsync(mp => mp.Id == request.MenuPlanId);
        if (!menuPlanExists) return null;

        var dayPlan = new DayPlan
        {
            Date = request.Date,
            MenuPlanId = request.MenuPlanId
        };

        _db.DayPlans.Add(dayPlan);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(dayPlan.Id);
    }

    public async Task<DayPlanResponse?> UpdateAsync(int id, UpdateDayPlanRequest request)
    {
        var dayPlan = await _db.DayPlans.FindAsync(id);
        if (dayPlan is null) return null;

        var menuPlanExists = await _db.MenuPlans.AnyAsync(mp => mp.Id == request.MenuPlanId);
        if (!menuPlanExists) return null;

        dayPlan.Date = request.Date;
        dayPlan.MenuPlanId = request.MenuPlanId;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var dayPlan = await _db.DayPlans.FindAsync(id);
        if (dayPlan is null) return false;

        _db.DayPlans.Remove(dayPlan);
        await _db.SaveChangesAsync();

        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DayPlanResponse MapToResponse(DayPlan dp) => new()
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
