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
- `Item.PurchaseUnit` = unit of the package (e.g. Piece); `Item.ContentUnit` = unit of content (e.g. Piece)
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
| RefactorItemUnits                          | Item: Unit+PackageSize → PurchaseUnit+ContentQuantity+ContentUnit; drop IsStaple+MonthlyEstimate |
| AddUnitAndOrderToRecipeIngredient          | RecipeIngredient: add Unit(MeasurementUnit) + Order(int)                       |
| AddPaymentTypeToSupplier                   | Supplier: add PaymentType(int)                                                 |
| AddPaymentTypeToCustomer                   | Customer: add PaymentType(int)                                                 |
| AddPaymentTypeCheckConstraints             | CHECK constraints `PaymentType IN (0, 1)` on Suppliers and Customers tables    |
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
- **On-demand DB records**: DailyMenu and Meal records are created lazily when the first MealItem is added to a date/meal type.

---

## Service method signatures

| Case                                 | Return type        |
|--------------------------------------|--------------------|
| Create, no FK to validate            | `Task<T>`          |
| Create, FK(s) to validate            | `Task<T?>`         |
| Create, ambiguous 404 vs 409         | `Task<ResultType>` |
| Update (entity may not exist)        | `Task<T?>`         |
| Delete                               | `Task<bool>`       |

### 404 vs 409 pattern (ItemSupplier model)

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
- Never use `Title` or `Tooltip` directly on `MudIconButton`.

---

## Grid alignment rule

Stacked grids (pending + main): `table-layout: fixed; width: 100%` on both, identical `width: X%` on columns.

---

## Completed slices

### Backend (all complete: DTO / Validator / Service / Controller / Tests)
Category, Item, Supplier, Customer, ItemSupplier, DailyMenu, Meal, MealItem, Recipe

**New endpoint**: `GET /api/dailymenus/{customerId}/monthly-summary` → `List<MonthlySummaryResponse>(Year, Month, HasMeals, MonthlyCost, DaysWithMeals)`
MonthlyCost = sum of `ceil(qty / ContentQuantity) * cheapest available UnitPrice` across all MealItems of the month, computed in C# after EF load. `DaysWithMeals` = count of DailyMenu rows in the month that have at least one MealItem.

**RecipeService**: All queries include `ItemSuppliers` via deep `ThenInclude` chain. `ComputeRecipeCost(Recipe r)` is a public static method (reused by `MealItemService`). Recipe response includes `EstimatedCost`. Ingredients ordered by `Order`.

**MealItemService**: `CreateAsync` accepts `ItemId?` or `RecipeId?` (exactly one must be set). Response maps `RecipeName`, `RecipeEstimatedCost` (via `RecipeService.ComputeRecipeCost`), `RecipeIngredientItemIds`, `Unit`, `ContentQuantity`, `PurchaseUnit`, `ContentUnit`. `UnitPrice` mapped with `OrderBy(s => s.UnitPrice)` (cheapest available supplier — was incorrectly `OrderBy(s => s.SupplierId)`).

**CategoryResponse** includes `AllowedMealTypes (int)` — cast from `MealTypeFlags`.
**ItemResponse** includes `CategoryAllowedMealTypes (int)` — mapped from `item.Category.AllowedMealTypes`.

**New endpoint**: `POST /api/itemsuppliers/by-items` — body: `ByItemsRequest { List<int> ItemIds }` → `List<ItemPricingResponse>`. Returns all **available** `ItemSupplier` rows for the requested item IDs, with `ContentQuantity` and `Supplier` info (`Id`, `CompanyName`, `PaymentType`).

**New endpoint**: `GET /api/itemsuppliers/best-by-item` → `Dictionary<int, BestSupplierInfo>`. Returns the cheapest available supplier per item (globally, not filtered by item list). `BestSupplierInfo` DTO (`Shared/DTOs/ItemSupplierDtos.cs`): `ItemId`, `PaymentType`, `UnitPrice`, `SupplierName`. Used by `ItemSupplierCache` on DayPlan load.

**New endpoint**: `POST /api/dailymenus/duplicate` — body: `DuplicateMonthRequest { CustomerId, SourceYear, SourceMonth, TargetYear, TargetMonth }` → 204 (success) or 404 (customer not found). Server logic: delete all target-month `DailyMenu` rows first (cascade removes Meals+MealItems), then recreate day-by-day from source; days with `day > targetDaysInMonth` are silently skipped. Validator: `DuplicateMonthRequestValidator` (`Shared/Validators/`) — all fields > 0, months 1–12, source ≠ target. Client method: `DailyMenuService.DuplicateMonthAsync(DuplicateMonthRequest) → Task<bool>`.

