# CLAUDE.md — MenuManager context for Claude Code

> This file is the **single source of truth** for CC.
> Update it after each significant architectural decision.
> CW (advisor) and Lead Dev maintain it; CC reads it at the start of every session.

---

## Project

**MenuManager** — meal planning web app.
Stack: Blazor WASM (PWA) + ASP.NET Core Web API (.NET 9) + EF Core 9 + PostgreSQL 16 (Docker).

```
/MenuManager
  /Client   → Blazor WASM + MudBlazor
  /Server   → ASP.NET Core Web API
  /Shared   → Entities, DTOs, Validators, Enums (pure class library — NO EF dependency)
  /Tests    → xUnit + bUnit + FluentAssertions + Moq
  docker-compose.yml
```

---

## Network (dev)

| Service    | URL / Port            |
|------------|-----------------------|
| API Server | http://localhost:5075 |
| PostgreSQL | localhost:5432        |
| DB name    | menumanager           |
| DB user    | admin                 |

Client reads `ServerUrl` from `Client/wwwroot/appsettings.json`.

---

## Domain model (Shared/Entities/)

```
Party (abstract, TPT)
  ├── Customer   — PasswordHash, PasswordSalt, PaymentType, ICollection<DailyMenu>
  └── Supplier   — CompanyName, Siret, PaymentType, ICollection<ItemSupplier>

Category         — Id, Name, Description, ParentCategoryId (self-ref), SubCategories, Items,
                   AllowedMealTypes(MealTypeFlags, default=Breakfast|Lunch|Dinner|Snack)
Item             — Id, Name, Description, PurchaseUnit(MeasurementUnit), ContentQuantity(decimal, default=1),
                   ContentUnit(MeasurementUnit), CategoryId(int), CreatedAt, UpdatedAt, ItemSuppliers, MealItems
ItemSupplier     — PK composite (ItemId+SupplierId), UnitPrice(10,2), SupplierRef, IsAvailable, UpdatedAt
DailyMenu        — Id, Date(DateOnly), CustomerId, ICollection<Meal>
Meal             — Id, MealType, DailyMenuId, unique(DailyMenuId+MealType), ICollection<MealItem>
MealItem         — Id, Quantity(10,3), Notes, Order(int, default=0), Unit(MeasurementUnit), MealId, ItemId?, RecipeId?
Recipe           — Id, Name, Description, BaseServings, ICollection<RecipeIngredient>
RecipeIngredient — PK composite (RecipeId+ItemId), Quantity, Unit(MeasurementUnit), Order(int)

MealType        (enum, Shared/Entities/) — Breakfast, MorningSnack, Lunch, AfternoonSnack, Dinner
MeasurementUnit (enum, Shared/Enums/) — Piece, Gram, Kilogram, Milliliter, Liter
PaymentType     (enum, Shared/Enums/) — TR, CB
MealTypeFlags   (flags enum, Shared/Enums/) — None=0, Breakfast=1, Snack=2, Lunch=4, Dinner=8
```

Hierarchy: **Customer → DailyMenu → Meal → MealItem** (MenuPlan removed).

All EF config (precision, indexes, TPT, composite PKs) lives **exclusively** in
`AppDbContext.OnModelCreating()` Fluent API.

### ContentQuantity business rule
- `MealItem.Quantity` = quantity consumed (e.g. 1 portion)
- `Item.ContentQuantity` = content per package (e.g. 6 for a 6-pack)
- `Item.PurchaseUnit` = unit of the package; `Item.ContentUnit` = unit of content
- Purchase calculation: `ceil(total_needed / ContentQuantity)`
- `RecipeIngredient.Unit` = unit used in the recipe (may differ from `PurchaseUnit`/`ContentUnit`).

---

## Migrations applied

