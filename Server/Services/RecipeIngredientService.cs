using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IRecipeIngredientService
{
    Task<RecipeIngredientResponse?> AddIngredientAsync(RecipeIngredientRequest request);
    Task<bool> RemoveIngredientAsync(int recipeId, int itemId);
    Task<RecipeIngredientResponse?> UpdateAsync(int recipeId, int itemId, RecipeIngredientRequest request);
}

public class RecipeIngredientService : IRecipeIngredientService
{
    private readonly AppDbContext _db;

    public RecipeIngredientService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RecipeIngredientResponse?> AddIngredientAsync(RecipeIngredientRequest request)
    {
        var recipeExists = await _db.Recipes.AnyAsync(r => r.Id == request.RecipeId);
        if (!recipeExists) return null;

        var itemExists = await _db.Items.AnyAsync(i => i.Id == request.ItemId);
        if (!itemExists) return null;

        var alreadyExists = await _db.RecipeIngredients
            .AnyAsync(ri => ri.RecipeId == request.RecipeId && ri.ItemId == request.ItemId);
        if (alreadyExists) return null;

        var ingredient = new RecipeIngredient
        {
            RecipeId = request.RecipeId,
            ItemId = request.ItemId,
            Quantity = request.Quantity,
            Unit = request.Unit,
            Order = request.Order
        };

        _db.RecipeIngredients.Add(ingredient);
        await _db.SaveChangesAsync();

        var item = await _db.Items
            .Include(i => i.ItemSuppliers)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId);

        return MapToResponse(ingredient, item);
    }

    public async Task<bool> RemoveIngredientAsync(int recipeId, int itemId)
    {
        var ingredient = await _db.RecipeIngredients
            .FirstOrDefaultAsync(ri => ri.RecipeId == recipeId && ri.ItemId == itemId);

        if (ingredient is null) return false;

        _db.RecipeIngredients.Remove(ingredient);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<RecipeIngredientResponse?> UpdateAsync(int recipeId, int itemId, RecipeIngredientRequest request)
    {
        var ingredient = await _db.RecipeIngredients
            .Include(ri => ri.Item)
                .ThenInclude(i => i!.ItemSuppliers)
            .FirstOrDefaultAsync(ri => ri.RecipeId == recipeId && ri.ItemId == itemId);

        if (ingredient is null) return null;

        ingredient.Quantity = request.Quantity;
        ingredient.Unit = request.Unit;
        ingredient.Order = request.Order;
        await _db.SaveChangesAsync();

        return MapToResponse(ingredient, ingredient.Item);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RecipeIngredientResponse MapToResponse(RecipeIngredient ri, Item? item) => new()
    {
        RecipeId = ri.RecipeId,
        ItemId = ri.ItemId,
        ItemName = item?.Name ?? "",
        Quantity = ri.Quantity,
        Unit = ri.Unit,
        Order = ri.Order,
        PurchaseUnit = item?.PurchaseUnit ?? default,
        ContentUnit = item?.ContentUnit ?? default,
        ContentQuantity = item?.ContentQuantity ?? 1,
        UnitPrice = item?.ItemSuppliers
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.SupplierId)
            .Select(s => (decimal?)s.UnitPrice)
            .FirstOrDefault()
    };
}