### Frontend (Client)

| Slice        | Service | Index / Page    | Notes                                                              |
|--------------|---------|-----------------|--------------------------------------------------------------------|
| Layout       | —       | —               | ThemeState (Light/Dark/Custom), AppBar dark navy gradient, NavMenu split, RightPanelState, CycleTheme button (persisted to localStorage) |
| Category     | ✅      | ✅              | Reference implementation — new Save pattern applied                |
| Item         | ✅      | ✅              | FK CategoryId, PurchaseUnit + ContentQuantity + ContentUnit (3 cols) |
| Supplier     | ✅      | ✅              | Party fields + CompanyName, Siret, PaymentType (editable in grid + Edit page) |
| Customer     | ✅      | ✅              | Party fields + PaymentType — CalendarMonth button → `/menuplan/{id}` |
| ItemSupplier | ✅      | ✅              | Double FK dropdown, composite PK, snackbar 404/409; dropdown shows `CompanyName ?? Name`; ItemName/SupplierName columns `Editable="false"` |
| DailyMenu    | ✅      | ✅ via MenuPlan/Index | Route `/menuplan/{CustomerId}` — cards from `monthly-summary` endpoint |
| Meal         | ✅      | ✅ via DayPlan/Index  | FK DailyMenuId                                                |
| MealItem     | ✅      | ✅ via DayPlan/Index  | FK MealId, Order, UnitPrice, ContentQuantity, Unit; ItemId? or RecipeId?; add/edit via `AddItemDialog` (diff-based, MealRecap panel, meal-type filter) |
| Recipe       | ✅      | ✅ `/recipes`   | MudDataGrid + HierarchyColumn (inline ingredient editing per row) + RecipeDialog (create/edit) |

---

## Recipes/Index — recipe management

Route: `/recipes`.

- `MudDataGrid` read-only with `HierarchyColumn` — child row shows the ingredient list inline.
- Ingredient editing in child row: pending draft row + dirty tracking (same pattern as Category/Index).
- Per-ingredient fields: ItemId (dropdown), Quantity, Unit (enum select), Order (int). Ordered by `Order` in display.
- Create/edit recipe via `RecipeDialog.razor` (MudDialog). Fields: Name, Description, BaseServings.
- Estimated cost column: `RecipeService.ComputeRecipeCost` — `ceil(Qty / ContentQuantity) * UnitPrice` per ingredient.
- Client service: `RecipeService` (`Client/Services/RecipeService.cs`). Methods: `GetAllAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `AddIngredientAsync`, `UpdateIngredientAsync`, `DeleteIngredientAsync`.

---

## MenuPlan/Index — 3-year card grid

Route: `/menuplan/{CustomerId:int}` — always scoped to a customer.
Navigation entry point: `Customer/Index` — CalendarMonth icon button per row.

- Default: 3 years of cards grouped by year — current month → Dec, then full N+1 and N+2.
- **Show past months toggle** (History icon button): when on, adds the 12 preceding months (split by year if they straddle a year boundary). `_showPast` bool, `TogglePast()` re-runs `BuildSlotsByYear`.
- **Card click = navigate** to `dayplans?customerId=X&year=Y&month=M` — 280ms `Task.Delay` on click so dbl-click can cancel it via `CancellationTokenSource`.
- **HasData coloring** (inline styles via `GetCardStyle(slot)`): current month = green border `#7EC89A` (dark) / `success`; has data = blue border `#5B9EC9` (dark) / `primary`; empty = dim border. CSS class `.menuplan-card` adds hover lift + shadow.
- **Average daily cost**: `Moy. X.XX €/j` displayed in CardActions when `DaysWithMeals > 0` (`cost / daysWithMeals`). Monospace `.monthly-avg-cost` caption.
- **MonthlyCost**: server-side `ceil(qty / ContentQuantity) * cheapest UnitPrice`, displayed on each card.
- Data source: `GET /api/dailymenus/{customerId}/monthly-summary` → `List<MonthlySummaryResponse>`. Client: `DailyMenuService.GetMonthlySummaryAsync`. `_summaries` cached to avoid reload on `TogglePast`.

### Copy mode (duplicate month)