| Migration name                             | Description                                                                    |
|--------------------------------------------|--------------------------------------------------------------------------------|
| InitialCreate                              | Full initial schema                                                            |
| RefactorItemUnitAndPackageSize             | Unit → enum MeasurementUnit, PackageSize added                                 |
| AddMealSlotItemOrder                       | Order int default 0 added on MealSlotItem                                      |
| RemoveMenuPlan_RenameDayPlan_MealSlot      | Drop MenuPlan; DailyMenu→Meal→MealItem, FK=CustomerId                          |
| RefactorItemUnits                          | Item: Unit+PackageSize → PurchaseUnit+ContentQuantity+ContentUnit              |
| AddUnitAndOrderToRecipeIngredient          | RecipeIngredient: add Unit(MeasurementUnit) + Order(int)                       |
| AddPaymentTypeToSupplier                   | Supplier: add PaymentType(int)                                                 |
| AddPaymentTypeToCustomer                   | Customer: add PaymentType(int)                                                 |
| AddPaymentTypeCheckConstraints             | CHECK constraints `PaymentType IN (0, 1)` on Suppliers and Customers           |
| AddUniqueDailyMenuDateConstraint           | Unique index on `DailyMenu(CustomerId, Date)`                                  |
| AddMealTypesToCategory                     | Category: add AllowedMealTypes(int, default=15). SeedData updated per category.|

---

## Architecture rules (non-negotiable)

