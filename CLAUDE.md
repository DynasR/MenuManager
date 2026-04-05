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
  ├── Customer   — PasswordHash, PasswordSalt, ICollection<MenuPlan>
  └── Supplier   — CompanyName, Siret, ICollection<ItemSupplier>

Category       — Id, Name, Description, ParentCategoryId (self-ref), SubCategories, Items
Item           — Id, Name, Description, Unit(MeasurementUnit), PackageSize(decimal, default=1),
                 CategoryId(int), CreatedAt, UpdatedAt, ItemSuppliers, MealSlotItems
ItemSupplier   — PK composite (ItemId+SupplierId), UnitPrice(10,2), SupplierRef, IsAvailable, UpdatedAt
MenuPlan       — Id, Name, Month, Year, CustomerId, CreatedAt, ICollection<DayPlan>
DayPlan        — Id, Date(DateOnly), MenuPlanId, ICollection<MealSlot>
MealSlot       — Id, MealType, DayPlanId, unique(DayPlanId+MealType), ICollection<MealSlotItem>
MealSlotItem   — Id, Quantity(10,3), Notes, Order(int, default=0), MealSlotId, ItemId

MealType        (enum, Shared/Enums/) — Breakfast, MorningSnack, Lunch, AfternoonSnack, Dinner
MeasurementUnit (enum, Shared/Enums/) — Piece, Gram, Kilogram, Milliliter, Liter
```

All EF config (precision, indexes, TPT, composite PKs) lives **exclusively** in
`AppDbContext.OnModelCreating()` Fluent API.

### PackageSize business rule
- `MealSlotItem.Quantity` = quantity consumed in the recipe (e.g. 1 ice cream)
- `Item.PackageSize` = units per package (e.g. 6)
- Purchase calculation: `ceil(total_needed / PackageSize)`
- Future migration to weight: add nullable `UnitWeightG` — zero breaking change.

---

## Migrations applied

| Migration name                   | Description                                      |
|----------------------------------|--------------------------------------------------|
| InitialCreate                    | Full initial schema                              |
| RefactorItemUnitAndPackageSize   | Unit → enum MeasurementUnit, PackageSize added   |
| AddMealSlotItemOrder             | Order int default 0 added on MealSlotItem        |

---

## Architecture rules (non-negotiable)

- **Shared** = pure class library. Zero EF Core dependency.
- **Enums** in `Shared/Enums/` — single source of truth for Client and Server.
- **DTOs** in `Shared/DTOs/` — manual mapping only, no AutoMapper.
- **FluentValidation** in `Shared/Validators/`.
- **No repository pattern** — services use `AppDbContext` directly.
- **No business logic** in Blazor components — all HTTP calls go through a service.
- **Tests**: SQLite in-memory only. EF InMemory provider is **forbidden** (doesn't enforce constraints).
- **On-demand DB records**: DayPlan and MealSlot records are never pre-generated.
  They are created lazily when the first MealSlotItem is added to a day/slot.
  MenuPlan.CreateAsync persists only the MenuPlan — no child generation.

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
Category, Item, Supplier, Customer, ItemSupplier, MenuPlan, DayPlan, MealSlot, MealSlotItem

### Frontend (Client)

| Slice        | Service | Index           | Notes                                                              |
|--------------|---------|-----------------|--------------------------------------------------------------------|
| Layout       | —       | —               | MudTheme (Success=#1B5E20, Secondary=#7C3AED, Info=#1565C0), gradient AppBar, NavMenu split (main top / admin bottom), RightPanelState |
| Category     | ✅      | ✅ patched      | Reference implementation — new Save pattern applied                |
| Item         | ✅      | ✅ patched      | FK CategoryId, enum Unit, decimal PackageSize                      |
| Supplier     | ✅      | ✅ patched      | Party fields + CompanyName, Siret                                  |
| Customer     | ✅      | ✅ patched      | Party fields only — CalendarMonth button → `/menuplan/{id}`        |
| ItemSupplier | ✅      | ✅ patched      | Double FK dropdown, composite PK, snackbar 404/409                 |
| MenuPlan     | ✅      | ✅ cards        | 3-year cards, HasData coloring, MonthlyCost, unified button        |
| DayPlan      | ✅      | ✅ calendar     | Month nav bar, SortableJS reorder/move/copy (immediate), shopping cart, cell-drag copy/move, Ctrl+click clone item, click item=add drawer, dbl-click item=delete, dbl-click total=clear cell, saving overlay, 7-col grid |
| MealSlot     | ✅      | ❌ deleted      | Logic embedded in DayPlan/Index                                    |
| MealSlotItem | ✅      | ❌ deleted      | Logic embedded in DayPlan/Index                                    |

---

## MenuPlan/Index — 3-year card grid

Route: `/menuplan/{CustomerId:int}` — always scoped to a customer.
Navigation entry point: `Customer/Index` — CalendarMonth icon button per row.

- 3 years of cards grouped by year: current month → Dec, then full N+1 and N+2.
- Unified "Voir le planning" button — creates MenuPlan on-the-fly if absent.
- **HasData coloring**: current month = green (`#1B5E20`), has data = filled blue, empty = outlined.
- **MonthlyCost**: computed server-side (`ceil(qty / PackageSize) * cheapest available UnitPrice`), displayed on each card (EUR, FR locale).
- `MenuPlanResponse.HasData` / `MonthlyCost` — computed in `MenuPlanService.MapToResponse`.