- **Enter copy mode**: dbl-click on a card that `HasMeals` → `EnterCopyModeAsync(slot)` → `_copySourceMonth = slot`. Source card rendered with amber border + dark overlay background. A full-screen invisible backdrop `<div>` (z-index:5) is injected to catch outside-clicks → `ExitCopyModeAsync`.
- **JS Escape handler**: `addEscapeHandler(dotNetRef)` / `removeEscapeHandler()` in `sortable-interop.js`. Calls `[JSInvokable] ExitCopyModeJs()` on Escape.
- **In copy mode — click source card**: ignored (no-op).
- **In copy mode — click target card**: `HandleTargetSelectedAsync(target)`. If target `HasMeals`, opens `DuplicateMonthDialog` (MudDialog, `MudSelect` of options, warns about overwrite). On confirm → `ExecuteDuplicateAsync` → `POST /api/dailymenus/duplicate` → reload + `RefreshCopySource()` (stays in copy mode to allow chained copies).
- **In copy mode — dbl-click another card** with data: re-selects it as source.
- **Delete source month button**: shown inside source card (top-right, `@onclick:stopPropagation`). `ClearSourceMonthAsync()` uses `MudMessageBox` for confirmation, then `MealSvc.DeleteBatchAsync`. Exits copy mode after clear.
- **Hover effects**: `_hoverTarget` tracks currently hovered card. Target cards pulsate (CSS animation `copy-target-pulse`) when not hovered; scale+shadow on hover. Copy icon (`ContentCopy`) shown top-right of hovered target.
- **`GetCardCssClass(slot)`** returns `menuplan-card`, `menuplan-card menuplan-card-copy-hovered`, or `menuplan-card menuplan-card-copy-target`.
- **`GetCardStyle(slot)`** returns full inline style string covering normal mode + copy-mode source/target/hovered states.
- `DuplicateMonthDialog.razor` (`Client/Pages/MenuPlans/`): `SourceTitle`, `Options (List<TargetOption>)`, `PreSelected`. `TargetOption` sealed record `(Year, Month, Title, HasData)`. Shows overwrite warning when selected option `HasData`.

---

## DayPlan/Index — monthly calendar view

### Layout
- CSS grid: 7 columns — Date (80px) + 5 MealTypes + Date-right (80px). Wrapped in `.dayplan-grid-wrapper` (flex, `user-select: none`).
- Cell wrapper `<div>` carries `@key="@((capturedDate, capturedMt))"` for stable Blazor diffing across re-renders.
- One row per day of the month, FR locale, weekend/holiday coloring.
- Date labels: `.date-label` flex column — DOW abbreviation (small, uppercase) + day number (large). Classes: `date-label-weekend` (opacity), `date-label-holiday` (warning color).
- `FrenchHolidays.cs` helper for public holidays (fixed + Easter-based).
- **Row-primed highlight**: mousedown on either date cell (left or right) calls `addRowPrimed(date)` JS — creates a single absolutely-positioned `.primed-axis-overlay` div covering the bounding rect of all `[data-rowdate="yyyy-MM-dd"]` elements (red tint, pointer-events: none). mouseup/mouseleave calls `removeRowPrimed(date)` → removes overlay. dbl-click fires `ClearRowAsync(date)` (confirm-intent pattern). All date cells and meal cells carry `data-rowdate` attribute.
- **Column-primed highlight**: mousedown on a MealType header calls `addColumnPrimed(mealTypeInt)` JS — same overlay mechanism over all `[data-colmealtype="X"]` elements. dbl-click fires `ClearColumnAsync(mealType)`. Row-primed and column-primed are mutually exclusive (only one `_primedOverlay` exists at a time). All meal cell wrapper divs carry `data-colmealtype` attribute.
- **`.primed-axis-overlay`**: CSS class on the overlay div — `position: absolute; pointer-events: none; z-index: 10; background: error 12%; border: error 30%; border-radius: 4px`. Requires `.dayplan-grid { position: relative; overflow: hidden }` (already set).
- **Row / column cost totals**: `GetRowTotal(DateOnly)` and `GetColumnTotal(MealType)` delegate to `ComputeItemCost(MealItemResponse)` — delegates to `CostHelper.ComputeItemCost(item, ItemSupplierCache.GetBestSupplier(itemId)?.UnitPrice)`. Displayed as `.dayplan-cost-total` badge (info-colored, green-tinted border) — in the right date cell and in each MealType header cell.
- **Row / column TR/CB breakdowns**: `GetRowTrCb(DateOnly)` and `GetColumnTrCb(MealType)` return `(decimal tr, decimal cb)`. Helper `BucketRecipeCost(item, cost, ref tr, ref cb)` — uses `item.RecipeIngredientItemIds` to find distinct payment types; 1 type → full cost to that bucket; mixed → 50/50 split. Mini inline badges shown next to the `.dayplan-cost-total` (TR=`#1565C0`, CB=`#6A1B9A`). Row date cell wrapped in `date-right-inner` div for layout. `rowTotal` and `rowTrCb` computed once before `foreach (var mt in _mealTypes)` loop.
- **Month total badge**: `_monthTotal` (sum of `GetRowTotal` for all dates) is computed after each `LoadAsync()`, `ClearMonthAsync()`, and `RandomFillAsync()`. Displayed as `.dayplan-cost-total` badge in the **Date header** (top-left cell of the grid).

