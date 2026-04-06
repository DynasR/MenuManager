# TODO — Audit automatique
> Généré le 2026-04-06. Full scan. Remplace la version précédente.

---

## ★★★ Bloquant

| # | Fichier | Catégorie | Description |
|---|---------|-----------|-------------|
| 1 | `Server/Services/RecipeIngredientService.cs:100` | BUG | `MapToResponse` trie les fournisseurs par `SupplierId` au lieu de `UnitPrice` — le prix unitaire retourné pour un ingrédient est aléatoire selon l'ordre d'insertion, incohérent avec tous les autres services |
| 2 | `Server/Services/MealItemService.cs` (MoveAsync) | PERFORMANCE | 4 à 5 appels `SaveChangesAsync` séquentiels dans `MoveAsync` (création DailyMenu, création Meal, déplacement item, renumber source, renumber target) — peut provoquer des états intermédiaires invalides si la contrainte unique `(DailyMenuId, MealType)` est violée |
| 3 | `Server/Program.cs` | SÉCURITÉ | Validators FluentValidation déclarés dans `Shared/Validators/` mais jamais enregistrés dans le pipeline ASP.NET — toute validation côté serveur est silencieusement ignorée |

---

## ★★ Important

| # | Fichier | Catégorie | Description |
|---|---------|-----------|-------------|
| 4 | `Server/Services/ItemSupplierService.cs:154` | BUG | `MapToResponse` retourne `isp.Supplier.Name` (pas `CompanyName ?? Name`) — `SupplierName` peut être vide pour les Suppliers qui ont un `CompanyName` non null |
| 5 | `Client/Pages/DayPlans/Index.razor:665` | PERFORMANCE | `GetByCustomerAsync` charge tous les DailyMenus du client sans filtre de mois à chaque `LoadAsync()` — ajouter un filtre `year`+`month` côté service |
| 6 | `Client/Pages/DayPlans/Index.razor:720-744` | PERFORMANCE | `ApplyDialogSaveAsync` exécute deletes, updates et adds en séquentiel dans des boucles `foreach` avec `await` — les deletes et updates sans dépendance devraient utiliser `Task.WhenAll` |
| 7 | `Client/Pages/DayPlans/Index.razor:813-828` | PERFORMANCE | `ExecuteCellActionAsync` (cell drag-copy) boucle `await AddItemToSlotAsync` + deuxième boucle `await DeleteAsync` séquentielle — jusqu'à 2×N appels HTTP séquentiels |
| 8 | `Client/Pages/DayPlans/Index.razor:924-930` | PERFORMANCE | `ClearCellAsync` boucle `foreach` avec `await MealItemSvc.DeleteAsync(item.Id)` — devrait utiliser `MealSvc.DeleteBatchAsync` avec les MealIds |
| 9 | `Client/Services/ItemSupplierCache.cs` | MAINTENABILITÉ | Cache jamais invalidé lors d'un update `ItemSupplier` — les prix/badges TR/CB restent périmés pour toute la session après changement de fournisseur |
| 10 | `Client/Pages/DayPlans/Index.razor:643+665` | PERFORMANCE | Deux appels réseau séquentiels dans `OnParametersSetAsync` (`GetMonthlySummaryAsync` puis `GetByCustomerAsync`) — à lancer en `Task.WhenAll` |
| 11 | `Server/Services/MealService.cs` (QueryWithIncludes) | BUG | `QueryWithIncludes` n'inclut pas `Recipe.RecipeIngredients.Item.ItemSuppliers` — `MealItemMapper.ToResponse` attend ces nav-props pour les recipe slots mais ils ne sont pas chargés via `GetAllAsync`/`GetByIdAsync` de MealService |

---

## ★ Mineur / Quick win

