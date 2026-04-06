# TODO — MenuManager

> Tâches concrètes et actionnables. Ordonnées par priorité.
> Mettre à jour après chaque tâche complétée.

---

## Backlog existant

- **Catégories** : passer la vue en tree view.
- **ItemSupplier** : double-clic sur une ligne → préremplit une nouvelle ligne draft avec les mêmes valeurs (en excluant le fournisseur déjà associé à cet item de la liste déroulante). Si aucun autre fournisseur disponible, afficher un dialog "fonctionnalité à venir".

---

## Bugs (KI — Known Issues)

### KI-2 — Inclure les recettes dans le Shopping Cart
**Impact** : totaux TR/CB/Total du panier incomplets dès qu'une recette est dans le plan. Message "No items" peut s'afficher alors que le plan a des recettes.
- `Client/Components/ShoppingCart.razor` — modifier `ComputeLines()` :
  1. Séparer `plainItems` et `recipeItems`.
  2. Pour chaque `recipeItem` : itérer `RecipeIngredientItemIds`, appeler `Cache.GetBestSupplier(id)`, calculer quantité ingrédient en fonction de `item.Quantity` (portions servies).
  3. Décision à valider avec Lead Dev : fusionner les lignes ingrédients avec les items plain (même ItemId → même ligne) ou section séparée ?

### KI-4 — Optimiser DailyMenuService.DuplicateMonthAsync (N×M SaveChangesAsync)
**Impact** : performance pour un mois plein (~160 saves pour 31 jours × 5 repas).
- `Server/Services/DailyMenuService.cs:101-172`
- Passe 1 : AddRange tous les `DailyMenu` cibles → `SaveChangesAsync` une fois.
- Passe 2 : AddRange tous les `Meal` + `MealItem` → `SaveChangesAsync` une fois.