### Month navigation bar
- Horizontal strip of ±6 circular chips above the calendar (13 months total).
- Current month = green filled (`#1B5E20`), has data = blue filled, empty = outlined.
- Click navigates to `dayplans?customerId=X&year=Y&month=M` — no server-side creation needed.
- Uses `OnParametersSetAsync` (not `OnInitializedAsync`) for SPA re-navigation; triggered by `(CustomerId, Year, Month)` key change.
- `_monthlySummary` loaded via `DailyMenuSvc.GetMonthlySummaryAsync(CustomerId)` to detect `HasMeals` per month.

### MealCell component (`Client/Components/MealCell.razor`)
- One cell per `(DateOnly date, MealType mealType)`.
- Ordered list of items (SortableJS drag & drop for reorder within/across slots).
- `meal-cell-empty` class when no items; `.meal-cell:not(.meal-cell-empty)` gets subtle green tint.
- Displays price per item + slot total (EUR, FR locale, `ceil(qty/PackageSize)*UnitPrice`).
- **Whole cell is `draggable="true"`** — drag cell anywhere to move; Ctrl held = copy. Footer has two `.footer-zone-side` draggable divs flanking the slot total.
- **Slot total**: double-click = clear all items (`OnCellClearRequested`). `_clearPrimed` state: mousedown turns total red (`total-primed` CSS) to confirm intent.
- **Item interactions**: click item = open add dialog; Ctrl+click = clone in same slot (`OnItemCloneRequested`); double-click = delete (`OnItemRemoved`).
- **Click anywhere on cell** (non-item area) = open add dialog.
- **Hover states**: `.meal-cell:hover` subtle tint; `.meal-cell:not(.meal-cell-empty):hover` green glow; `.meal-cell-item:hover` blue wash; `.meal-cell-item-recipe` purple tint (secondary) for recipe entries.
- **Parameters**: `MealId` (int?), `Items` (List\<MealItemResponse\>), `IsActionTarget` (bool — set externally when cell is a drop target); `IsBeingDragged` (bool — set externally when this cell is the drag source → `cell-drag-source` CSS, items show dashed border).
- **CSS visual states**: `meal-cell-drag-copy`, `meal-cell-drag-move` on target cell during drag; `cell-drag-source` on source cell; `cell-drag-copy-mode` on source cell when Ctrl held.
- **Callbacks**: `OnItemMoved(itemId, fromDate, fromMealType, toDate, toMealType, newIndex, isCopy)`, `OnItemRemoved`, `OnAddRequested`, `OnOrderChanged`, `OnCellFooterDrop`, `OnItemCloneRequested`, `OnDragStarted(date, mealType)`, `OnDragEnded`, `OnCellClearRequested`.
- **Payment badge**: each item line shows small TR/CB badge(s) (`.payment-badge-tr` blue / `.payment-badge-cb` purple). Item slots: from `ItemSupplierCache.GetBestSupplier(itemId)`. Recipe slots: from `GetRecipePaymentTypes(item)` — iterates `item.RecipeIngredientItemIds`, calls `Cache.GetBestSupplier` per ingredient, returns distinct ordered `PaymentType` list. CSS: text-only colored label (no border/background). CSS defined in `DayPlan/Index.razor` `<style>`.
- **CSS item row layout**: `.meal-cell` list uses `display: grid; grid-template-columns: 1fr auto auto`. Each `.meal-cell-item` spans all 3 columns via `subgrid` — name col 1, `.meal-cell-item-badges` col 2 (badges), `.meal-cell-item-price` col 3 (price). Ensures badges and prices align across all rows.
- **Slot TR/CB mini-badges**: `SlotTrCb` computed property returns `(decimal tr, decimal cb)` — loops items, delegates to `GetRecipePaymentTypes` for recipes (if 1 type → full cost; if mixed → 50/50 split), uses `Cache.GetBestSupplier` for plain items. Footer layout: slot total is `position:absolute; left:50%; transform:translateX(-50%)` (centered); TR/CB badge is `position:absolute; right:4px` (far right). Only rendered when `SlotTotal > 0`.
- **ShouldRender() optimisation**: MealCell overrides `ShouldRender()` — compares `_renderedItems` (via `ItemsEqual`: id/quantity/order), `_renderedMealId`, `_renderedIsBeingDragged`, `_renderedIsActionTarget`, `_renderedClearPrimed`, `_renderedPaymentTypes` (snapshot of `GetCurrentPaymentTypes()`) against current params. `GetCurrentPaymentTypes()` includes both direct item IDs and recipe ingredient item IDs so ShouldRender detects cache changes for recipe slots. Snapshot updated in `OnAfterRenderAsync`. Skips re-render when parent rebuilds but cell data hasn't changed.
- **Fire-and-forget JS drag calls**: `setFooterDragSource` and `clearFooterDragSource` use `_ = JS.InvokeVoidAsync(...)` (non-blocking) — drag start/end must not block Blazor's event thread.

