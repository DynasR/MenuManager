using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IMealItemService
{
    Task<List<MealItemResponse>> GetAllAsync();
    Task<MealItemResponse?> GetByIdAsync(int id);
    Task<MealItemResponse?> CreateAsync(CreateMealItemRequest request);
    Task<MealItemResponse?> UpdateAsync(int id, UpdateMealItemRequest request);
    Task<bool> DeleteAsync(int id);
    Task<MealItemResponse?> MoveAsync(int id, MoveMealItemRequest request);
    Task<bool> ReorderAsync(ReorderMealItemsRequest request);
}

public class MealItemService : IMealItemService
{
    private readonly AppDbContext _db;

    public MealItemService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<MealItemResponse>> GetAllAsync()
    {
        var items = await _db.MealItems
            .Include(mi => mi.Item)
                .ThenInclude(i => i!.ItemSuppliers)
            .Include(mi => mi.Recipe)
                .ThenInclude(r => r!.RecipeIngredients)
                    .ThenInclude(ri => ri.Item)
                        .ThenInclude(i => i!.ItemSuppliers)
            .AsNoTracking()
            .ToListAsync();

        return items.Select(MapToResponse).ToList();
    }

    public async Task<MealItemResponse?> GetByIdAsync(int id)
    {
        var item = await _db.MealItems
            .Include(mi => mi.Item)
                .ThenInclude(i => i!.ItemSuppliers)
            .Include(mi => mi.Recipe)
                .ThenInclude(r => r!.RecipeIngredients)
                    .ThenInclude(ri => ri.Item)
                        .ThenInclude(i => i!.ItemSuppliers)
            .AsNoTracking()
            .FirstOrDefaultAsync(mi => mi.Id == id);

        return item is null ? null : MapToResponse(item);
    }

    public async Task<MealItemResponse?> CreateAsync(CreateMealItemRequest request)
    {
        var mealExists = await _db.Meals.AnyAsync(m => m.Id == request.MealId);
        if (!mealExists) return null;

        if (request.ItemId.HasValue)
        {
            var itemExists = await _db.Items.AnyAsync(i => i.Id == request.ItemId.Value);
            if (!itemExists) return null;
        }
        else if (request.RecipeId.HasValue)
        {
            var recipeExists = await _db.Recipes.AnyAsync(r => r.Id == request.RecipeId.Value);
            if (!recipeExists) return null;
        }
        else
        {
            return null;
        }

        var currentCount = await _db.MealItems.CountAsync(mi => mi.MealId == request.MealId);

        var mealItem = new MealItem
        {
            Quantity = request.Quantity,
            Notes = request.Notes,
            MealId = request.MealId,
            ItemId = request.ItemId,
            RecipeId = request.RecipeId,
            Unit = request.Unit,
            Order = currentCount + 1
        };

        _db.MealItems.Add(mealItem);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(mealItem.Id);
    }

