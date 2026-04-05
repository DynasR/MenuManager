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
    Task DeleteBatchAsync(List<int> ids);
    Task<List<DailyMenuResponse>> RandomFillAsync(RandomFillRequest request);
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

    public async Task DeleteBatchAsync(List<int> ids)
    {
        if (ids.Count == 0) return;

        var meals = await _db.Meals
            .Where(m => ids.Contains(m.Id))
            .ToListAsync();

        if (meals.Count == 0) return;

        _db.Meals.RemoveRange(meals);
        await _db.SaveChangesAsync();
    }

    public async Task<List<DailyMenuResponse>> RandomFillAsync(RandomFillRequest request)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId);
        if (!customerExists) return [];

        var availableItemIds = await _db.Items
            .Where(i => i.ItemSuppliers.Any(s => s.IsAvailable))
            .Select(i => i.Id)
            .ToListAsync();

        if (availableItemIds.Count == 0) return [];

        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);
        var monthStart = new DateOnly(request.Year, request.Month, 1);
        var monthEnd = new DateOnly(request.Year, request.Month, daysInMonth);

        // Load existing daily menus; skip days that already have at least one meal
        var existingDailyMenus = await _db.DailyMenus
            .Include(dm => dm.Meals)
            .Where(dm => dm.CustomerId == request.CustomerId
                      && dm.Date >= monthStart
                      && dm.Date <= monthEnd)
            .ToListAsync();

        var datesWithMeals = existingDailyMenus
            .Where(dm => dm.Meals.Count > 0)
            .Select(dm => dm.Date)
            .ToHashSet();

        // Generate random meals
        var rng = new Random();
        (MealType type, int min, int max)[] ranges =
        [
            (MealType.Breakfast,      0, 2),
            (MealType.MorningSnack,   0, 1),
            (MealType.Lunch,          1, 3),
            (MealType.AfternoonSnack, 0, 1),
            (MealType.Dinner,         1, 3),
        ];

        var dailyMenusByDate = existingDailyMenus.ToDictionary(dm => dm.Date);

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(request.Year, request.Month, day);

            if (datesWithMeals.Contains(date)) continue;

            foreach (var (mealType, min, max) in ranges)
            {
                var count = rng.Next(min, max + 1);
                if (count == 0) continue;

                if (!dailyMenusByDate.TryGetValue(date, out var dailyMenu))
                {
                    dailyMenu = new DailyMenu { Date = date, CustomerId = request.CustomerId };
                    _db.DailyMenus.Add(dailyMenu);
                    dailyMenusByDate[date] = dailyMenu;
                }

                var meal = new Meal { MealType = mealType, DailyMenu = dailyMenu };
                _db.Meals.Add(meal);

                var pickedIds = availableItemIds.OrderBy(_ => rng.Next()).Take(count).ToList();
                int order = 1;
                foreach (var itemId in pickedIds)
                {
                    _db.MealItems.Add(new MealItem
                    {
                        ItemId = itemId,
                        Meal = meal,
                        Quantity = 1,
                        Order = order++
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        var result = await _db.DailyMenus
            .Where(dm => dm.CustomerId == request.CustomerId
                      && dm.Date >= monthStart
                      && dm.Date <= monthEnd)
            .Include(dm => dm.Meals)
                .ThenInclude(m => m.MealItems)
                    .ThenInclude(mi => mi.Item)
                        .ThenInclude(i => i!.ItemSuppliers)
            .AsNoTracking()
            .ToListAsync();

        return result.Select(MapDailyMenuToResponse).ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IQueryable<Meal> QueryWithIncludes() =>
        _db.Meals
            .Include(m => m.MealItems)
                .ThenInclude(mi => mi.Item)
                    .ThenInclude(i => i.ItemSuppliers);

    private static DailyMenuResponse MapDailyMenuToResponse(DailyMenu dm) => new()
    {
        Id = dm.Id,
        Date = dm.Date,
        CustomerId = dm.CustomerId,
        Meals = dm.Meals.Select(MapToResponse).ToList()
    };

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
