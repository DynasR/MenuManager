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

## État du projet (2026-04-05)

### Backend — complet
8 slices : Category, Item, Supplier, Customer, ItemSupplier, DailyMenu, Meal, MealItem.
Chaque slice : DTO / Validator / Service / Controller / Tests (SQLite in-memory).

### Frontend — complet

| Page         | Points clés                                                                      |
|--------------|----------------------------------------------------------------------------------|
| Category     | Référence pattern Index (pending rows + dirty + Save All)                        |
| Item         | FK Category, PurchaseUnit + ContentQuantity + ContentUnit (refactorisé)          |
| Supplier     | Party + CompanyName, Siret, **PaymentType** (TR/CB)                              |
| Customer     | Party + **PaymentType** (TR/CB) — CalendarMonth → `/menuplan/{id}`               |
| ItemSupplier | PK composite, pattern 404/409                                                    |
| MenuPlan/Index | Route `/menuplan/{CustomerId}`. Cards sur 3 ans. Données via `GET /api/dailymenus/{customerId}/monthly-summary` (HasMeals, MonthlyCost). Bouton "Voir le planning" → navigation directe `dayplans?customerId=X&year=Y&month=M`, pas de création serveur. |
| DayPlan/Index | Calendrier mensuel. Tiroir d'ajout à **2 onglets (Items / Recettes)**. Recettes ajoutables dans les slots (`AddRecipeToSlotAsync`). Calcul coût via `ComputeItemCost` (gère items et recettes). Copy/clone/cell-drag recipe-aware. Dark mode chips inline via `ThemeState`. CSS `.meal-cell-item-recipe` (teinte violet). |
| MealCell | **`ShouldRender()` override** — compare items (id/qty/order), MealId, IsBeingDragged, IsActionTarget, _clearPrimed ; snapshot dans `OnAfterRenderAsync`. JS drag fire-and-forget. |
| Recipe       | ✅ `/recipes` — MudDataGrid + HierarchyColumn (ingrédients inline), RecipeDialog (create/edit), coût estimé par recette |
| Layout       | **3 thèmes** : Light (palette chaude), Dark (navy), Custom (noir pur). `ThemeState` + bouton CycleTheme dans AppBar. Persisté localStorage. AppBar dégradé marine fixe. |
| Shopping Cart | **Enrichissement fournisseur** : s'ouvre → appel `POST /api/itemsuppliers/by-items` → items groupés par meilleur fournisseur (TR bleu / CB violet). Affiche best/worst/avg totals. Fallback si données non chargées. |

---

## Décisions d'architecture clés

Voir `CLAUDE.md` pour les détails. Résumé :
- **Shared** pur (zéro EF), pas de repository, on-demand DailyMenu/Meal.
- **Shopping Cart** (panneau droit global, `RightPanelState`).
- **Item : refacto unités** — `Unit+PackageSize` → `PurchaseUnit + ContentQuantity + ContentUnit`. Deux migrations : `RefactorItemUnits`, `AddUnitAndOrderToRecipeIngredient`.
- **MonthlyCost** calculé serveur-side (`ceil(qty / ContentQuantity) * UnitPrice`), affiché sur les cards MenuPlan et par item/cellule dans MealCell.
- **Recettes dans les slots** — `MealItem` peut référencer un `Item` ou une `Recipe` (champs `ItemId?` / `RecipeId?`). Coût recette = `RecipeEstimatedCost * Quantity`. Shopping Cart distingue les deux sections.
- **PaymentType** — enum `TR | CB` ajouté sur `Supplier` et `Customer`. Deux migrations séparées. Seed : Carrefour=TR, Leclerc=CB, Dynas=TR, Marlène=CB.
- **Shopping Cart enrichi** — chargement lazy au premier affichage du panneau. Par item : tous les fournisseurs disponibles → best/worst/avg. Items groupés par fournisseur retenu (= le moins cher). Endpoint dédié : `POST /api/itemsuppliers/by-items`.
- **Copy/Move cellule** — cellule entière draggable (zones latérales footer). Ctrl tenu = copie. Plus de trash-zone : clear via dbl-clic sur le total.
- **Bulk clear** — `DELETE /api/meals/batch` (body JSON, toujours 204). Vider-ligne, vider-colonne, vider-mois utilisent tous ce même endpoint. Pattern confirm-intent : prime (mousedown rouge) + dbl-clic exécute.
- **Random fill** — `POST /api/meals/random-fill`. Remplit les jours vides du mois avec des items disponibles aléatoires. Skip les jours qui ont déjà des repas.
- **Drag & drop immédiat** — plus de "Save All" / `_pendingMoves`. Tout appel API se fait au moment du drop. Overlay sombre pendant l'async.
- **SortableJS copy** — Ctrl au lâcher d'un item cross-cell = copie (ghost visible à la source pendant le drag).
- **Thème** — 3 thèmes (Light/Dark/Custom), `ThemeState` scoped service, persisté localStorage.
- **Tests** — SQLite in-memory uniquement.

---

## Prochains chantiers (backlog CW)

### MenuPlan/Index
- Duplication d'un planning vers un autre mois
- Option voir les mois passés

### DayPlan/Index
- Total par semaine
- Dialogue de confirmation pour les actions destructives (vider ligne/colonne/mois)

### Détails financiers
- Coût moyen et médian par jour/semaine/mois

### Style global
- Clic sur toute la carte MenuPlan → voir le planning (pas seulement le bouton)
- Revue globale du style

### Recettes
- Calcul coût par portion (BaseServings)
- Affichage des ingrédients dans la MealCell (tooltip ou expand)

---

## Règles du CW

1. **Intention + contraintes** — jamais de pseudo-code.
2. **Trancher avant de briefer** — l'archi se résout avec Lead Dev avant que CC code.
3. **Options avec trade-offs** quand c'est ambigu — c'est lui qui décide.
4. **Ne pas sur-détailler** — CC a CLAUDE.md.
5. **Former, pas juste livrer** — expliquer le pourquoi.