    public async Task<MealItemResponse?> UpdateAsync(int id, UpdateMealItemRequest request)
    {
        var mealItem = await _db.MealItems.FindAsync(id);
        if (mealItem is null) return null;

        var mealExists = await _db.Meals.AnyAsync(m => m.Id == request.MealId);
        if (!mealExists) return null;

        var itemExists = await _db.Items.AnyAsync(i => i.Id == request.ItemId);
        if (!itemExists) return null;

        mealItem.Quantity = request.Quantity;
        mealItem.Notes = request.Notes;
        mealItem.MealId = request.MealId;
        mealItem.ItemId = request.ItemId;
        mealItem.Unit = request.Unit;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var mealItem = await _db.MealItems.FindAsync(id);
        if (mealItem is null) return false;

        _db.MealItems.Remove(mealItem);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<MealItemResponse?> MoveAsync(int id, MoveMealItemRequest request)
    {
        var mealItem = await _db.MealItems
            .Include(mi => mi.Meal)
                .ThenInclude(m => m.DailyMenu)
            .FirstOrDefaultAsync(mi => mi.Id == id);

        if (mealItem is null) return null;

        var sourceMealId = mealItem.MealId;
        var customerId = mealItem.Meal.DailyMenu.CustomerId;

        // Resolve target DailyMenu (on-demand)
        var targetDailyMenu = await _db.DailyMenus
            .FirstOrDefaultAsync(dm => dm.CustomerId == customerId && dm.Date == request.TargetDate);

        if (targetDailyMenu is null)
        {
            targetDailyMenu = new DailyMenu { Date = request.TargetDate, CustomerId = customerId };
            _db.DailyMenus.Add(targetDailyMenu);
            await _db.SaveChangesAsync();
        }

        // Resolve target Meal (on-demand)
        var targetMeal = await _db.Meals
            .FirstOrDefaultAsync(m => m.DailyMenuId == targetDailyMenu.Id && m.MealType == request.TargetMealType);

        if (targetMeal is null)
        {
            targetMeal = new Meal { DailyMenuId = targetDailyMenu.Id, MealType = request.TargetMealType };
            _db.Meals.Add(targetMeal);
            await _db.SaveChangesAsync();
        }

        // Move item to target meal
        mealItem.MealId = targetMeal.Id;
        await _db.SaveChangesAsync();

        // Renumber source meal (fill the gap left by removed item)
        if (sourceMealId != targetMeal.Id)
            await RenumberMealAsync(sourceMealId);

        // Renumber target meal with item inserted at desired position
        var targetItems = await _db.MealItems
            .Where(mi => mi.MealId == targetMeal.Id)
            .OrderBy(mi => mi.Order)
            .ToListAsync();

        targetItems.Remove(mealItem);
        var insertIdx = Math.Clamp(request.NewOrder, 0, targetItems.Count);
        targetItems.Insert(insertIdx, mealItem);

        for (int i = 0; i < targetItems.Count; i++)
            targetItems[i].Order = i + 1;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> ReorderAsync(ReorderMealItemsRequest request)
    {
        var items = await _db.MealItems
            .Where(mi => mi.MealId == request.MealId)
            .ToListAsync();

        if (items.Count == 0) return false;
        if (request.OrderedItemIds.Count != items.Count) return false;

        var itemDict = items.ToDictionary(i => i.Id);

        for (int i = 0; i < request.OrderedItemIds.Count; i++)
        {
            if (!itemDict.TryGetValue(request.OrderedItemIds[i], out var item))
                return false;

            item.Order = i + 1;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task RenumberMealAsync(int mealId)
    {
        var items = await _db.MealItems
            .Where(mi => mi.MealId == mealId)
            .OrderBy(mi => mi.Order)
            .ToListAsync();

        for (int i = 0; i < items.Count; i++)
            items[i].Order = i + 1;

        await _db.SaveChangesAsync();
    }

    private static MealItemResponse MapToResponse(MealItem mi) => new()
    {
        Id = mi.Id,
        ItemId = mi.ItemId ?? 0,
        ItemName = mi.Item?.Name ?? "",
        RecipeId = mi.RecipeId,
        RecipeName = mi.Recipe?.Name,
        RecipeEstimatedCost = mi.Recipe != null ? RecipeService.ComputeRecipeCost(mi.Recipe) : null,
        Quantity = mi.Quantity,
        Notes = mi.Notes,
        Order = mi.Order,
        Unit = mi.Unit,
        MealId = mi.MealId,
        UnitPrice = mi.Item?.ItemSuppliers
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.SupplierId)
            .Select(s => (decimal?)s.UnitPrice)
            .FirstOrDefault(),
        ContentQuantity = mi.Item?.ContentQuantity ?? 1,
        PurchaseUnit = mi.Item?.PurchaseUnit ?? default,
        ContentUnit = mi.Item?.ContentUnit ?? default
    };
}
