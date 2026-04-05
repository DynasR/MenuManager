using MenuManager.Shared.DTOs;

namespace MenuManager.Client.Helpers;

public static class CostHelper
{
    /// <summary>
    /// Calcule le coût d'un lot : ceil(totalQty / contentQty) × unitPrice.
    /// C'est la formule canonique utilisée partout (cellule repas, totaux ligne/colonne, panier).
    /// </summary>
    public static decimal PackageCost(decimal totalQty, decimal contentQty, decimal unitPrice)
    {
        if (contentQty <= 0) contentQty = 1m;
        return (decimal)Math.Ceiling(totalQty / contentQty) * unitPrice;
    }

    /// <summary>
    /// Coût d'un MealItemResponse individuel.
    /// - Recette : RecipeEstimatedCost × Quantity (si null → 0).
    /// - Article  : PackageCost(Quantity, ContentQuantity, bestUnitPrice ?? item.UnitPrice).
    /// bestUnitPrice provient de ItemSupplierCache et prend la priorité sur item.UnitPrice.
    /// </summary>
    public static decimal ComputeItemCost(MealItemResponse item, decimal? bestUnitPrice = null)
    {
        if (item.RecipeId.HasValue)
            return (item.RecipeEstimatedCost ?? 0m) * item.Quantity;

        var price = bestUnitPrice ?? item.UnitPrice;
        if (price is null) return 0m;

        return PackageCost(item.Quantity, item.ContentQuantity, price.Value);
    }
}
