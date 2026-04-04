using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IMealSlotService
{
    Task<List<MealSlotResponse>> GetAllAsync();
    Task<MealSlotResponse?> GetByIdAsync(int id);
    Task<CreateMealSlotResult> CreateAsync(CreateMealSlotRequest request);
    Task<MealSlotResponse?> UpdateAsync(int id, UpdateMealSlotRequest request);
    Task<bool> DeleteAsync(int id);
}

public class MealSlotService : IMealSlotService
{
    private readonly AppDbContext _db;

    public MealSlotService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<MealSlotResponse>> GetAllAsync()
    {
        var mealSlots = await QueryWithIncludes()
            .AsNoTracking()
            .ToListAsync();

        return mealSlots.Select(MapToResponse).ToList();
    }

    public async Task<MealSlotResponse?> GetByIdAsync(int id)
    {
        var mealSlot = await QueryWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(ms => ms.Id == id);

        return mealSlot is null ? null : MapToResponse(mealSlot);
    }

    public async Task<CreateMealSlotResult> CreateAsync(CreateMealSlotRequest request)
    {
        var dayPlanExists = await _db.DayPlans.AnyAsync(dp => dp.Id == request.DayPlanId);
        if (!dayPlanExists)
            return new CreateMealSlotResult(null, CreateMealSlotError.DayPlanNotFound);

        var alreadyExists = await _db.MealSlots.AnyAsync(
            ms => ms.DayPlanId == request.DayPlanId && ms.MealType == request.MealType);
        if (alreadyExists)
            return new CreateMealSlotResult(null, CreateMealSlotError.AlreadyExists);

        var mealSlot = new MealSlot
        {
            MealType = request.MealType,
            DayPlanId = request.DayPlanId
        };

        _db.MealSlots.Add(mealSlot);
        await _db.SaveChangesAsync();

        return new CreateMealSlotResult(await GetByIdAsync(mealSlot.Id), null);
    }

    public async Task<MealSlotResponse?> UpdateAsync(int id, UpdateMealSlotRequest request)
    {
        var mealSlot = await _db.MealSlots.FindAsync(id);
        if (mealSlot is null) return null;

        var dayPlanExists = await _db.DayPlans.AnyAsync(dp => dp.Id == request.DayPlanId);
        if (!dayPlanExists) return null;

        mealSlot.MealType = request.MealType;
        mealSlot.DayPlanId = request.DayPlanId;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var mealSlot = await _db.MealSlots.FindAsync(id);
        if (mealSlot is null) return false;

        _db.MealSlots.Remove(mealSlot);
        await _db.SaveChangesAsync();

        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IQueryable<MealSlot> QueryWithIncludes() =>
        _db.MealSlots
            .Include(ms => ms.MealSlotItems)
                .ThenInclude(msi => msi.Item);

    private static MealSlotResponse MapToResponse(MealSlot ms) => new()
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
        MealSlotId = msi.MealSlotId
    };
}
