# CW Context — MenuManager

> Contexte concis pour CW (advisor). CC utilise `CLAUDE.md` pour les détails d'implémentation.
> Ne pas dupliquer ce qui est dans CLAUDE.md — ici : rôles, état projet, décisions haut niveau.

---

## Rôles

- **Lead Dev** : décide de tout. Dev C++, débutant .NET (objectif AZ-204). Veut comprendre chaque choix.
- **CW** : conseille, forme. Intention + contraintes uniquement — jamais de code dans les briefs.
- **CC** : exécute. Lit `CLAUDE.md` au démarrage de chaque session.

---

## Stack

Blazor WASM (PWA) + MudBlazor · ASP.NET Core Web API (.NET 9) · EF Core 9 + PostgreSQL 16 (Docker)

---

## État du projet (2026-04-06)

### Backend — complet
9 slices : Category, Item, Supplier, Customer, ItemSupplier, DailyMenu, Meal, MealItem, Recipe.
Chaque slice : DTO / Validator / Service / Controller / Tests (SQLite in-memory).

### Frontend — complet

| Page         | Points clés                                                                      |
|--------------|----------------------------------------------------------------------------------|
| Category     | Référence pattern Index (pending rows + dirty + Save All)                        |
| Item         | FK Category, PurchaseUnit + ContentQuantity + ContentUnit (refactorisé)          |
| Supplier     | Party + CompanyName, Siret, **PaymentType** (TR/CB)                              |
| Customer     | Party + **PaymentType** (TR/CB) — CalendarMonth → `/menuplan/{id}`               |
| ItemSupplier | PK composite, pattern 404/409 ; dropdown `CompanyName ?? Name` ; colonnes FK non-éditables |
| MenuPlan/Index | Route `/menuplan/{CustomerId}`. Cards 3 ans + 12 mois passés (toggle History). HasData coloring. **Dark mode** via `ThemeState`. **Coût moyen journalier** sur les cards. **Mode duplication** : dbl-clic → copy mode, clic cible → copie jour par jour (confirmation si données existantes), Echap/clic extérieur pour quitter. |
| DayPlan/Index | Calendrier mensuel. **`AddItemDialog` diff-based** : colonnes triables (fournisseur, prix, qté), stepper toujours rendu (`visibility:hidden` quand 0). Random fill scindé : Casino (items) + MenuBook (recettes). Unique constraint `DailyMenu(CustomerId,Date)`. |
| MealCell | `ShouldRender()` override. Badges TR/CB via `CostHelper.GetRecipePaymentTypes` + `BucketByCost`. CSS subgrid. **TR/CB mini-badges dans le footer du slot** (total centré, breakdown à droite). |
| Recipe       | `/recipes` — MudDataGrid + HierarchyColumn, RecipeDialog, coût estimé par recette |
| Layout       | 3 thèmes (Light/Dark/Custom). `ThemeState` + CycleTheme. Persisté localStorage. |
| Shopping Cart | `CartLine` unifié. CSS grid 4 col. Footer TR\|CB + total. Coût = ceil par slot. **Colonne qty = nb de colis** (`PackageCount`, computed). **⚠️ Recettes exclues du panier** (voir Bugs connus). |

---

## Décisions d'architecture clés