### Add dialog (`Client/Components/AddItemDialog.razor`)
- `MudDialog` opened via `IDialogService.ShowAsync<AddItemDialog>` from `DayPlan/Index.OpenAddDialog`.
- **Parameters**: `SelectedDate`, `SelectedMealType`, `AllItems`, `AllRecipes`, `CurrentMealItems` (List\<MealItemResponse\> — existing items in slot), `OnSave` (Func\<AddItemDialogSaveDiff, Task\>).
- **Diff-based save**: dialog holds local `_itemQty`/`_recipeQty` dictionaries. `OnInitializedAsync` calls `LoadSlot(CurrentMealItems)`. `LoadSlot()` clears and reloads all four dicts (`_itemQty`, `_recipeQty`, `_existingByItemId`, `_existingByRecipeId`). `ComputeDiff()` produces `AddItemDialogSaveDiff` (record in `Client/Components/AddItemDialogSaveDiff.cs`): `ToAddItems`, `ToAddRecipes`, `ToUpdate (MealItemId, NewQty)`, `ToDelete (MealItemId)`. `ConfirmAsync` calls `OnSave(diff)` then closes.
- **Two tabs**: Items / Recettes — switched via `MudButtonGroup` (single `_search` field shared, cleared on tab switch).
- **`_showAll` toggle** (FilterAlt icon button): when false (default), Items tab filters by `CategoryAllowedMealTypes` via `PassesMealTypeFilter`. Uses `MealTypeFlags` bit test on `ItemResponse.CategoryAllowedMealTypes`. Recettes tab always shows all.
- **Dual view mode** (`_denseView`, default `true`): toggled by icon button, persisted to `localStorage` key `add-dialog-dense`.
  - Dense (list): `MudDataGrid` — All columns have `HeaderStyle="text-align:center;"`. Items columns: Nom, Catégorie, Fournisseur (`Sortable=true`, `SortBy` SupplierName from cache), Prix unitaire (`Sortable=true`, `SortBy` UnitPrice from cache), Unité, €/kg ou /L, **Qté** (`SortBy` current qty). Recettes: Nom, Portions, Coût estimé, Coût/portion, Ingrédients, **Qté** (`SortBy` current qty). Stepper: `−`, count, `+` always rendered — `−` and count use `visibility:hidden` (not conditional) when qty=0 to prevent layout shift; container `min-width:90px`. Row click → `IncrItemQty`/`IncrRecipeQty` (always increments, no guard). `RowClassFunc` adds `qty-row-selected` CSS when qty>0.
  - Cards: `MudGrid` xs=6 sm=3 — gradient thumbnail (hue derived from category/name hash), price, supplier, TR/CB badge, content info, stepper at bottom. Card border highlighted (primary/secondary) when qty>0. Card click → `IncrItemQty`/`IncrRecipeQty` (always increments).
