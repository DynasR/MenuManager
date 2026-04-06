using MenuManager.Server.Data;
using MenuManager.Server.Mapping;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IDailyMenuService
{
    Task<List<DailyMenuResponse>> GetAllAsync();
    Task<List<DailyMenuResponse>> GetByCustomerAsync(int customerId);
    Task<DailyMenuResponse?> GetByIdAsync(int id);
    Task<DailyMenuResponse?> CreateAsync(CreateDailyMenuRequest request);
    Task<DailyMenuResponse?> UpdateAsync(int id, UpdateDailyMenuRequest request);
    Task<bool> DeleteAsync(int id);
    Task<List<MonthlySummaryResponse>> GetMonthlySummaryAsync(int customerId);
    Task<bool> DuplicateMonthAsync(DuplicateMonthRequest request);
}

public class DailyMenuService : IDailyMenuService
{
    private readonly AppDbContext _db;

    public DailyMenuService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DailyMenuResponse>> GetAllAsync()
    {
        var dailyMenus = await QueryWithIncludes()
            .AsNoTracking()
            .ToListAsync();

        return dailyMenus.Select(MapToResponse).ToList();
    }

    public async Task<List<DailyMenuResponse>> GetByCustomerAsync(int customerId)
    {
        var dailyMenus = await QueryWithIncludes()
            .AsNoTracking()
            .Where(dm => dm.CustomerId == customerId)
            .ToListAsync();

        return dailyMenus.Select(MapToResponse).ToList();
    }

    public async Task<DailyMenuResponse?> GetByIdAsync(int id)
    {
        var dailyMenu = await QueryWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(dm => dm.Id == id);

        return dailyMenu is null ? null : MapToResponse(dailyMenu);
    }