- **Shared** = pure class library. Zero EF Core dependency.
- **Enums** in `Shared/Enums/` — single source of truth for Client and Server.
- **DTOs** in `Shared/DTOs/` — manual mapping only, no AutoMapper.
- **FluentValidation** in `Shared/Validators/`.
- **No repository pattern** — services use `AppDbContext` directly.
- **No business logic** in Blazor components — all HTTP calls go through a service.
- **Tests**: SQLite in-memory only. EF InMemory provider is **forbidden** (doesn't enforce constraints).
- **On-demand DB records**: DailyMenu and Meal records are created lazily when the first MealItem is added.

---

## Service method signatures

| Case                                 | Return type        |
|--------------------------------------|--------------------|
| Create, no FK to validate            | `Task<T>`          |
| Create, FK(s) to validate            | `Task<T?>`         |
| Create, ambiguous 404 vs 409         | `Task<ResultType>` |
| Update (entity may not exist)        | `Task<T?>`         |
| Delete                               | `Task<bool>`       |

### 404 vs 409 pattern
When `CreateAsync` can fail for multiple distinct reasons, use a result type (`enum` + `record`) in `Shared/DTOs/`.
Controller switches on `Error` → correct HTTP status. Frontend switches on `Error` → correct snackbar.
Reference: `ItemSupplierDtos.cs`.

---

## Index page pattern (established — reference: `Category/Index.razor`)

- **Pending rows**: draft grid above main grid, "Add row" appends `XxxDraft`, Validate → `CreateAsync`, Cancel to discard.
- **Main grid**: `MudDataGrid` cell edit, `ValueChanged` + `_dirtyRows` tracking, explicit Save per row.
- **Save All**: creates pending rows first (insertion order), then updates dirty rows. Snackbar summary.
- **No** separate Create/Edit pages — all inline. Draft class is private `sealed class` in `@code`.
- FK/Enum columns: `MudSelect` with `EditTemplate` + `ValueChanged`. Enums via `Enum.GetValues<T>()`.
- Composite PK (ItemSupplier): `_dirtyRows` is `HashSet<(int, int)>`.

---

## MudBlazor component rules

- `MudIconButton` does **not** accept `Title` or `Tooltip` attributes — compiler warning MUD0002.
- To show a tooltip on an icon button, wrap it: `<MudTooltip Text="..."><MudIconButton .../></MudTooltip>`.

---

## Grid alignment rule

Stacked grids (pending + main): `table-layout: fixed; width: 100%` on both, identical `width: X%` on columns.

---

## Completed slices

### Backend (all complete: DTO / Validator / Service / Controller / Tests)
Category, Item, Supplier, Customer, ItemSupplier, DailyMenu, Meal, MealItem, Recipe

**`GET /api/dailymenus/{customerId}/monthly-summary`** → `List<MonthlySummaryResponse>(Year, Month, HasMeals, MonthlyCost, DaysWithMeals)`
MonthlyCost computed in C# after EF load.
- Plain items: `ceil(qty / ContentQuantity) * cheapest available UnitPrice` (`OrderBy(s => s.UnitPrice)`)
- Recipes: `RecipeService.ComputeRecipeCost(recipe) * quantity`
- `DaysWithMeals` = count of DailyMenu rows with at least one MealItem.

**RecipeService**: All queries include `ItemSuppliers` via deep `ThenInclude`. `ComputeRecipeCost(Recipe r)` is public static (reused by `MealItemService` and `DailyMenuService`). Recipe response includes `EstimatedCost`. Ingredients ordered by `Order`.

**MealItemService**: `CreateAsync` accepts `ItemId?` or `RecipeId?` (exactly one must be set). Response maps `RecipeName`, `RecipeEstimatedCost`, `RecipeIngredientItemIds`, `Unit`, `ContentQuantity`, `PurchaseUnit`, `ContentUnit`. `UnitPrice` mapped with `OrderBy(s => s.UnitPrice)` (cheapest). `UpdateAsync` accepts `ItemId?` + `RecipeId?` in `UpdateMealItemRequest` — validates whichever is set (fixed KI-3).

**`Server/Mapping/MealItemMapper.cs`** — single `ToResponse(MealItem) → MealItemResponse` method used by `MealItemService`, `MealService`, and `DailyMenuService`. Was previously duplicated in all three.

**CategoryResponse** includes `AllowedMealTypes (int)`. **ItemResponse** includes `CategoryAllowedMealTypes (int)`.

**`GET /api/itemsuppliers/best-by-item`** → `Dictionary<int, BestSupplierInfo>`. Cheapest available supplier per item (globally). `BestSupplierInfo`: `ItemId`, `PaymentType`, `UnitPrice`, `SupplierName`. Used by `ItemSupplierCache`.

**`POST /api/dailymenus/duplicate`** — body: `DuplicateMonthRequest { CustomerId, SourceYear, SourceMonth, TargetYear, TargetMonth }` → 204 or 404. Deletes target month (cascade), recreates day-by-day from source. Days beyond `targetDaysInMonth` silently skipped. Validator: `DuplicateMonthRequestValidator`. Client: `DailyMenuService.DuplicateMonthAsync → Task<bool>`. ⚠️ KI-4: `SaveChangesAsync` inside nested loops.

### Frontend (Client)

| Slice        | Service | Index / Page    | Notes                                                              |
|--------------|---------|-----------------|--------------------------------------------------------------------|
| Layout       | —       | —               | ThemeState (Light/Dark/Custom), AppBar dark navy gradient, NavMenu split, RightPanelState, CycleTheme (persisted localStorage) |
| Category     | ✅      | ✅              | Reference implementation — new Save pattern applied                |
| Item         | ✅      | ✅              | FK CategoryId, PurchaseUnit + ContentQuantity + ContentUnit        |
| Supplier     | ✅      | ✅              | Party fields + CompanyName, Siret, PaymentType                     |
| Customer     | ✅      | ✅              | Party fields + PaymentType — CalendarMonth → `/menuplan/{id}`      |
| ItemSupplier | ✅      | ✅              | Double FK dropdown, composite PK, snackbar 404/409; `CompanyName ?? Name`; FK columns non-editable |
| DailyMenu    | ✅      | ✅ via MenuPlan/Index | Route `/menuplan/{CustomerId}` — cards from `monthly-summary` |
| Meal         | ✅      | ✅ via DayPlan/Index  | FK DailyMenuId                                                |
| MealItem     | ✅      | ✅ via DayPlan/Index  | FK MealId, Order, UnitPrice; ItemId? or RecipeId?; add/edit via `AddItemDialog` |
| Recipe       | ✅      | ✅ `/recipes`   | MudDataGrid + HierarchyColumn + RecipeDialog                       |

---

## Recipes/Index

Route: `/recipes`. `MudDataGrid` read-only with `HierarchyColumn` — child row shows ingredient list inline.
Ingredient editing: pending draft row + dirty tracking (Category/Index pattern).
Per-ingredient fields: ItemId (dropdown), Quantity, Unit (enum select), Order. Ordered by `Order`.
Create/edit via `RecipeDialog.razor`. Fields: Name, Description, BaseServings.
Estimated cost: `ceil(Qty / ContentQuantity) * UnitPrice` per ingredient (cheapest available supplier).
Client service: `RecipeService` — `GetAllAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `AddIngredientAsync`, `UpdateIngredientAsync`, `DeleteIngredientAsync`.

---

## MenuPlan/Index — 3-year card grid

Route: `/menuplan/{CustomerId:int}`.

- Default: 3 years of cards grouped by year — current month → Dec, then N+1 and N+2.
- **Show past months toggle**: 12 preceding months. `_showPast` bool, `TogglePast()` re-runs `BuildSlotsByYear`.
- **Card click** = navigate to `dayplans?customerId=X&year=Y&month=M` — 280ms delay for dbl-click cancel.
- **HasData coloring** (inline styles): current month = green border; has data = blue border; empty = dim.
- **Average daily cost**: `Moy. X.XX €/j` in CardActions when `DaysWithMeals > 0`.
- **MonthlyCost**: server-side via `ComputeMonthlyCost`. Uses `OrderBy(s => s.UnitPrice)` — consistent with DayPlan cell costs (`ItemSupplierCache` = cheapest).
- Data source: `GET /api/dailymenus/{customerId}/monthly-summary`. `_summaries` cached across `TogglePast`.

### Copy mode (duplicate month)

- **Enter**: dbl-click card with `HasMeals` → source card gets amber border + overlay. Full-screen backdrop catches outside-clicks → `ExitCopyModeAsync`.
- **JS Escape**: `addEscapeHandler(dotNetRef)` / `removeEscapeHandler()` in `sortable-interop.js` → `[JSInvokable] ExitCopyModeJs()`.
- **Click source**: ignored. **Click target**: `HandleTargetSelectedAsync`. If target HasMeals → `DuplicateMonthDialog` (warns overwrite). On confirm → `POST /api/dailymenus/duplicate` → reload + stay in copy mode (chainable).
- **Dbl-click another card with data**: re-selects as source.
- **Delete source button**: `ClearSourceMonthAsync()` via `MudMessageBox`, then `MealSvc.DeleteBatchAsync`. Exits copy mode.
- **Hover**: `_hoverTarget` tracks hovered card. Target cards pulsate (CSS `copy-target-pulse`) when not hovered; scale+shadow on hover. `GetCardCssClass(slot)` / `GetCardStyle(slot)` handle all states.
- `DuplicateMonthDialog.razor`: `SourceTitle`, `Options (List<TargetOption>)`, `PreSelected`. `TargetOption` record `(Year, Month, Title, HasData)`. Shows overwrite warning when selected HasData.

---

## DayPlan/Index — monthly calendar view

### Layout
- CSS grid: 7 columns — Date (80px) + 5 MealTypes + Date-right (80px). `user-select: none`.
- Cell wrapper `<div>` carries `@key="@((capturedDate, capturedMt))"` for stable Blazor diffing.
- One row per day. FR locale. Weekend/holiday coloring via `FrenchHolidays.cs`.
- **Row-primed highlight**: mousedown on date cell → `addRowPrimed(date)` JS — absolutely-positioned `.primed-axis-overlay` over all `[data-rowdate="yyyy-MM-dd"]` elements (red tint). dbl-click → `ClearRowAsync(date)`.
- **Column-primed highlight**: mousedown on MealType header → `addColumnPrimed(mealTypeInt)` over `[data-colmealtype="X"]` elements. dbl-click → `ClearColumnAsync(mealType)`. Only one `_primedOverlay` at a time.
- **Row/column cost totals**: `GetRowTotal` / `GetColumnTotal` via `CostHelper.ComputeItemCost`. `.dayplan-cost-total` badge (info-colored) in right date cell and MealType headers.
- **Row/column TR/CB breakdowns**: `GetRowTrCb` / `GetColumnTrCb` → `(decimal tr, decimal cb)`. Uses `CostHelper.BucketByCost` + `CostHelper.GetRecipePaymentTypes`. Mini badges next to total (TR=`#1565C0`, CB=`#6A1B9A`).
- **Month total badge**: `_monthTotal` computed after each `LoadAsync()`, `ClearMonthAsync()`, `RandomFillAsync()`. Displayed in Date header cell.

### Month navigation bar
- ±6 circular chips (13 months total). Current = green filled, has data = blue filled, empty = outlined.
- Uses `OnParametersSetAsync` for SPA re-navigation. `_monthlySummary` loaded via `GetMonthlySummaryAsync`.

### MealCell component (`Client/Components/MealCell.razor`)
- One cell per `(DateOnly date, MealType mealType)`. SortableJS drag & drop for reorder.
- **Whole cell draggable** — drag to move; Ctrl = copy. Footer `.footer-zone-side` handles for cell drag.
- **Slot total**: dbl-click = clear all items (`_clearPrimed` confirm intent).
- **Item interactions**: click = open add dialog; Ctrl+click = clone; dbl-click = delete.
- **Parameters**: `MealId`, `Items`, `IsActionTarget`, `IsBeingDragged`.
- **Callbacks**: `OnItemMoved`, `OnItemRemoved`, `OnAddRequested`, `OnOrderChanged`, `OnCellFooterDrop`, `OnItemCloneRequested`, `OnDragStarted`, `OnDragEnded`, `OnCellClearRequested`.
- **Payment badges**: TR/CB per item line. Item slots: `ItemSupplierCache.GetBestSupplier(itemId)`. Recipe slots: `CostHelper.GetRecipePaymentTypes(RecipeIngredientItemIds, Cache)`.
- **Item row layout**: `display:grid; grid-template-columns: 1fr auto auto` — name / badges / price (subgrid).
- **Slot TR/CB footer**: `SlotTrCb` loops items; slot total centered (`position:absolute; left:50%`); TR/CB far right. Only rendered when `SlotTotal > 0`.
- **`ShouldRender()` override**: compares `_renderedItems`, `_renderedMealId`, `_renderedIsBeingDragged`, `_renderedIsActionTarget`, `_renderedClearPrimed`, `_renderedPaymentTypes`. Snapshot updated in `OnAfterRenderAsync`.
- **Fire-and-forget JS**: `setFooterDragSource` / `clearFooterDragSource` use `_ = JS.InvokeVoidAsync(...)`.

### Add dialog (`Client/Components/AddItemDialog.razor`)
- `MudDialog` opened via `IDialogService.ShowAsync<AddItemDialog>`.
- **Parameters**: `SelectedDate`, `SelectedMealType`, `AllItems`, `AllRecipes`, `CurrentMealItems`, `OnSave (Func<AddItemDialogSaveDiff, Task>)`.
- **Diff-based save**: `_itemQty`/`_recipeQty` dicts. `LoadSlot(CurrentMealItems)` on init. `ComputeDiff()` → `AddItemDialogSaveDiff`: `ToAddItems`, `ToAddRecipes`, `ToUpdate`, `ToDelete`.
- **Two tabs**: Items / Recettes — single `_search` field, cleared on tab switch.
- **`_filterByMealType` toggle**: when true (default), Items tab filters by `CategoryAllowedMealTypes` (`MealTypeFlags` bit test). Recettes always shows all.
- **Dual view mode** (`_denseView`, default true, persisted `localStorage` key `add-dialog-dense`):
  - Dense: `MudDataGrid` — Items cols: Nom, Catégorie, Fournisseur, Prix unitaire, Unité, €/kg·L, Qté. Recettes: Nom, Portions, Coût estimé, Coût/portion, Ingrédients, Qté. Stepper always rendered (`visibility:hidden` at qty=0). Row click → increment.
  - Cards: `MudGrid` xs=6 sm=3 — gradient thumbnail, price, supplier, TR/CB badge, stepper. Card click → increment.
- **`MealRecap` sidebar** (460px right): `Client/Components/MealRecap.razor`. 5-col grid: `× | Nom | €/U | U | Total`. TR/CB breakdown in footer. Qty changes bidirectional with parent via `OnQtyChange`.
- **`ApplyDialogSaveAsync`** (DayPlan/Index): processes diff — deletes, updates (item + recipe: `MealItemSvc.UpdateAsync` with nullable `ItemId`/`RecipeId`), adds via `AddItemToSlotAsync`/`AddRecipeToSlotAsync`. RightPanel stays open.

### AddItemToSlotAsync / AddRecipeToSlotAsync
- `Task<bool> AddItemToSlotAsync(date, mealType, itemId, quantity)` — creates DailyMenu → Meal → MealItem lazily.
- `Task<bool> AddRecipeToSlotAsync(date, mealType, recipeId, quantity)` — same with `RecipeId`.
- All copy/clone/drag operations dispatch to correct helper based on `MealItemResponse.RecipeId.HasValue`.

### Cell drag-and-drop (DayPlan/Index)

All operations are **immediate** (no deferred save). `_saving` bool shows full-screen dark overlay.

**Cell-level (footer drag):** drag via `.footer-zone-side` → `HandleCellDragDropAsync` → move (default) or copy (Ctrl). Same-cell Ctrl drag = copy-in-place. Clear via dbl-click on slot total.

**Item-level:** Ctrl+Click = clone in same slot. Click (no Ctrl) = open dialog. Dbl-click = delete. Cross-cell SortableJS drag → `HandleMoveItemAsync` (Ctrl at drop = copy). Same-slot reorder → `HandleOrderChanged`.

**JS interop (`sortable-interop.js`):**
- `setFooterDragSource` / `clearFooterDragSource` / `getAndClearFooterDragSource` — manages cell drag state, Ctrl tracking, CSS classes.
- `addCellDragOverHandler` / `removeCellDragOverHandler` — native dragover/drop handlers; applies `meal-cell-drag-copy` or `meal-cell-drag-move`.
- SortableJS `onStart` tracks `_sortableDragItem` + shows `.sortable-copy-ghost`. `onEnd` reads Ctrl → `isCopy`; on copy moves element back before Blazor reconciles.
- `addRowPrimed` / `removeRowPrimed` / `addColumnPrimed` / `removeColumnPrimed` — overlay-based bounding box highlight. Single `_primedOverlay` at a time.
- `addEscapeHandler(dotNetRef)` / `removeEscapeHandler()` — Escape → `[JSInvokable] ExitCopyModeJs()`. Used by MenuPlan/Index copy mode.

### Bulk clear / random fill (DayPlan/Index)

| Action | Trigger | Implementation |
|--------|---------|----------------|
| Clear row | dbl-click date label | `ClearRowAsync(date)` → `MealSvc.DeleteBatchAsync` |
| Clear column | dbl-click MealType header | `ClearColumnAsync(mealType)` → `DeleteBatchAsync` |
| Clear month | DeleteSweep button | `ClearMonthAsync()` → `DeleteBatchAsync` |
| Random fill items | Casino button | `RandomFillAsync(RandomFillMode.Items)` |
| Random fill recipes | MenuBook button | `RandomFillAsync(RandomFillMode.Recipes)` |

**`DELETE /api/meals/batch`** — body: `DeleteMealsBatchRequest { List<int> Ids }`. Always 204. Service: `DeleteBatchAsync(List<int>) → Task`.

**`POST /api/meals/random-fill`** — body: `RandomFillRequest { CustomerId, Year, Month, Mode }`. Returns `List<DailyMenuResponse>`. Skips days with ≥1 meal. Item ranges: Breakfast(0-2), MorningSnack(0-1), Lunch(1-3), AfternoonSnack(0-1), Dinner(1-3). Pool built once before day loop. Recipe MealItems get `Unit = Piece`. Returns `[]` if pool empty.

---

## Drag & drop (fully implemented)

- SortableJS via JS interop (`sortable-interop.js`). All moves call the API immediately.
- Backend: `PATCH /mealitems/{id}/move` (cross-cell), `PATCH /mealitems/reorder` (same-slot).
- `MealItem.Order` — 1-based, gap-free, auto-renumbered on move.
- `MealItemResponse` includes `Order`, `UnitPrice`, `ContentQuantity`, `PurchaseUnit`, `ContentUnit`, `Unit`, `RecipeId?`, `RecipeName?`, `RecipeEstimatedCost?`, `RecipeIngredientItemIds (List<int>)`.

---

## CostHelper (`Client/Helpers/CostHelper.cs`)

- `Fr` — static `CultureInfo("fr-FR")` shared across all components (replaces per-component `_fr`/`FrCulture` fields).
- `PackageCost(totalQty, contentQty, unitPrice)` → `ceil(totalQty / contentQty) * unitPrice`.
- `ComputeItemCost(MealItemResponse item, decimal? bestUnitPrice = null)` — recipe: `RecipeEstimatedCost * Quantity`; item: `PackageCost(Quantity, ContentQuantity, bestUnitPrice ?? item.UnitPrice)`.
- `ComputePricePerKgL(unitPrice, contentQty, contentUnit)` → formatted `€/kg` or `€/L` string, or null.
- `GetRecipePaymentTypes(ingredientIds, cache)` → `IReadOnlyList<PaymentType>` — distinct payment types of recipe ingredients via cache.
- `BucketByCost(cost, paymentTypes)` → `(decimal tr, decimal cb)` — 0 types: (0,0); 1 type: full cost to that bucket; mixed: 50/50.

## MealTypeHelper (`Client/Helpers/MealTypeHelper.cs`)

Extension `ToFrenchLabel(this MealType)` — maps to French display strings. Used in DayPlan headers and AddItemDialog subtitle.

---

## Shopping Cart (right panel)

- `RightPanelState` — scoped service, global `MudDrawer` in `MainLayout`.
- `ShoppingCart.razor` — fed by DayPlan/Index on every `LoadAsync()`. Parameter: `Items (IEnumerable<MealItemResponse>)`. Implements `IDisposable`.
- **`ItemSupplierCache`** (`Client/Services/ItemSupplierCache.cs`) — scoped. `EnsureLoadedAsync()` calls `GET /api/itemsuppliers/best-by-item` once, caches `Dictionary<int, BestSupplierInfo>`. Shared by `ShoppingCart` and `MealCell`.
- **KI-2: recipes excluded** — `ComputeLines()` filters `.Where(i => !i.RecipeId.HasValue)`. Totals incomplete when plan contains recipes.
- Plain items grouped by `(PaymentType, SupplierName)` of cheapest supplier. Items with no `GetBestSupplier` result excluded.
- CSS grid: `grid-template-columns: 1fr 52px 58px 62px` (article / qty / U / total). Scrollable `.sc-scroll` + pinned `.sc-footer-wrap`.
- `CartLine` record: Name, TotalQuantity, Unit, ContentQuantity, ContentUnit, UnitPrice, Cost, SupplierName, PaymentType + `PackageCount = ceil(TotalQuantity / ContentQuantity)`. Cost = sum of `CostHelper.ComputeItemCost` per slot (ceil per slot).
- **Pinned footer**: TR | CB left-aligned; grand total right-aligned.
- `FmtMoney(decimal)` → `0.00 €`. `FmtQty(decimal, maxDec)` — no trailing zeros.

---

## Theme system

- `ThemeState` — scoped service. `AppTheme` enum: `Light`, `Dark`, `Custom`.
- Three static `MudTheme` objects: `_lightTheme`, `_darkTheme`, `_customTheme` (pure black).
- **CycleTheme**: Light → Dark → Custom → Light. Persisted `localStorage` key `"theme"`.
- AppBar: fixed dark navy gradient (does not change with theme).
- `DayPlan/Index` subscribes to `ThemeState.OnChange` for inline dark mode chip styles.

---

## Known issues / tech debt

| # | Severity | Location | Description |
|---|----------|----------|-------------|
| KI-2 | ★★★ | `ShoppingCart.razor` | Recipes excluded from cart. Footer totals incomplete. Fix: expand recipe MealItems via `RecipeIngredientItemIds` + cache. |
| KI-4 | ★ | `DailyMenuService.DuplicateMonthAsync` | `SaveChangesAsync` inside nested loops (~N×M). Fix: batch insertions, 2-pass save. |

---

## Coding conventions

- All code in **English** (variables, functions, files, comments).
- One file per class/record/enum.
- Follow existing slice structure exactly — do not introduce new patterns without explicit approval.
- CC briefs from CW: **intention + constraints only** — no code in briefs.
- Check **Known Issues** before working on any affected component.