- **`MealRecap` sidebar** (460px, right of list): `Client/Components/MealRecap.razor`. Parameters: `Items`, `MealType`, `CurrentDate`, `OnQtyChange`. Header shows `MealType.ToFrenchLabel()` + date (`"dd MMM"` FR locale). 5-col grid: `× | Nom | €/U | U | Total`. `×` column: `MudIconButton Close` → `RemoveFromRecapAsync` sends qty=0 to parent. `€/U` column: unit price from cache (`decimal? UnitPrice` on `RecapLine`). `U` column: package count (or `—` for recipes). `OnQtyChange` callback → bidirectional: changes in recap update `_itemQty`/`_recipeQty` in parent. TR/CB breakdown computed in `ComputeLines()` (`_trTotal`, `_cbTotal`); same bucketing logic as DayPlan. Footer: flex row — TR (blue) | CB (purple) | total (right-aligned). Qty buttons: `border-radius:50%` (circular).
- **`ApplyDialogSaveAsync`** (in `DayPlan/Index`): processes `AddItemDialogSaveDiff` — deletes, updates (item: `MealItemSvc.UpdateAsync`; recipe: delete+re-add since no recipe update endpoint), adds items via `AddItemToSlotAsync`, adds recipes via `AddRecipeToSlotAsync`. `OpenAddDialog` passes `currentItems` captured in closure; `_selectedDate`/`_selectedMealType` fields removed.
- **`MealItemService` client `UpdateAsync(int id, UpdateMealItemRequest)`** — `PUT /api/mealitems/{id}` → `MealItemResponse?`. Added to `Client/Services/MealItemService.cs`.
- `ComputePricePerKgL` duplicated locally (same formula as `ShoppingCart`).
- `MealTypeHelper.ToFrenchLabel()` used in subtitle. Title no longer prefixed with "Ajouter — ".
- `RightPanel.Close()` not called when opening the add dialog — shopping cart stays visible.

### AddItemToSlotAsync / AddRecipeToSlotAsync — on-demand creation helpers
- `Task<bool> AddItemToSlotAsync(date, mealType, itemId, quantity)` — creates DailyMenu → Meal → MealItem lazily.
- `Task<bool> AddRecipeToSlotAsync(date, mealType, recipeId, quantity)` — same pattern with `RecipeId` on `CreateMealItemRequest`.
- All copy/clone/cell-drag operations dispatch to the correct helper based on `MealItemResponse.RecipeId.HasValue`.

### Cell drag-and-drop (DayPlan/Index — fully implemented)

All drag & drop operations are **immediate** (no deferred save, no Save All button).
A `_saving` bool shows a full-screen dark overlay (`dayplan-overlay`) during any async operation.

**Cell-level (footer drag):**
- Drag a cell (via `.footer-zone-side` handles) to another cell → `HandleCellDragDropAsync` → `ExecuteCellActionAsync` (move by default, Ctrl held = copy).
- Dragging onto the same cell with Ctrl = copy-in-place (allowed).
- No trash zone — clear via double-click on slot total instead.
- `_draggingCell` (nullable tuple) tracks which cell is dragging; passed as `IsBeingDragged` to MealCell.

**Item-level operations:**
- `Ctrl+Click` on item → `CloneItemAsync` (adds duplicate with same quantity in same slot).
- `Click` on item (no Ctrl) → open add dialog.
- `Double-click` on item → delete (`RemoveItemAsync`).
- Cross-cell item drag via SortableJS → **immediate** `HandleMoveItemAsync` (supports Ctrl at drop = copy).
- Same-slot reorder via SortableJS → **immediate** `HandleOrderChanged` (calls API directly; reloads on failure).

