using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IMealService
{
    Task<List<MealResponse>> GetAllAsync();
    Task<MealResponse?> GetByIdAsync(int id);
    Task<CreateMealResult> CreateAsync(CreateMealRequest request);
    Task<MealResponse?> UpdateAsync(int id, UpdateMealRequest request);
    Task<bool> DeleteAsync(int id);
}

public class MealService : IMealService
{
    private readonly AppDbContext _db;

    public MealService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<MealResponse>> GetAllAsync()
    {
        var meals = await QueryWithIncludes()
            .AsNoTracking()
            .ToListAsync();

        return meals.Select(MapToResponse).ToList();
    }

    public async Task<MealResponse?> GetByIdAsync(int id)
    {
        var meal = await QueryWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        return meal is null ? null : MapToResponse(meal);
    }

    public async Task<CreateMealResult> CreateAsync(CreateMealRequest request)
    {
        var dailyMenuExists = await _db.DailyMenus.AnyAsync(dm => dm.Id == request.DailyMenuId);
        if (!dailyMenuExists)
            return new CreateMealResult(null, CreateMealError.DailyMenuNotFound);

        var alreadyExists = await _db.Meals.AnyAsync(
            m => m.DailyMenuId == request.DailyMenuId && m.MealType == request.MealType);
        if (alreadyExists)
            return new CreateMealResult(null, CreateMealError.AlreadyExists);

        var meal = new Meal
        {
            MealType = request.MealType,
            DailyMenuId = request.DailyMenuId
        };

        _db.Meals.Add(meal);
        await _db.SaveChangesAsync();

        return new CreateMealResult(await GetByIdAsync(meal.Id), null);
    }

    public async Task<MealResponse?> UpdateAsync(int id, UpdateMealRequest request)
    {
        var meal = await _db.Meals.FindAsync(id);
        if (meal is null) return null;

        var dailyMenuExists = await _db.DailyMenus.AnyAsync(dm => dm.Id == request.DailyMenuId);
        if (!dailyMenuExists) return null;

        meal.MealType = request.MealType;
        meal.DailyMenuId = request.DailyMenuId;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var meal = await _db.Meals.FindAsync(id);
        if (meal is null) return false;

        _db.Meals.Remove(meal);
        await _db.SaveChangesAsync();

        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IQueryable<Meal> QueryWithIncludes() =>
        _db.Meals
            .Include(m => m.MealItems)
                .ThenInclude(mi => mi.Item)
                    .ThenInclude(i => i.ItemSuppliers);

    private static MealResponse MapToResponse(Meal m) => new()
    {
        Id = m.Id,
        MealType = m.MealType,
        DailyMenuId = m.DailyMenuId,
        MealItems = m.MealItems.Select(MapMealItemToResponse).ToList()
    };

    private static MealItemResponse MapMealItemToResponse(MealItem mi) => new()
    {
        Id = mi.Id,
        ItemId = mi.ItemId ?? 0,
        ItemName = mi.Item?.Name ?? "",
        Quantity = mi.Quantity,
        Notes = mi.Notes,
        Order = mi.Order,
        MealId = mi.MealId,
        UnitPrice = mi.Item?.ItemSuppliers
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.SupplierId)
            .Select(s => (decimal?)s.UnitPrice)
            .FirstOrDefault(),
        PackageSize = mi.Item?.PackageSize ?? 1,
        Unit = mi.Item?.Unit ?? default
    };
}
