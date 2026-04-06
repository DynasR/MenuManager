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
| MenuPlan/Index | Route `/menuplan/{CustomerId}`. Cards 3 ans. HasData coloring. **Dark mode** via `ThemeState` (inline styles dynamiques). |
| DayPlan/Index | Calendrier mensuel. **`AddItemDialog` diff-based** : colonnes triables (fournisseur, prix, qté), stepper toujours rendu (`visibility:hidden` quand 0). Random fill scindé : Casino (items) + MenuBook (recettes). Unique constraint `DailyMenu(CustomerId,Date)`. |
| MealCell | `ShouldRender()` override. Badges TR/CB : items via cache direct, recettes via `GetRecipePaymentTypes`. CSS subgrid. **TR/CB mini-badges dans le footer du slot** (total centré, breakdown à droite). |
| Recipe       | `/recipes` — MudDataGrid + HierarchyColumn, RecipeDialog, coût estimé par recette |
| Layout       | 3 thèmes (Light/Dark/Custom). `ThemeState` + CycleTheme. Persisté localStorage. |
| Shopping Cart | Refactorisé. `CartLine` unifié. CSS grid 4 col. Footer TR\|CB + total. Coût = ceil par slot. **Colonne qty = nb de colis** (`PackageCount`, computed). |

---

## Décisions d'architecture clés

Voir `CLAUDE.md` pour les détails. Résumé :
- **Shared** pur (zéro EF), pas de repository, on-demand DailyMenu/Meal.
- **Shopping Cart** (panneau droit global, `RightPanelState`).
- **Item : refacto unités** — `Unit+PackageSize` → `PurchaseUnit + ContentQuantity + ContentUnit`. Deux migrations : `RefactorItemUnits`, `AddUnitAndOrderToRecipeIngredient`.
- **MonthlyCost** calculé serveur-side (`ceil(qty / ContentQuantity) * UnitPrice`), affiché sur les cards MenuPlan et par item/cellule dans MealCell.
- **Recettes dans les slots** — `MealItem` peut référencer un `Item` ou une `Recipe` (champs `ItemId?` / `RecipeId?`). Coût recette = `RecipeEstimatedCost * Quantity`. Shopping Cart distingue les deux sections.
- **PaymentType** — enum `TR | CB` ajouté sur `Supplier` et `Customer`. Deux migrations séparées. Seed : Carrefour=TR, Leclerc=CB, Dynas=TR, Marlène=CB.
- **Shopping Cart enrichi** — `ItemSupplierCache` (service scoped) chargé une fois sur `OnInitializedAsync` de DayPlan (`GET /api/itemsuppliers/best-by-item`). Meilleur fournisseur par item en mémoire → partagé avec `MealCell` (badge TR/CB). Shopping Cart groupe par fournisseur retenu, **footer épinglé TR/CB/Total**. Endpoint `POST /api/itemsuppliers/by-items` conservé mais n'est plus utilisé par le panier.
- **CHECK constraints PaymentType** — migration `AddPaymentTypeCheckConstraints` : `PaymentType IN (0, 1)` sur Suppliers et Customers.
- **Copy/Move cellule** — cellule entière draggable (zones latérales footer). Ctrl tenu = copie. Plus de trash-zone : clear via dbl-clic sur le total.
- **Bulk clear** — `DELETE /api/meals/batch` (body JSON, toujours 204). Vider-ligne, vider-colonne, vider-mois utilisent tous ce même endpoint. Pattern confirm-intent : prime (mousedown rouge) + dbl-clic exécute.
- **Random fill** — `POST /api/meals/random-fill`. Pool = items disponibles + **toutes les recettes**. Skip les jours qui ont déjà des repas.
- **Drag & drop immédiat** — plus de "Save All" / `_pendingMoves`. Tout appel API se fait au moment du drop. Overlay sombre pendant l'async.
- **SortableJS copy** — Ctrl au lâcher d'un item cross-cell = copie (ghost visible à la source pendant le drag).
- **Thème** — 3 thèmes (Light/Dark/Custom), `ThemeState` scoped service, persisté localStorage. `MenuPlan/Index` dark mode : inline styles dynamiques via `ThemeState.OnChange`.
- **CostHelper** — `Client/Helpers/CostHelper.cs` : `PackageCost` + `ComputeItemCost` — formules canoniques partagées par `MealCell`, `DayPlan/Index`, `ShoppingCart`. Fin de la duplication.
- **AddItemDialog** — le tiroir inline de DayPlan/Index a été remplacé par `AddItemDialog.razor` (MudDialog via `IDialogService`). Deux modes d'affichage : cards (gradients, infos fournisseur) et dense (MudDataGrid sortable). Préférence mémorisée en localStorage (`add-dialog-dense`). L'ouverture du dialog ne ferme plus le panneau droit (shopping cart reste visible).
- **MealTypeHelper** — `Client/Helpers/MealTypeHelper.cs` : extension `ToFrenchLabel()` sur `MealType`. Utilisé dans les en-têtes colonnes DayPlan et le sous-titre de `AddItemDialog`.
- **Fix UnitPrice mapping** — les services (`MealItemService`, `DailyMenuService`, `MealService`) utilisaient `OrderBy(s => s.SupplierId)` pour le prix unitaire affiché → corrigé en `OrderBy(s => s.UnitPrice)` (vraiment le moins cher).
- **MealItemResponse.RecipeIngredientItemIds** — liste des `ItemId` des ingrédients d'une recette, propagée depuis le serveur. Permet aux badges TR/CB de la `MealCell` et au footer TR/CB du `ShoppingCart` de coûter les recettes par ingrédient.
- **TR/CB breakdowns** — visibles partout : (1) slot footer de `MealCell` (total centré, TR|CB à droite), (2) cellule-date gauche dans DayPlan (par jour), (3) en-têtes colonnes MealType (par repas). Recette à ingrédients mixtes → coût 50/50. Helpers : `BucketRecipeCost`, `GetRowTrCb`, `GetColumnTrCb` dans DayPlan/Index ; `SlotTrCb` dans MealCell.
- **Random fill scindé** — `RandomFillMode` enum (`Items` | `Recipes`) dans `Shared/DTOs/MealDtos.cs`. Deux boutons : Casino (items disponibles) et MenuBook (toutes les recettes). Pool construit **une fois** avant la boucle de jours.
- **Unique constraint DailyMenu** — migration `AddUniqueDailyMenuDateConstraint` : index unique sur `(CustomerId, Date)`. `ToDictionary` corrigé côté client avec `GroupBy().First()` pour robustesse.
- **ShoppingCart refactorisé** — `Recipes` param supprimé. `CartLine` unifié. Pas de fallback. CSS grid 4 colonnes. Footer 4-col. Coût = ceil/slot. **Colonne qty affiche `PackageCount`** (nb de colis = `ceil(TotalQty / ContentQuantity)`, propriété calculée sur `CartLine`).
- **MealTypeFlags** — flags enum (`Shared/Enums/MealTypeFlags.cs`) : `None=0, Breakfast=1, Snack=2, Lunch=4, Dinner=8`. `Category.AllowedMealTypes` (EF, migration `AddMealTypesToCategory`). `CategoryResponse.AllowedMealTypes (int)` + `ItemResponse.CategoryAllowedMealTypes (int)`. SeedData : flags par catégorie (ex. Biscuits = Breakfast|Snack, Viandes = Lunch|Dinner).
- **AddItemDialog diff-based** — Dialog reçoit `CurrentMealItems`, calcule un diff (add/update/delete), envoie à `ApplyDialogSaveAsync`. Colonnes denses triables (`SortBy` sur fournisseur, prix unitaire, qté via lambda cache). Stepper toujours rendu — `visibility:hidden` quand qty=0 (pas de layout shift). `LoadSlot()` extrait de `OnInitializedAsync` pour clarté. Panel `MealRecap` sidebar 460px : grille 5 col (`× | Nom | €/U | U | Total`), bouton `×` remove (envoie qty=0), header affiche MealType + date, footer TR/CB/Total comme DayPlan. Toggle `_showAll` filtre par `CategoryAllowedMealTypes`.
- **Tests** — SQLite in-memory uniquement.