**JS interop (`sortable-interop.js`):**
- `setFooterDragSource(element, date, mealType, initialCopy)` — registers drag source; `element` is the footer, `cell = element.parentElement`. Attaches keydown/keyup/dragover listeners for live Ctrl tracking. Applies `cell-drag-copy-mode` CSS on source cell.
- `clearFooterDragSource()` — exposed as `window.clearFooterDragSource`; called on `HandleFooterDragEnd`.
- `getAndClearFooterDragSource()` → `"date|mealType|1|0"` string or `null`.
- `addCellDragOverHandler(element)` / `removeCellDragOverHandler(element)` — native `dragover/enter/leave/drop` handlers; adds `meal-cell-drag-copy` or `meal-cell-drag-move` CSS class on both footer-drag and SortableJS hover.
- SortableJS `onStart` tracks `_sortableDragItem` + shows `.sortable-copy-ghost` at source. `onEnd` reads Ctrl at drop time → `isCopy`; on copy, moves element back to source before Blazor reconciles.
- `addRowPrimed(date)` / `removeRowPrimed(date)` — overlay-based: computes bounding box of all `[data-rowdate="date"]` elements, injects a single `.primed-axis-overlay` div into `.dayplan-grid`; remove clears it. No CSS class toggling on individual cells.
- `addColumnPrimed(mealType)` / `removeColumnPrimed(mealType)` — same overlay mechanism over `[data-colmealtype="X"]` elements. Only one `_primedOverlay` exists at a time — activating one axis automatically replaces any previous overlay.
- `addEscapeHandler(dotNetRef)` / `removeEscapeHandler()` — registers/removes a `keydown` listener on `document`; on Escape, calls `dotNetRef.invokeMethodAsync('ExitCopyModeJs')`. Used by `MenuPlan/Index` copy mode.

### Bulk clear / random fill operations (DayPlan/Index)

All operations use `_saving` overlay. Implemented as immediate API calls (no confirm dialog — primed highlight is the confirmation UX).

| Action | Trigger | Implementation |
|--------|---------|----------------|
| Clear row | dbl-click date label (left or right) | `ClearRowAsync(date)` → `MealSvc.DeleteBatchAsync(mealIds)` |
| Clear column | dbl-click MealType header | `ClearColumnAsync(mealType)` → `DeleteBatchAsync` |
| Clear month | DeleteSweep button (top-right header) | `ClearMonthAsync()` → `DeleteBatchAsync` |
| Random fill items | Casino button (top-right header) | `RandomFillItemsAsync()` → `RandomFillAsync(RandomFillMode.Items)` |
| Random fill recipes | MenuBook button (top-right header) | `RandomFillRecipesAsync()` → `RandomFillAsync(RandomFillMode.Recipes)` |

**`DELETE /api/meals/batch`** — body: `DeleteMealsBatchRequest { List<int> Ids }`. Always returns 204 (unknown IDs silently ignored). Service: `DeleteBatchAsync(List<int>)` → `Task` (not `Task<bool>` — bulk delete is fire-and-forget at HTTP level).

**`POST /api/meals/random-fill`** — body: `RandomFillRequest { CustomerId, Year, Month, Mode }`. `Mode` = `RandomFillMode` enum (`Items` | `Recipes`, defined in `Shared/DTOs/MealDtos.cs`). Returns `List<DailyMenuResponse>`. Service skips days that already have ≥1 meal. Item ranges per type: Breakfast(0-2), MorningSnack(0-1), Lunch(1-3), AfternoonSnack(0-1), Dinner(1-3). Pool = items only (mode=Items) OR recipes only (mode=Recipes). Pool built **once** before the day loop (not rebuilt per slot). Recipe MealItems get `Unit = Piece`. Response includes full ThenInclude chain so `RecipeIngredientItemIds` is populated. Returns `[]` if pool empty. Client: two buttons — Casino (items) + MenuBook (recipes).

---

## Drag & drop (fully implemented)

- SortableJS via JS interop (`sortable-interop.js`).
- **Immediate save**: all moves and reorders call the API directly; no buffering, no Save All.
- SortableJS cross-cell drag supports Ctrl at drop time = copy (item added to target, source untouched).
- Backend: `PATCH /mealitems/{id}/move` (cross-cell), `PATCH /mealitems/reorder` (same-slot).
- `MealItem.Order` — 1-based, gap-free, auto-renumbered on move.
- `MealItemResponse` includes `Order`, `UnitPrice`, `ContentQuantity`, `PurchaseUnit`, `ContentUnit`, `Unit`, `RecipeId?`, `RecipeName?`, `RecipeEstimatedCost?`, `RecipeIngredientItemIds` (List\<int\>).

---

## CostHelper (`Client/Helpers/CostHelper.cs`)

Static helper class — canonical cost formulas shared across `MealCell`, `DayPlan/Index`, `ShoppingCart`.

- `PackageCost(totalQty, contentQty, unitPrice)` → `ceil(totalQty / contentQty) * unitPrice`. Guards `contentQty <= 0`.
- `ComputeItemCost(MealItemResponse item, decimal? bestUnitPrice = null)` — recipe: `RecipeEstimatedCost * Quantity`; item: `PackageCost(Quantity, ContentQuantity, bestUnitPrice ?? item.UnitPrice)`. `bestUnitPrice` comes from `ItemSupplierCache` and takes priority over the DTO field.

