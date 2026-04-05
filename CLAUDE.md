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

Category         — Id, Name, Description, ParentCategoryId (self-ref), SubCategories, Items
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

**New endpoint**: `GET /api/dailymenus/{customerId}/monthly-summary` → `List<MonthlySummaryResponse>(Year, Month, HasMeals, MonthlyCost)`
MonthlyCost = sum of `ceil(qty / ContentQuantity) * cheapest available UnitPrice` across all MealItems of the month, computed in C# after EF load.

**RecipeService**: All queries include `ItemSuppliers` via deep `ThenInclude` chain. `ComputeRecipeCost(Recipe r)` is a public static method (reused by `MealItemService`). Recipe response includes `EstimatedCost`. Ingredients ordered by `Order`.

**MealItemService**: `CreateAsync` accepts `ItemId?` or `RecipeId?` (exactly one must be set). Response maps `RecipeName`, `RecipeEstimatedCost` (via `RecipeService.ComputeRecipeCost`), `Unit`, `ContentQuantity`, `PurchaseUnit`, `ContentUnit`.

**New endpoint**: `POST /api/itemsuppliers/by-items` — body: `ByItemsRequest { List<int> ItemIds }` → `List<ItemPricingResponse>`. Returns all **available** `ItemSupplier` rows for the requested item IDs, with `ContentQuantity` and `Supplier` info (`Id`, `CompanyName`, `PaymentType`).

**New endpoint**: `GET /api/itemsuppliers/best-by-item` → `Dictionary<int, BestSupplierInfo>`. Returns the cheapest available supplier per item (globally, not filtered by item list). `BestSupplierInfo` DTO (`Shared/DTOs/ItemSupplierDtos.cs`): `ItemId`, `PaymentType`, `UnitPrice`, `SupplierName`. Used by `ItemSupplierCache` on DayPlan load.

### Frontend (Client)

| Slice        | Service | Index / Page    | Notes                                                              |
|--------------|---------|-----------------|--------------------------------------------------------------------|
| Layout       | —       | —               | ThemeState (Light/Dark/Custom), AppBar dark navy gradient, NavMenu split, RightPanelState, CycleTheme button (persisted to localStorage) |
| Category     | ✅      | ✅              | Reference implementation — new Save pattern applied                |
| Item         | ✅      | ✅              | FK CategoryId, PurchaseUnit + ContentQuantity + ContentUnit (3 cols) |
| Supplier     | ✅      | ✅              | Party fields + CompanyName, Siret, PaymentType (editable in grid + Edit page) |
| Customer     | ✅      | ✅              | Party fields + PaymentType — CalendarMonth button → `/menuplan/{id}` |
| ItemSupplier | ✅      | ✅              | Double FK dropdown, composite PK, snackbar 404/409                 |
| DailyMenu    | ✅      | ✅ via MenuPlan/Index | Route `/menuplan/{CustomerId}` — cards from `monthly-summary` endpoint |
| Meal         | ✅      | ✅ via DayPlan/Index  | FK DailyMenuId                                                |
| MealItem     | ✅      | ✅ via DayPlan/Index  | FK MealId, Order, UnitPrice, ContentQuantity, Unit; ItemId? or RecipeId? |
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

- 3 years of cards grouped by year: current month → Dec, then full N+1 and N+2.
- Unified "Voir le planning" button — navigates directly to `dayplans?customerId=X&year=Y&month=M` (no server-side creation).
- **HasData coloring**: current month = green (`#1B5E20`), has data = filled blue, empty = outlined.
- **MonthlyCost**: computed server-side (`ceil(qty / PackageSize) * cheapest available UnitPrice`), displayed on each card (EUR, FR locale).
- Data source: `GET /api/dailymenus/{customerId}/monthly-summary` → `List<MonthlySummaryResponse>` (`Year`, `Month`, `HasMeals`, `MonthlyCost`). Client service: `DailyMenuService.GetMonthlySummaryAsync`.

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
- **Row / column cost totals**: `GetRowTotal(DateOnly)` and `GetColumnTotal(MealType)` delegate to static `ComputeItemCost(MealItemResponse)` — returns `RecipeEstimatedCost * Quantity` for recipe items, `ceil(qty/ContentQuantity)*UnitPrice` for item items. Displayed as `.dayplan-cost-total` badge (info-colored, green-tinted border) — in the right date cell (below date label, inside `.date-right-inner`) and in each MealType header cell.

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
- **Item interactions**: click item = open add drawer; Ctrl+click = clone in same slot (`OnItemCloneRequested`); double-click = delete (`OnItemRemoved`).
- **Click anywhere on cell** (non-item area) = open add drawer.
- **Hover states**: `.meal-cell:hover` subtle tint; `.meal-cell:not(.meal-cell-empty):hover` green glow; `.meal-cell-item:hover` blue wash; `.meal-cell-item-recipe` purple tint (secondary) for recipe entries.
- **Parameters**: `MealId` (int?), `Items` (List\<MealItemResponse\>), `IsActionTarget` (bool — set externally when cell is a drop target); `IsBeingDragged` (bool — set externally when this cell is the drag source → `cell-drag-source` CSS, items show dashed border).
- **CSS visual states**: `meal-cell-drag-copy`, `meal-cell-drag-move` on target cell during drag; `cell-drag-source` on source cell; `cell-drag-copy-mode` on source cell when Ctrl held.
- **Callbacks**: `OnItemMoved(itemId, fromDate, fromMealType, toDate, toMealType, newIndex, isCopy)`, `OnItemRemoved`, `OnAddRequested`, `OnOrderChanged`, `OnCellFooterDrop`, `OnItemCloneRequested`, `OnDragStarted(date, mealType)`, `OnDragEnded`, `OnCellClearRequested`.
- **Payment badge**: each item line shows a small TR/CB badge (`.payment-badge-tr` blue / `.payment-badge-cb` purple) from `ItemSupplierCache.GetBestSupplier(itemId)`. CSS defined in `DayPlan/Index.razor` `<style>`.
- **ShouldRender() optimisation**: MealCell overrides `ShouldRender()` — compares `_renderedItems` (via `ItemsEqual`: id/quantity/order), `_renderedMealId`, `_renderedIsBeingDragged`, `_renderedIsActionTarget`, `_renderedClearPrimed`, `_renderedPaymentTypes` (snapshot of `GetCurrentPaymentTypes()`) against current params. Snapshot updated in `OnAfterRenderAsync`. Skips re-render when parent rebuilds but cell data hasn't changed.
- **Fire-and-forget JS drag calls**: `setFooterDragSource` and `clearFooterDragSource` use `_ = JS.InvokeVoidAsync(...)` (non-blocking) — drag start/end must not block Blazor's event thread.

