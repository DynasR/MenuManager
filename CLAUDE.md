# CLAUDE.md — MenuManager context for Claude Code

> Single source of truth for CC. Update after each significant architectural decision.

## Index
- [Project & Network](#project--network)
- [Domain model](#domain-model)
- [Architecture rules](#architecture-rules)
- [Service signatures & patterns](#service-signatures--patterns)
- [Frontend slice map](#frontend-slice-map)
- [Key component details](#key-component-details)
- [CostHelper & helpers](#costhelper--helpers)
- [Shopping Cart](#shopping-cart)
- [Known issues](#known-issues)
- [Coding conventions](#coding-conventions)

---

## Project & Network

**MenuManager** — meal planning web app.
Stack: Blazor WASM (PWA) + ASP.NET Core Web API (.NET 9) + EF Core 9 + PostgreSQL 16 (Docker).

```
/Client   → Blazor WASM + MudBlazor
/Server   → ASP.NET Core Web API
/Shared   → Entities, DTOs, Validators, Enums (pure class library — NO EF dependency)
/Tests    → xUnit + bUnit + FluentAssertions + Moq
```

| Service    | URL / Port            |
|------------|-----------------------|
| API Server | http://localhost:5075 |
| PostgreSQL | localhost:5432 / menumanager / admin |

Client reads `ServerUrl` from `Client/wwwroot/appsettings.json`.

---

## Domain model

```
Party (abstract, TPT)
  ├── Customer   — PasswordHash, PasswordSalt, PaymentType, ICollection<DailyMenu>
  └── Supplier   — CompanyName, Siret, PaymentType, ICollection<ItemSupplier>

Category         — Id, Name, Description, ParentCategoryId (self-ref),
                   AllowedMealTypes(MealTypeFlags, default=Breakfast|Lunch|Dinner|Snack)
Item             — Id, Name, PurchaseUnit(MeasurementUnit), ContentQuantity(decimal,default=1),
                   ContentUnit(MeasurementUnit), CategoryId(int)
ItemSupplier     — PK composite (ItemId+SupplierId), UnitPrice(10,2), SupplierRef, IsAvailable
DailyMenu        — Id, Date(DateOnly), CustomerId — unique(CustomerId,Date)
Meal             — Id, MealType, DailyMenuId — unique(DailyMenuId,MealType)
MealItem         — Id, Quantity(10,3), Notes, Order(int), Unit, MealId, ItemId?, RecipeId?
Recipe           — Id, Name, Description, BaseServings, ICollection<RecipeIngredient>
RecipeIngredient — PK composite (RecipeId+ItemId), Quantity(10,3), Unit, Order(int)

MealType     — Breakfast, MorningSnack, Lunch, AfternoonSnack, Dinner
MeasurementUnit — Piece, Gram, Kilogram, Milliliter, Liter
PaymentType  — TR, CB
MealTypeFlags (flags) — None=0, Breakfast=1, Snack=2, Lunch=4, Dinner=8
```

Hierarchy: **Customer → DailyMenu → Meal → MealItem**.

All EF config (precision, indexes, TPT, composite PKs) lives **exclusively** in `AppDbContext.OnModelCreating()`.

### ContentQuantity rule
- `MealItem.Quantity` = quantity consumed. `Item.ContentQuantity` = content per package.
- Purchase cost: `ceil(total_needed / ContentQuantity) * UnitPrice` (cheapest available supplier).
- Recipe cost: `RecipeService.ComputeRecipeCost(recipe)` — public static, reused by MealItemService + DailyMenuService.
- `RecipeIngredient.Unit` may differ from `PurchaseUnit`/`ContentUnit`.

---

## Architecture rules

- **Shared** = pure class library. Zero EF Core dependency.
- **Enums** in `Shared/Enums/` — single source of truth.
- **DTOs** in `Shared/DTOs/` — manual mapping only, no AutoMapper.
- **FluentValidation** in `Shared/Validators/`. ⚠️ Validators are NOT yet registered in `Server/Program.cs` — server-side validation is not enforced (KI-5).
- **No repository pattern** — services use `AppDbContext` directly.
- **No business logic** in Blazor components — all HTTP calls go through a service.
- **Tests**: SQLite in-memory only. EF InMemory provider is **forbidden** (doesn't enforce constraints).
- **On-demand DB records**: DailyMenu and Meal created lazily when the first MealItem is added.
- **Mapping**: `Server/Mapping/MealItemMapper.cs` — single `ToResponse(MealItem)` used by MealItemService, MealService, DailyMenuService. Do not inline mapping.

---

## Service signatures & patterns

| Case | Return type |
|------|-------------|
| Create, no FK to validate | `Task<T>` |
| Create, FK(s) to validate | `Task<T?>` |
| Create, ambiguous 404 vs 409 | `Task<ResultType>` (enum+record in Shared/DTOs) |
| Update | `Task<T?>` |
| Delete | `Task<bool>` |

**404 vs 409 pattern**: result type with `Error` enum. Controller switches on Error → HTTP status. Frontend switches on Error → snackbar. Reference: `ItemSupplierDtos.cs`.

**`SupplierName` display**: always use `CompanyName ?? Name` (not just `Name`).

**Cheapest supplier**: always `OrderBy(s => s.UnitPrice)` — never by SupplierId.

---

## Frontend slice map

All slices complete (DTO / Validator / Service / Controller / Tests + Client).

| Slice | Route | Notes |
|-------|-------|-------|
| Category | `/categories` | Reference Index pattern |
| Item | `/items` | FK CategoryId, PurchaseUnit+ContentQuantity+ContentUnit |
| Supplier | `/suppliers` | Party + CompanyName, Siret, PaymentType |
| Customer | `/customers` | Party + PaymentType → `/menuplan/{id}` |
| ItemSupplier | `/itemsuppliers` | Composite PK, 404/409 pattern |
| DailyMenu | `/menuplan/{CustomerId}` | Cards from `monthly-summary` |
| Meal+MealItem | `/dayplans?customerId=X&year=Y&month=M` | AddItemDialog, drag & drop |
| Recipe | `/recipes` | MudDataGrid + HierarchyColumn + RecipeDialog |

### Index page pattern (reference: `Category/Index.razor`)
- **Pending rows**: draft grid above main, "Add row" → `XxxDraft` (private sealed class in @code), Validate → `CreateAsync`.
- **Main grid**: `MudDataGrid` cell edit, `ValueChanged` + `_dirtyRows`, explicit Save per row.
- **Save All**: pending rows first (insertion order), then dirty rows. Snackbar summary.
- FK/Enum columns: `MudSelect` with `EditTemplate` + `ValueChanged`. Enums via `Enum.GetValues<T>()`.
- Composite PK: `_dirtyRows` is `HashSet<(int, int)>`.
- Grid alignment: `table-layout: fixed; width: 100%` + identical `width: X%` on columns.

### MudBlazor rules
- `MudIconButton` does NOT accept `Title` or `Tooltip` — wrap: `<MudTooltip Text="..."><MudIconButton .../></MudTooltip>`.

---

## Key component details

### MenuPlan/Index (`/menuplan/{CustomerId:int}`)
- 3-year card grid. Default: current month→Dec + N+1 + N+2 full years. Past toggle: 12 months back.
- Card click = navigate (280ms debounce for dbl-click). Dbl-click on card with data = enter copy mode.
- **Copy mode**: amber border on source, backdrop catches outside-click → `ExitCopyModeAsync`. JS Escape → `[JSInvokable] ExitCopyModeJs()`. Click target → `HandleTargetSelectedAsync`. If target HasMeals → `DuplicateMonthDialog` (overwrite warning).
- JS: `addEscapeHandler(dotNetRef)` / `removeEscapeHandler()` in `sortable-interop.js`.
- `DuplicateMonthAsync` → `POST /api/dailymenus/duplicate`. Full graph built in memory, single `SaveChangesAsync`.

### DayPlan/Index (`/dayplans?customerId&year&month`)
- CSS grid: 7 cols — Date(80px) + 5 MealTypes + Date-right(80px). `user-select:none`.
- Cell `<div>` carries `@key="@((capturedDate, capturedMt))"` — required for stable Blazor diffing.
- `OnParametersSetAsync` with `_lastKey` guard handles SPA month navigation without full dispose.
- Row/column primed highlight via JS `addRowPrimed`/`addColumnPrimed` → absolutely-positioned `.primed-axis-overlay`.
- Cost totals: `GetRowTotal`/`GetColumnTotal`/`_monthTotal` via `CostHelper.ComputeItemCost` + `ItemSupplierCache`.
- TR/CB breakdowns: `GetRowTrCb`/`GetColumnTrCb` → `(decimal tr, decimal cb)`. Uses `CostHelper.BucketByCost` + `GetRecipePaymentTypes`.

### MealCell (`Client/Components/MealCell.razor`)
- `ShouldRender()` override — compares `_renderedItems`, `_renderedMealId`, `_renderedIsBeingDragged`, `_renderedIsActionTarget`, `_renderedClearPrimed`, `_renderedPaymentTypes`. Snapshot in `OnAfterRenderAsync`.
- Fire-and-forget JS calls use `_ = JS.InvokeVoidAsync(...)`.
- Whole cell draggable; item dbl-click = delete; Ctrl+click = clone; click = open dialog.
- Slot total: dbl-click → `OnCellClearRequested`. Mouse hold → `_clearPrimed` visual feedback.

### AddItemDialog (`Client/Components/AddItemDialog.razor`)
- **Diff-based save**: `LoadSlot(CurrentMealItems)` on init. `ComputeDiff()` → `AddItemDialogSaveDiff(ToAddItems, ToAddRecipes, ToUpdate, ToDelete)`.
- Two tabs (Items/Recettes), single `_search` cleared on tab switch.
- `_filterByMealType` (default true): items tab filters by `CategoryAllowedMealTypes` flags.
- Dense view (default, persisted `localStorage` key `add-dialog-dense`): MudDataGrid. Card view: MudGrid.
- `MealRecap` sidebar (460px): bidirectional qty via `OnQtyChange`.
- `OnSave` is `Func<AddItemDialogSaveDiff, Task>` — applied in `ApplyDialogSaveAsync` (DayPlan/Index).

### AddItemToSlotAsync / AddRecipeToSlotAsync (DayPlan/Index)
- Both create DailyMenu → Meal → MealItem lazily on demand.
- All copy/clone/drag dispatch to correct helper based on `MealItemResponse.RecipeId.HasValue`.

### Cell drag-and-drop
- All moves immediate (no deferred save). `_saving` = full-screen overlay.
- Cell-level: footer drag → `HandleCellDragDropAsync`. Ctrl = copy. Same-cell Ctrl = copy-in-place.
- Item-level: SortableJS cross-cell → `HandleMoveItemAsync`. Same-slot reorder → `HandleOrderChanged`.
- `PATCH /mealitems/{id}/move` (cross-cell), `PATCH /mealitems/reorder` (same-slot).
- `MealItem.Order` 1-based, gap-free, auto-renumbered on move.

### Bulk clear / random fill
- `DELETE /api/meals/batch` → body `{ Ids: List<int> }`, always 204.
- `POST /api/meals/random-fill` → body `{ CustomerId, Year, Month, Mode }`, returns `List<DailyMenuResponse>`. Skips days with ≥1 meal. Pool built once before day loop.

---

## CostHelper & helpers

**`CostHelper` (`Client/Helpers/CostHelper.cs`)**:
- `Fr` — static `CultureInfo("fr-FR")` shared across all components.
- `PackageCost(totalQty, contentQty, unitPrice)` → `ceil(totalQty/contentQty) * unitPrice`.
- `ComputeItemCost(item, bestUnitPrice?)` — recipe: `EstimatedCost/BaseServings * Qty`; item: `PackageCost(...)`.
- `ComputePricePerKgL(unitPrice, contentQty, contentUnit)` → `"€/kg"` or `"€/L"` or null.
- `GetRecipePaymentTypes(ingredientIds, cache)` → distinct `PaymentType` list via cache.
- `BucketByCost(cost, paymentTypes)` → `(tr, cb)`: 0 types=(0,0); 1 type=full; mixed=50/50.

**`MealTypeHelper`**: `ToFrenchLabel(this MealType)` extension.

**`ItemSupplierCache`** (scoped): `EnsureLoadedAsync()` calls `GET /api/itemsuppliers/best-by-item` once. `GetBestSupplier(itemId)` → `BestSupplierInfo?`. Cache is NOT invalidated on ItemSupplier updates — prices/badges stale after supplier change until page reload.

---

## Shopping Cart

- `RightPanelState` — scoped service, global `MudDrawer` in MainLayout.
- `ShoppingCart.razor` fed by DayPlan/Index on every `LoadAsync()`. Parameter: `Items (IEnumerable<MealItemResponse>)`.
- Plain items: grouped by `(PaymentType, SupplierName)`, ordered by cheapest supplier.
- Recipe items: separate "Ingrédients recettes" section — one row per recipe, `RecipeEstimatedCost × Qty`.
- Footer: TR | CB left; grand total right. Aggregates both sections.
- `CartLine` record: cost = sum of `CostHelper.ComputeItemCost` per slot (ceil per slot, then sum). `PackageCount = ceil(TotalQty / ContentQty)`.

---

## Known issues

| # | Severity | Location | Description |
|---|----------|----------|-------------|
| KI-5 | ★★★ | `Server/Program.cs` | FluentValidation validators declared in Shared but NOT registered — server validation silently skipped |
| KI-6 | ★★★ | `Server/Services/RecipeIngredientService.cs:100` | `MapToResponse` orders suppliers by `SupplierId` instead of `UnitPrice` — wrong price returned for recipe ingredient |
| KI-7 | ★★ | `Server/Services/MealItemService.cs` MoveAsync | 4-5 sequential `SaveChangesAsync` calls — intermediate state visible; refactor to single transaction |
| KI-8 | ★★ | `Server/Services/ItemSupplierService.cs:154` | `MapToResponse` uses `Supplier.Name` not `CompanyName ?? Name` — SupplierName may be wrong in ItemSupplier list |

---

## Coding conventions

- All code in **English** (variables, functions, files, comments).
- One file per class/record/enum.
- Follow existing slice structure exactly — do not introduce new patterns without explicit approval.
- CC briefs from CW: **intention + constraints only** — no code in briefs.
- Check **Known Issues** before working on any affected component.
