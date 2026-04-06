using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;

namespace MenuManager.Server.Mapping;

public static class MealItemMapper
{
    public static MealItemResponse ToResponse(MealItem mi) => new()
    {
        Id = mi.Id,
        ItemId = mi.ItemId ?? 0,
        ItemName = mi.Item?.Name ?? "",
        RecipeId = mi.RecipeId,
        RecipeName = mi.Recipe?.Name,
        RecipeEstimatedCost = mi.Recipe != null ? RecipeService.ComputeRecipeCost(mi.Recipe) : null,
        RecipeBaseServings = mi.Recipe?.BaseServings ?? 1,
        RecipeIngredientItemIds = mi.Recipe?.RecipeIngredients.Select(ri => ri.ItemId).ToList() ?? [],
        Quantity = mi.Quantity,
        Notes = mi.Notes,
        Order = mi.Order,
        Unit = mi.Unit,
        MealId = mi.MealId,
        UnitPrice = mi.Item?.ItemSuppliers
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.UnitPrice)
            .Select(s => (decimal?)s.UnitPrice)
            .FirstOrDefault(),
        ContentQuantity = mi.Item?.ContentQuantity ?? 1,
        PurchaseUnit = mi.Item?.PurchaseUnit ?? default,
        ContentUnit = mi.Item?.ContentUnit ?? default
    };
}
