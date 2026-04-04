using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IRecipeIngredientService
{
    Task<RecipeIngredientResponse?> AddIngredientAsync(RecipeIngredientRequest request);
    Task<bool> RemoveIngredientAsync(int recipeId, int itemId);
    Task<RecipeIngredientResponse?> UpdateQuantityAsync(int recipeId, int itemId, decimal quantity);
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
            Quantity = request.Quantity
        };

        _db.RecipeIngredients.Add(ingredient);
        await _db.SaveChangesAsync();

        var item = await _db.Items.FindAsync(request.ItemId);

        return new RecipeIngredientResponse
        {
            RecipeId = ingredient.RecipeId,
            ItemId = ingredient.ItemId,
            ItemName = item?.Name ?? "",
            Quantity = ingredient.Quantity
        };
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

    public async Task<RecipeIngredientResponse?> UpdateQuantityAsync(int recipeId, int itemId, decimal quantity)
    {
        var ingredient = await _db.RecipeIngredients
            .Include(ri => ri.Item)
            .FirstOrDefaultAsync(ri => ri.RecipeId == recipeId && ri.ItemId == itemId);

        if (ingredient is null) return null;

        ingredient.Quantity = quantity;
        await _db.SaveChangesAsync();

        return new RecipeIngredientResponse
        {
            RecipeId = ingredient.RecipeId,
            ItemId = ingredient.ItemId,
            ItemName = ingredient.Item?.Name ?? "",
            Quantity = ingredient.Quantity
        };
    }
}