---

## DayPlan/Index — monthly calendar view

### Layout
- CSS grid: 7 columns — Date (80px) + 5 MealTypes + Date-right (80px). Wrapped in `.dayplan-grid-wrapper` (flex, `user-select: none`).
- One row per day of the month, FR locale, weekend/holiday coloring.
- Date labels: `.date-label` flex column — DOW abbreviation (small, uppercase) + day number (large). Classes: `date-label-weekend` (opacity), `date-label-holiday` (warning color).
- `FrenchHolidays.cs` helper for public holidays (fixed + Easter-based).

### Month navigation bar
- Horizontal strip of ±6 circular chips above the calendar (13 months total).
- Current month = green filled (`#1B5E20`), has data = blue filled, empty = outlined.
- Click navigates to that month's DayPlan — creates MenuPlan on-the-fly if absent.
- Uses `OnParametersSetAsync` (not `OnInitializedAsync`) for SPA re-navigation.
- `_siblingPlans` loaded via `MenuPlanSvc.GetByCustomerAsync` to detect existing plans.

### MealCell component (`Client/Components/MealCell.razor`)
- One cell per `(DateOnly date, MealType mealType)`.
- Ordered list of items (SortableJS drag & drop for reorder within/across slots).
- `meal-cell-empty` class when no items; `.meal-cell:not(.meal-cell-empty)` gets subtle green tint.
- Displays price per item + slot total (EUR, FR locale, `ceil(qty/PackageSize)*UnitPrice`).
- **Whole cell is `draggable="true"`** — drag cell anywhere to move; Ctrl held = copy. Footer has two `.footer-zone-side` draggable divs flanking the slot total.
- **Slot total**: double-click = clear all items (`OnCellClearRequested`). `_clearPrimed` state: mousedown turns total red (`total-primed` CSS) to confirm intent.
- **Item interactions**: click item = open add drawer; Ctrl+click = clone in same slot (`OnItemCloneRequested`); double-click = delete (`OnItemRemoved`).
- **Click anywhere on cell** (non-item area) = open add drawer.
- **Hover states**: `.meal-cell:hover` subtle tint; `.meal-cell:not(.meal-cell-empty):hover` green glow; `.meal-cell-item:hover` blue wash.
- **Parameters**: `IsActionTarget` (bool — set externally when cell is a drop target); `IsBeingDragged` (bool — set externally when this cell is the drag source → `cell-drag-source` CSS, items show dashed border).
- **CSS visual states**: `meal-cell-drag-copy`, `meal-cell-drag-move` on target cell during drag; `cell-drag-source` on source cell; `cell-drag-copy-mode` on source cell when Ctrl held.
- **Callbacks**: `OnItemMoved(itemId, fromDate, fromMealType, toDate, toMealType, newIndex, isCopy)`, `OnItemRemoved`, `OnAddRequested`, `OnOrderChanged`, `OnCellFooterDrop`, `OnItemCloneRequested`, `OnDragStarted(date, mealType)`, `OnDragEnded`, `OnCellClearRequested`.

### AddItemToSlotAsync — on-demand creation (shared helper)
- Private `Task<bool> AddItemToSlotAsync(date, mealType, itemId, quantity)`.
- DayPlan → MealSlot → MealSlotItem created lazily. Returns `false` on any API failure.
- Called by: AddItemAsync (drawer), ExecuteCellActionAsync, CloneItemAsync.

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

---

## Drag & drop (fully implemented)

- SortableJS via JS interop (`sortable-interop.js`).
- **Immediate save**: all moves and reorders call the API directly; no buffering, no Save All.
- SortableJS cross-cell drag supports Ctrl at drop time = copy (item added to target, source untouched).
- Backend: `PATCH /mealslotitem/{id}/move` (cross-cell), `PATCH /mealslotitems/reorder` (same-slot).
- `MealSlotItem.Order` — 1-based, gap-free, auto-renumbered on move.
- `MealSlotItemResponse` includes `Order`, `UnitPrice`, `PackageSize`, `Unit`.

---

## Shopping Cart (right panel)

- `RightPanelState` — scoped service, global `MudDrawer` in `MainLayout`. Any page can push content.
- `ShoppingCart.razor` — aggregates items by `ItemId`, computes packages to buy (`ceil(qty / PackageSize)`), line totals, grand total (EUR, FR locale).
- Fed by DayPlan/Index on every `LoadAsync()`. Disposed on page leave.

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