### Add drawer (DayPlan/Index)
- Two tabs: **Items** (search + list) and **Recettes** (search + list with estimated cost).
- `_drawerTab` int tracks active tab; `_recipeSearch` separate search field.
- Tab is shared (same `_selectedDate` / `_selectedMealType`); reset on new open.
- Month nav chips apply dark mode inline styles from `ThemeState.IsDarkMode` — current=dark green, hasData=dark blue, empty=outlined dim. `DayPlan/Index` subscribes to `ThemeState.OnChange`.

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
- `Click` on item (no Ctrl) → open add drawer.
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

### Bulk clear / random fill operations (DayPlan/Index)

All operations use `_saving` overlay. Implemented as immediate API calls (no confirm dialog — primed highlight is the confirmation UX).

| Action | Trigger | Implementation |
|--------|---------|----------------|
| Clear row | dbl-click date label (left or right) | `ClearRowAsync(date)` → `MealSvc.DeleteBatchAsync(mealIds)` |
| Clear column | dbl-click MealType header | `ClearColumnAsync(mealType)` → `DeleteBatchAsync` |
| Clear month | DeleteSweep button (top-right header) | `ClearMonthAsync()` → `DeleteBatchAsync` |
| Random fill | Casino button (top-right header) | `RandomFillAsync()` → `MealSvc.RandomFillAsync(customerId, year, month)` |

**`DELETE /api/meals/batch`** — body: `DeleteMealsBatchRequest { List<int> Ids }`. Always returns 204 (unknown IDs silently ignored). Service: `DeleteBatchAsync(List<int>)` → `Task` (not `Task<bool>` — bulk delete is fire-and-forget at HTTP level).

**`POST /api/meals/random-fill`** — body: `RandomFillRequest { CustomerId, Year, Month }`. Returns `List<DailyMenuResponse>` (only newly created + pre-existing daily menus for that month). Service skips days that already have ≥1 meal. Item ranges per type: Breakfast(0-2), MorningSnack(0-1), Lunch(1-3), AfternoonSnack(0-1), Dinner(1-3). Uses only items with at least one available `ItemSupplier`. Returns `[]` if customer not found or no available items.

---

## Drag & drop (fully implemented)

- SortableJS via JS interop (`sortable-interop.js`).
- **Immediate save**: all moves and reorders call the API directly; no buffering, no Save All.
- SortableJS cross-cell drag supports Ctrl at drop time = copy (item added to target, source untouched).
- Backend: `PATCH /mealitems/{id}/move` (cross-cell), `PATCH /mealitems/reorder` (same-slot).
- `MealItem.Order` — 1-based, gap-free, auto-renumbered on move.
- `MealItemResponse` includes `Order`, `UnitPrice`, `ContentQuantity`, `PurchaseUnit`, `ContentUnit`, `Unit`, `RecipeId?`, `RecipeName?`, `RecipeEstimatedCost?`.

---

## Shopping Cart (right panel)

- `RightPanelState` — scoped service, global `MudDrawer` in `MainLayout`. Any page can push content.
- `ShoppingCart.razor` — fed by DayPlan/Index on every `LoadAsync()`. Implements `IDisposable`; unsubscribes from `RightPanel.OnChange` on dispose.
- **`ItemSupplierCache`** (`Client/Services/ItemSupplierCache.cs`) — scoped service. Calls `GET /api/itemsuppliers/best-by-item` once per DayPlan load (`EnsureLoadedAsync()`), caches `Dictionary<int, BestSupplierInfo>`. `GetBestSupplier(itemId)` returns cheapest available supplier info. Shared by `ShoppingCart` and `MealCell`.
- **Enriched display** (`_supplierDataLoaded = true`):
  - Items grouped by `(PaymentType, CompanyName)` of the **cheapest available supplier** per item. Group header color: TR = blue `#1565C0`, CB = purple `#6A1B9A`.
  - `EnrichedShoppingLine` (private record): Name, TotalQuantity, Unit, ContentQuantity, PackagesToBuy, BestCost, RetainedSupplierName, RetainedPaymentType.
  - Footer shows **best total** only (items + recipes combined). Worst/avg removed.
- **Fallback display** (`_supplierDataLoaded = false`): classic flat list with `_hasMissingPrices` warning.
- **Recettes** section unchanged: aggregated by `RecipeId`, line total = `RecipeEstimatedCost * totalQty`.

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