## MealTypeHelper (`Client/Helpers/MealTypeHelper.cs`)

Extension method `ToFrenchLabel(this MealType)` — maps enum values to French display strings (Breakfast → "Petit-déjeuner", etc.). Used in `DayPlan/Index` column headers and `AddItemDialog` subtitle.

---

## Shopping Cart (right panel)

- `RightPanelState` — scoped service, global `MudDrawer` in `MainLayout`. Any page can push content.
- `ShoppingCart.razor` — fed by DayPlan/Index on every `LoadAsync()`. Parameter: `Items` (`IEnumerable<MealItemResponse>`) only — `Recipes` param removed. Implements `IDisposable`; unsubscribes from `RightPanel.OnChange` on dispose. Header: dark navy gradient (`#06091A → #0C1228`) with `ShoppingCart` icon.
- **`ItemSupplierCache`** (`Client/Services/ItemSupplierCache.cs`) — scoped service. Calls `GET /api/itemsuppliers/best-by-item` once per DayPlan load (`EnsureLoadedAsync()`), caches `Dictionary<int, BestSupplierInfo>`. `GetBestSupplier(itemId)` returns cheapest available supplier info. Shared by `ShoppingCart` and `MealCell`.
- **Unified display** (always enriched — no fallback):
  - Items (plain only, no recipe section) grouped by `(PaymentType, SupplierName)` of the cheapest available supplier. Items where `GetBestSupplier` returns null are excluded. Group header uses `--sc-accent` CSS var (TR=`#1565C0`, CB=`#6A1B9A`).
  - CSS grid layout: `.sc-col-labels`, `.sc-row`, `.sc-footer-row` share `grid-template-columns: 1fr 52px 58px 62px` (article / qty / U / total). Alternating `.sc-row-alt` zebra. Scrollable `.sc-scroll` flex area + pinned `.sc-footer-wrap`.
  - `CartLine` private record: Name, TotalQuantity, Unit, ContentQuantity, ContentUnit, UnitPrice, **Cost**, SupplierName, PaymentType + computed property `PackageCount = ceil(TotalQuantity / ContentQuantity)`. Qty column displays `PackageCount` (nb de colis). Cost = sum of `CostHelper.ComputeItemCost(i, best.UnitPrice)` per slot (ceil per slot, not global ceil).
  - `ComputePricePerKgL(unitPrice, contentQty, contentUnit)` — returns formatted €/kg or €/L; null otherwise.
  - **Pinned footer** (`.sc-footer-row`): TR | CB left-aligned; grand total right-aligned. `_total = _trTotal + _cbTotal`.
  - `_loaded` bool replaces `_supplierDataLoaded`. `LoadAsync()` called when panel opens; `ComputeLines()` called on `OnParametersSet` if already loaded.
- `FmtMoney(decimal)` → `0.00 €` (fixed 2 decimals, FR locale). `FmtQty(decimal, maxDec)` — no trailing zeros.

---

## Theme system

- `ThemeState` — scoped service (`Client/Services/ThemeState.cs`). `AppTheme` enum: `Light`, `Dark`, `Custom` (black variant).
- `MainLayout` subscribes to `ThemeState.OnChange`, passes `IsDarkMode` to `MudThemeProvider`.
- **Three static `MudTheme` objects**: `_lightTheme` (custom warm palette), `_darkTheme` (dark navy/blue), `_customTheme` (pure black).
- **CycleTheme button** in AppBar: cycles Light → Dark → Custom → Light. Persisted to `localStorage` key `"theme"`. Restored in `OnInitializedAsync`.
- AppBar style is a fixed dark navy gradient (does not change with theme).
- `DayPlan/Index` subscribes to `ThemeState.OnChange` to re-render dark mode chip styles inline.

---

## Coding conventions

- All code in **English** (variables, functions, files, comments).
- One file per class/record/enum.
- Follow existing slice structure exactly — do not introduce new patterns without explicit approval from Lead Dev.
- CC briefs from CW: **intention + constraints only** — no code in briefs.

---

## How to use this file

1. Read it at the start of every session.
2. If a decision conflicts with a rule above → **stop and ask Lead Dev**.
3. After completing a task, remind Lead Dev to update this file if the architecture evolved.