| # | Fichier | Catégorie | Description |
|---|---------|-----------|-------------|
| 12 | `Shared/Validators/CreateMealItemRequestValidator.cs:14` | VALIDATION | `Quantity >= 0` autorise 0 — devrait être `> 0` |
| 13 | `Client/Pages/DayPlans/Index.razor:1055` | DRY | `ComputeItemCost` locale identique à `MealCell.razor:111` — consolider dans `CostHelper` |
| 14 | `Client/Components/AddItemDialog.razor:587-596` | ROBUSTESSE | `GetGradientStyle` : hash `h * 31 + c` peut déborder et `Math.Abs(int.MinValue)` lève `OverflowException` — entourer d'un bloc `unchecked` |
| 15 | `Client/Pages/MenuPlans/Index.razor:195` | ROBUSTESSE | `JS.InvokeVoidAsync("removeEscapeHandler")` dans `Dispose()` sans `_ =` ni try/catch — fire-and-forget non capturé |
| 16 | `Client/Pages/DayPlans/Index.razor:646-650` | BUG | `_allItems` chargé une seule fois via `!_allItems.Any()` — si le serveur retourne une liste vide au premier appel, le chargement n'est jamais réessayé |
| 17 | `Server/Services/DailyMenuService.cs:206` | BUG | `ComputeMonthlyCost` caste `Math.Ceiling` en `int` pour les plain items — incohérent avec `RecipeService.ComputeRecipeCost` qui garde `decimal` |
| 18 | `Client/Services/ItemSupplierCache.cs` | CONCURRENCE | `EnsureLoadedAsync` non thread-safe — deux composants appelant simultanément peuvent déclencher deux requêtes HTTP au premier rendu |
| 19 | `Client/Pages/Recipes/Index.razor:7` | DEAD CODE | `@using System.Globalization` inutilisé |
| 20 | `Tests/UnitTest1.cs` | DEAD CODE | Fichier de test vide (classe générée par défaut, zéro test) |
| 21 | `Client/Pages/ItemSuppliers/Create.razor` (+ `Edit.razor`, `Customers/Edit.razor`, `Items/Edit.razor`, `Suppliers/Edit.razor`) | DEAD CODE | Pages Edit/Create orphelines — supprimer si non référencées dans NavMenu |
| 22 | `Client/Components/ShoppingCart.razor:346-352` (+ MealRecap.razor, AddItemDialog.razor) | DRY | `FmtMoney` / `FmtQty` redéfinis dans 3 composants — déplacer dans `CostHelper` |
| 23 | `Client/Pages/DayPlans/Index.razor` (+ MealCell.razor) | DRY | HTML inline TR/CB badge répété 4+ fois — extraire en composant `TrCbBadges` ou helper Razor |
| 24 | `Server/Controllers/DailyMenusController.cs:38` | ROBUSTESSE | `POST /dailymenus` retourne 404 si le customer n'existe pas — préférer 422 UnprocessableEntity pour distinguer FK invalide vs ressource absente |
| 25 | `Client/Pages/MenuPlans/Index.razor:331` | MAINTENABILITÉ | Délai 280ms (debounce click/dblclick) est un magic number — nommer en constante |
| 26 | `Server/Services/DailyMenuService.cs:30` | MAINTENABILITÉ | `GetAllAsync` sans filtre charge tous les DailyMenus avec deep includes — endpoint non utilisé par le client; à supprimer ou restreindre |

---

## Bugs connus — statut

| KI | Statut | Note |
|----|--------|------|
| KI-2 | CORRIGÉ | `ShoppingCart.razor` inclut bien une section "Ingrédients recettes" distincte avec TR/CB, footer agrège les deux sections. |
| KI-4 | CORRIGÉ | `DuplicateMonthAsync` construit le graphe complet via nav-props et appelle `SaveChangesAsync` une seule fois après `AddRange`. |

---

## Duplications résiduelles confirmées

| Duplication | Fichiers |
|-------------|---------|
| `ComputeItemCost` locale (même logique) | `DayPlans/Index.razor:1055`, `MealCell.razor:111` |
| `FmtMoney` / `FmtQty` | `ShoppingCart.razor`, `MealRecap.razor`, `AddItemDialog.razor` |
| HTML inline TR/CB badge | `DayPlans/Index.razor` (×3 positions), `MealCell.razor` footer |
| Deep ThenInclude chain | `DailyMenuService.QueryWithIncludes`, `GetMonthlySummaryAsync`, `MealService.RandomFillAsync` — identiques, non partagés |