---

## Prochains chantiers (backlog CW)

### MenuPlan/Index — duplication de planning (analyse de faisabilité demandée)
UX cible (style ModX) :
- Dbl-clic sur une carte = sélection source (highlight identique aux MealCards de DayPlan)
- Clic sur une autre carte = sélection secondaire (légère)
- Clic sur une carte cible = copie jour par jour — warning si mois source et mois cible n'ont pas le même nombre de jours correspondants
- Si la cible a déjà des données : confirmation de remplacement
- Clic hors cartes = quitter le mode duplication

### MenuPlan/Index
- Option voir les mois passés
- Cards : clic sur toute la carte pour voir le planning (pas seulement le bouton)
- Améliorer style des détails : coût par MealType, coût moyen, coût médian, style cohérent avec les totaux DayPlan

### DayPlan/Index
- Total par semaine
- Dialogue de confirmation pour les actions destructives (vider ligne/colonne/mois)

### Gestion Catégories
- Tree view

---

## Règles du CW

1. **Intention + contraintes** — jamais de pseudo-code.
2. **Trancher avant de briefer** — l'archi se résout avec Lead Dev avant que CC code.
3. **Options avec trade-offs** quand c'est ambigu — c'est lui qui décide.
4. **Ne pas sur-détailler** — CC a CLAUDE.md.
5. **Former, pas juste livrer** — expliquer le pourquoi.