    public async Task<DailyMenuResponse?> CreateAsync(CreateDailyMenuRequest request)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId);
        if (!customerExists) return null;

        var dailyMenu = new DailyMenu
        {
            Date = request.Date,
            CustomerId = request.CustomerId
        };

        _db.DailyMenus.Add(dailyMenu);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(dailyMenu.Id);
    }

    public async Task<DailyMenuResponse?> UpdateAsync(int id, UpdateDailyMenuRequest request)
    {
        var dailyMenu = await _db.DailyMenus.FindAsync(id);
        if (dailyMenu is null) return null;

        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId);
        if (!customerExists) return null;

        dailyMenu.Date = request.Date;
        dailyMenu.CustomerId = request.CustomerId;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var dailyMenu = await _db.DailyMenus.FindAsync(id);
        if (dailyMenu is null) return false;

        _db.DailyMenus.Remove(dailyMenu);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DuplicateMonthAsync(DuplicateMonthRequest request)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId);
        if (!customerExists) return false;

        // Load source month
        var sourceDailyMenus = await _db.DailyMenus
            .Where(dm => dm.CustomerId == request.CustomerId
                      && dm.Date.Year == request.SourceYear
                      && dm.Date.Month == request.SourceMonth)
            .Include(dm => dm.Meals)
                .ThenInclude(m => m.MealItems)
            .ToListAsync();

        // Delete target month (cascade removes Meals and MealItems)
        var targetDailyMenus = await _db.DailyMenus
            .Where(dm => dm.CustomerId == request.CustomerId
                      && dm.Date.Year == request.TargetYear
                      && dm.Date.Month == request.TargetMonth)
            .ToListAsync();

        if (targetDailyMenus.Count > 0)
        {
            _db.DailyMenus.RemoveRange(targetDailyMenus);
            await _db.SaveChangesAsync();
        }

        // Recreate in target month — build full graph, save once
        int targetDaysInMonth = DateTime.DaysInMonth(request.TargetYear, request.TargetMonth);

        var newMenus = sourceDailyMenus
            .Where(sm => sm.Date.Day <= targetDaysInMonth)
            .Select(sm => new DailyMenu
            {
                Date = new DateOnly(request.TargetYear, request.TargetMonth, sm.Date.Day),
                CustomerId = request.CustomerId,
                Meals = sm.Meals.Select(sourceMeal => new Meal
                {
                    MealType = sourceMeal.MealType,
                    MealItems = sourceMeal.MealItems.Select(sourceItem => new MealItem
                    {
                        ItemId = sourceItem.ItemId,
                        RecipeId = sourceItem.RecipeId,
                        Quantity = sourceItem.Quantity,
                        Notes = sourceItem.Notes,
                        Order = sourceItem.Order,
                        Unit = sourceItem.Unit
                    }).ToList()
                }).ToList()
            })
            .ToList();

        _db.DailyMenus.AddRange(newMenus);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<MonthlySummaryResponse>> GetMonthlySummaryAsync(int customerId)
    {
        var dailyMenus = await _db.DailyMenus
            .Where(dm => dm.CustomerId == customerId)
            .Include(dm => dm.Meals)
                .ThenInclude(m => m.MealItems)
                    .ThenInclude(mi => mi.Item)
                        .ThenInclude(i => i!.ItemSuppliers)
            .Include(dm => dm.Meals)
                .ThenInclude(m => m.MealItems)
                    .ThenInclude(mi => mi.Recipe)
                        .ThenInclude(r => r!.RecipeIngredients)
                            .ThenInclude(ri => ri.Item)
                                .ThenInclude(i => i!.ItemSuppliers)
            .AsNoTracking()
            .ToListAsync();

        return dailyMenus
            .GroupBy(dm => (dm.Date.Year, dm.Date.Month))
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlySummaryResponse
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                HasMeals = g.Any(dm => dm.Meals.Any(m => m.MealItems.Count > 0)),
                MonthlyCost = ComputeMonthlyCost(g.SelectMany(dm => dm.Meals).SelectMany(m => m.MealItems)),
                DaysWithMeals = g.Count(dm => dm.Meals.Any(m => m.MealItems.Count > 0))
            })
            .ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static decimal ComputeMonthlyCost(IEnumerable<MealItem> items)
    {
        return items.Sum(mi =>
        {
            if (mi.Item is not null)
            {
                var unitPrice = mi.Item.ItemSuppliers
                    .Where(s => s.IsAvailable)
                    .OrderBy(s => s.UnitPrice)
                    .Select(s => (decimal?)s.UnitPrice)
                    .FirstOrDefault();

                if (unitPrice is null) return 0m;
                return (int)Math.Ceiling(mi.Quantity / mi.Item.ContentQuantity) * unitPrice.Value;
            }
            if (mi.Recipe is not null)
                return RecipeService.ComputeRecipeCost(mi.Recipe) / Math.Max(mi.Recipe.BaseServings, 1) * mi.Quantity;
            return 0m;
        });
    }

    private IQueryable<DailyMenu> QueryWithIncludes() =>
        _db.DailyMenus
            .Include(dm => dm.Meals)
                .ThenInclude(m => m.MealItems)
                    .ThenInclude(mi => mi.Item)
                        .ThenInclude(i => i!.ItemSuppliers)
            .Include(dm => dm.Meals)
                .ThenInclude(m => m.MealItems)
                    .ThenInclude(mi => mi.Recipe)
                        .ThenInclude(r => r!.RecipeIngredients)
                            .ThenInclude(ri => ri.Item)
                                .ThenInclude(i => i!.ItemSuppliers);

    private static DailyMenuResponse MapToResponse(DailyMenu dm) => new()
    {
        Id = dm.Id,
        Date = dm.Date,
        CustomerId = dm.CustomerId,
        Meals = dm.Meals.Select(MapMealToResponse).ToList()
    };

    private static MealResponse MapMealToResponse(Meal m) => new()
    {
        Id = m.Id,
        MealType = m.MealType,
        DailyMenuId = m.DailyMenuId,
        MealItems = m.MealItems.Select(MealItemMapper.ToResponse).ToList()
    };
}