Voir `CLAUDE.md` pour les détails. Résumé :
- **Shared** pur (zéro EF), pas de repository, on-demand DailyMenu/Meal.
- **Shopping Cart** (panneau droit global, `RightPanelState`). Recettes **non incluses** — voir Bugs connus.
- **Item : refacto unités** — `Unit+PackageSize` → `PurchaseUnit + ContentQuantity + ContentUnit`. Deux migrations : `RefactorItemUnits`, `AddUnitAndOrderToRecipeIngredient`.
- **MonthlyCost** calculé serveur-side dans `DailyMenuService.ComputeMonthlyCost`. Utilise `OrderBy(s => s.UnitPrice)` — prix correct (fix KI-1 appliqué).
- **Recettes dans les slots** — `MealItem` peut référencer un `Item` ou une `Recipe` (champs `ItemId?` / `RecipeId?`). Coût recette côté client = `RecipeEstimatedCost * Quantity`. Shopping Cart exclut toujours les recettes (`.Where(i => !i.RecipeId.HasValue)`) — voir Bugs connus KI-2.
- **PaymentType** — enum `TR | CB` ajouté sur `Supplier` et `Customer`. Deux migrations séparées. Seed : Carrefour=TR, Leclerc=CB, Dynas=TR, Marlène=CB.
- **Shopping Cart enrichi** — `ItemSupplierCache` (service scoped) chargé une fois sur `EnsureLoadedAsync()` (`GET /api/itemsuppliers/best-by-item`). Meilleur fournisseur par item en mémoire → partagé avec `MealCell` (badge TR/CB). Shopping Cart groupe par fournisseur retenu, **footer épinglé TR/CB/Total**. Endpoint `POST /api/itemsuppliers/by-items` supprimé (KI-6 résolu).
- **CHECK constraints PaymentType** — migration `AddPaymentTypeCheckConstraints` : `PaymentType IN (0, 1)` sur Suppliers et Customers.
- **Copy/Move cellule** — cellule entière draggable (zones latérales footer). Ctrl tenu = copie. Plus de trash-zone : clear via dbl-clic sur le total.
- **Bulk clear** — `DELETE /api/meals/batch` (body JSON, toujours 204). Vider-ligne, vider-colonne, vider-mois utilisent tous ce même endpoint. Pattern confirm-intent : prime (mousedown rouge) + dbl-clic exécute.
- **Random fill** — `POST /api/meals/random-fill`. Pool = items disponibles (mode Items) OU toutes les recettes (mode Recipes). Skip les jours qui ont déjà des repas.
- **Drag & drop immédiat** — plus de "Save All" / `_pendingMoves`. Tout appel API se fait au moment du drop. Overlay sombre pendant l'async.
- **SortableJS copy** — Ctrl au lâcher d'un item cross-cell = copie (ghost visible à la source pendant le drag).
- **Thème** — 3 thèmes (Light/Dark/Custom), `ThemeState` scoped service, persisté localStorage. `MenuPlan/Index` dark mode : inline styles dynamiques via `ThemeState.OnChange`.
- **CostHelper** — `Client/Helpers/CostHelper.cs` : étendu avec `Fr` (CultureInfo partagée), `ComputePricePerKgL` (centralisé, KI-5 résolu), `GetRecipePaymentTypes`, `BucketByCost`. Plus de `_fr` local dans les composants. `@using MenuManager.Client.Helpers` global dans `_Imports.razor`.
- **AddItemDialog** — `MudDialog` via `IDialogService`. Deux modes : cards et dense (MudDataGrid sortable). Préférence mémorisée en localStorage (`add-dialog-dense`). L'ouverture du dialog ne ferme plus le panneau droit (shopping cart reste visible).
- **MealTypeHelper** — `Client/Helpers/MealTypeHelper.cs` : extension `ToFrenchLabel()` sur `MealType`.
- **Fix UnitPrice mapping** — tous les mappings (MealItemMapper, RecipeService, DailyMenuService) utilisent `OrderBy(s => s.UnitPrice)` (le moins cher). KI-1 résolu.
- **MealItemMapper** (`Server/Mapping/MealItemMapper.cs`) — mapping `MealItem → MealItemResponse` centralisé, utilisé par `MealItemService`, `MealService`, `DailyMenuService` (élimine la triplication).
- **MealItemResponse.RecipeIngredientItemIds** — liste des `ItemId` des ingrédients d'une recette, propagée depuis le serveur. Utilisée par les badges TR/CB de `MealCell` et les calculs TR/CB de DayPlan/MealRecap.
- **TR/CB breakdowns** — visibles dans : slot footer de `MealCell`, cellule-date gauche (par jour), en-têtes colonnes MealType (par repas). Logique centralisée : `CostHelper.BucketByCost` + `CostHelper.GetRecipePaymentTypes` (plus de duplication dans DayPlan/MealCell/MealRecap).
- **Random fill scindé** — `RandomFillMode` enum (`Items` | `Recipes`). Deux boutons : Casino (items) et MenuBook (recettes).
- **Unique constraint DailyMenu** — migration `AddUniqueDailyMenuDateConstraint` : index unique sur `(CustomerId, Date)`.
- **MealTypeFlags** — flags enum : `None=0, Breakfast=1, Snack=2, Lunch=4, Dinner=8`. `Category.AllowedMealTypes`. SeedData : flags par catégorie.
- **AddItemDialog diff-based** — Dialog reçoit `CurrentMealItems`, calcule un diff (add/update/delete). `UpdateMealItemRequest` accepte `ItemId?` + `RecipeId?` — les recettes sont mises à jour via PUT (KI-3 résolu). `_filterByMealType` bool (était `_showAll` inversé).
- **Tests** — SQLite in-memory uniquement.
- **Duplication de mois** — `POST /api/dailymenus/duplicate` : supprime d'abord le mois cible (cascade), recopie jour par jour depuis la source. Copie chaînable. ⚠️ N×M appels `SaveChangesAsync` — voir Bugs connus KI-4.

---

## Bugs connus (backlog technique prioritaire)

| # | Priorité | Description | Fix attendu |
|---|----------|-------------|-------------|
| KI-2 | ★★★ | Shopping Cart exclut les recettes (`.Where(i => !i.RecipeId.HasValue)`). Totaux TR/CB/total incomplets. Message "No items" possible même avec des recettes dans le plan. | Étendre `ComputeLines()` pour expanser les recettes via `RecipeIngredientItemIds` + cache. |
| KI-4 | ★ | `DuplicateMonthAsync` : `SaveChangesAsync` dans les boucles imbriquées (~N×M saves). | Grouper les insertions, sauvegarder en 2 passes (DailyMenus, puis Meals+MealItems). |

---

## Duplications résiduelles

Pour information du CW lors des briefs :

- **Chaîne `ThenInclude`** (Item→ItemSuppliers + Recipe→RecipeIngredients→Item→ItemSuppliers) copiée dans `DailyMenuService.QueryWithIncludes`, `MealItemService`, `MealService.QueryWithIncludes`.

---

## Règles du CW

1. **Intention + contraintes** — jamais de pseudo-code.
2. **Trancher avant de briefer** — l'archi se résout avec Lead Dev avant que CC code.
3. **Options avec trade-offs** quand c'est ambigu — c'est lui qui décide.
4. **Ne pas sur-détailler** — CC a CLAUDE.md.
5. **Former, pas juste livrer** — expliquer le pourquoi.
6. **Vérifier les Bugs connus** avant tout brief sur un composant affecté — ne pas demander à CC de coder par-dessus un bug connu sans l'adresser.
