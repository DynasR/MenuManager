using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IRecipeService
{
    Task<List<RecipeResponse>> GetAllAsync();
    Task<RecipeResponse?> GetByIdAsync(int id);
    Task<RecipeResponse> CreateAsync(CreateRecipeRequest request);
    Task<RecipeResponse?> UpdateAsync(int id, UpdateRecipeRequest request);
    Task<bool> DeleteAsync(int id);
}

public class RecipeService : IRecipeService
{
    private readonly AppDbContext _db;

    public RecipeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<RecipeResponse>> GetAllAsync()
    {
        var recipes = await _db.Recipes
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Item)
            .AsNoTracking()
            .ToListAsync();

        return recipes.Select(MapToResponse).ToList();
    }

    public async Task<RecipeResponse?> GetByIdAsync(int id)
    {
        var recipe = await _db.Recipes
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Item)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        return recipe is null ? null : MapToResponse(recipe);
    }

    public async Task<RecipeResponse> CreateAsync(CreateRecipeRequest request)
    {
        var recipe = new Recipe
        {
            Name = request.Name,
            Description = request.Description,
            BaseServings = request.BaseServings
        };

        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        return MapToResponse(recipe);
    }

    public async Task<RecipeResponse?> UpdateAsync(int id, UpdateRecipeRequest request)
    {
        var recipe = await _db.Recipes
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Item)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe is null) return null;

        recipe.Name = request.Name;
        recipe.Description = request.Description;
        recipe.BaseServings = request.BaseServings;

        await _db.SaveChangesAsync();

        return MapToResponse(recipe);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var recipe = await _db.Recipes.FindAsync(id);
        if (recipe is null) return false;

        _db.Recipes.Remove(recipe);
        await _db.SaveChangesAsync();

        return true;
    }

    private static RecipeResponse MapToResponse(Recipe r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        BaseServings = r.BaseServings,
        Ingredients = r.RecipeIngredients.Select(ri => new RecipeIngredientResponse
        {
            RecipeId = ri.RecipeId,
            ItemId = ri.ItemId,
            ItemName = ri.Item?.Name ?? "",
            Quantity = ri.Quantity
        }).ToList()
    };
}
