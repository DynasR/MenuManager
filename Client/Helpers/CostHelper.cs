using System.Globalization;
using MenuManager.Client.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Enums;

namespace MenuManager.Client.Helpers;

public static class CostHelper
{
    public static readonly CultureInfo Fr = new("fr-FR");

    /// <summary>
    /// Retourne le prix au kg ou au litre formaté (ex. "1,50 €/kg"), ou null si l'unité n'est pas massique/volumique.
    /// </summary>
    public static string? ComputePricePerKgL(decimal unitPrice, decimal contentQty, MeasurementUnit contentUnit)
    {
        if (contentQty == 0) return null;
        static string Fmt(decimal v) => v.ToString("0.00 €", Fr);
        return contentUnit switch
        {
            MeasurementUnit.Gram       => Fmt(unitPrice / contentQty * 1000) + "/kg",
            MeasurementUnit.Kilogram   => Fmt(unitPrice / contentQty) + "/kg",
            MeasurementUnit.Milliliter => Fmt(unitPrice / contentQty * 1000) + "/L",
            MeasurementUnit.Liter      => Fmt(unitPrice / contentQty) + "/L",
            _                          => null
        };
    }


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

    /// <summary>
    /// Retourne les PaymentType distincts des ingrédients d'une recette (via le cache fournisseur).
    /// </summary>
    public static IReadOnlyList<PaymentType> GetRecipePaymentTypes(IReadOnlyList<int> ingredientIds, ItemSupplierCache cache) =>
        ingredientIds
            .Select(id => cache.GetBestSupplier(id)?.PaymentType)
            .Where(pt => pt.HasValue)
            .Select(pt => pt!.Value)
            .Distinct()
            .Order()
            .ToList();

    /// <summary>
    /// Répartit un coût dans les buckets TR/CB selon la liste de PaymentType d'une recette.
    /// - 0 type → rien ajouté.
    /// - 1 type → coût entier dans le bucket correspondant.
    /// - Mixte  → 50/50.
    /// </summary>
    public static (decimal tr, decimal cb) BucketByCost(decimal cost, IReadOnlyList<PaymentType> paymentTypes)
    {
        if (paymentTypes.Count == 0) return (0m, 0m);
        if (paymentTypes.Count == 1)
            return paymentTypes[0] == PaymentType.TR ? (cost, 0m) : (0m, cost);
        return (cost / 2, cost / 2);
    }
}
