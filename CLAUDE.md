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

When `CreateAsync` can fail for multiple distinct reasons, use a result type in `Shared/DTOs/`:

```csharp
public enum CreateItemSupplierError { ItemNotFound, SupplierNotFound, AlreadyExists }
public record CreateItemSupplierResult(ItemSupplierResponse? Response, CreateItemSupplierError? Error);
```

Controller switches on `Error` to return the correct HTTP status.
Frontend switches on `Error` to display the correct snackbar message.

---

## Index page pattern (established and harmonized on all slices)

All Index pages follow this exact pattern. Reference implementation: `Category/Index.razor`.

### Toolbar
- **"Add row" button** — always visible, no toggle, no "New" button, no Edition Mode switch.
- **"Save All" button** — single button handling both pending rows and dirty rows (see below).
- No navigation to a separate Create page — creation is exclusively via pending rows.

### Pending rows (two-phase commit UI)
- A **draft grid** (same columns and widths as the main grid) sits above the main grid.
- Each "Add row" click appends a typed `XxxDraft` instance to `_pendingRows` (local state, no Id).
- Each draft row has a **Validate** button (disabled when `Name` is empty or whitespace)
  and a **Cancel** button in the Actions column.
- `ValidateRow(draft)` → builds the request DTO → calls `Service.CreateAsync()`:
  - On success: remove from `_pendingRows`, reload main grid.
  - On failure (`null` or error): display a **snackbar error** — never fail silently.
- Draft class is a private `sealed class` defined in `@code`, not in `Shared/`.

### Main grid (inline edit)
- `MudDataGrid` with `EditMode="DataGridEditMode.Cell"`.
- Editable columns use `EditTemplate` with `ValueChanged` (not `@bind-Value`) to intercept
  changes and mark the row dirty:
  `ValueChanged="@((T val) => { context.Item.Field = val; _dirtyRows.Add(context.Item.Id); StateHasChanged(); })"`
- `_dirtyRows` is a `HashSet<int>` declared in `@code`. Cleared in `LoadAsync()` after reload.
- Each row has an inline **Save** button (disquette icon),
  disabled when `!_dirtyRows.Contains(context.Item.Id)`.
- Each row has an inline **Delete** button.
- **No** `CommittedItemChanges` — saving is always explicit, never automatic.
- FK columns: `MudSelect` with `EditTemplate` + `ValueChanged` — editable inline.
- Enum columns: `MudSelect<TEnum>` with `EditTemplate` + `ValueChanged` — editable inline.
- **Composite PK (ItemSupplier)**: `_dirtyRows` stores a `HashSet<(int, int)>` instead of `HashSet<int>`.

### Save All (toolbar)
- A single **Save All** button handles both pending rows and dirty rows.
- Disabled when `_pendingRows.Count == 0 && _dirtyRows.Count == 0`.
- Execution order is strict and non-negotiable:
  1. Create all pending rows sequentially (insertion order).
  2. Update all dirty rows.
- Each operation is non-blocking: on failure → snackbar error + continue.
- After the loop: if at least one success → reload full list + snackbar summary
  ("X created, Y updated", zeros omitted).

### Deleted artifacts
- All `Create.razor` pages have been removed — creation is via pending rows only.
- All separate `Edit.razor` pages have been removed — editing is inline.

---

## MudBlazor component rules

- `MudIconButton` does **not** accept `Title` or `Tooltip` attributes — compiler warning MUD0002.
- To show a tooltip on an icon button, wrap it: `<MudTooltip Text="..."><MudIconButton .../></MudTooltip>`.
- Never use `Title` or `Tooltip` directly on `MudIconButton`.

---

## Column width alignment rule

When two `MudDataGrid` instances are stacked (main grid + pending rows grid), their columns
must be visually aligned. The only reliable approach:

1. Add `Style="table-layout: fixed; width: 100%;"` on **both** `<MudDataGrid>` tags.
2. Apply **strictly identical** `Style="width: X%"` on each `<PropertyColumn>` / `<TemplateColumn>`
   in both grids.

Percentage values must sum to 100%. Choose values proportional to expected content width.

---

## FK dropdown pattern

```razor
@code {
    private List<CategoryResponse> _categories = new();

    protected override async Task OnInitializedAsync()
    {
        _categories = await CategoryService.GetAllAsync();
    }
}

// In pending rows (draft): @bind-Value is fine — no dirty tracking needed
<MudSelect T="int" @bind-Value="draft.CategoryId" Label="Category">
    @foreach (var cat in _categories)
    {
        <MudSelectItem Value="cat.Id">@cat.Name</MudSelectItem>
    }
</MudSelect>

// In main grid (EditTemplate): use ValueChanged to mark row dirty
<MudSelect T="int"
           Value="context.Item.CategoryId"
           ValueChanged="@((int val) => { context.Item.CategoryId = val; _dirtyRows.Add(context.Item.Id); StateHasChanged(); })">
    @foreach (var cat in _categories)
    {
        <MudSelectItem Value="cat.Id">@cat.Name</MudSelectItem>
    }
</MudSelect>
```

Rules:
- `MudSelect<T>` type must match the DTO field type exactly (`int` vs `int?`).
- For nullable FK (e.g. `ParentCategoryId`): add a "— None —" option with `null` value at the top.
- FK data loaded in `OnInitializedAsync()`, never in a button handler.
- No business logic in the component.

---

## Enum dropdown pattern

```razor
// In pending rows (draft): @bind-Value
<MudSelect T="MeasurementUnit" @bind-Value="draft.Unit" Label="Unit">
    @foreach (var u in Enum.GetValues<MeasurementUnit>())
    {
        <MudSelectItem Value="u">@u</MudSelectItem>
    }
</MudSelect>

// In main grid (EditTemplate): ValueChanged to mark row dirty
<MudSelect T="MeasurementUnit"
           Value="context.Item.Unit"
           ValueChanged="@((MeasurementUnit val) => { context.Item.Unit = val; _dirtyRows.Add(context.Item.Id); StateHasChanged(); })">
    @foreach (var u in Enum.GetValues<MeasurementUnit>())
    {
        <MudSelectItem Value="u">@u</MudSelectItem>
    }
</MudSelect>
```

Rules:
- No hardcoded list — `Enum.GetValues<T>()` is the single source of truth.
- The enum lives in `Shared/Enums/` — automatically available to both Client and Server.

---

## Completed slices

### Backend (all complete: DTO / Validator / Service / Controller / Tests)
Category, Item, Supplier, Customer, ItemSupplier, MenuPlan, DayPlan, MealSlot, MealSlotItem

### Frontend (Client)

| Slice        | Service | Index           | Notes                                                              |
|--------------|---------|-----------------|--------------------------------------------------------------------|
| Layout       | —       | —               | MainLayout, NavMenu, 4 MudBlazor providers                         |
| Category     | ✅      | ✅ patched      | Reference implementation — new Save pattern applied                |
| Item         | ✅      | ✅ patched      | FK CategoryId, enum Unit, decimal PackageSize                      |
| Supplier     | ✅      | ✅ patched      | Party fields + CompanyName, Siret                                  |
| Customer     | ✅      | ✅ patched      | Party fields only — CalendarMonth button → `/menuplan/{id}`        |
| ItemSupplier | ✅      | ✅ patched      | Double FK dropdown, composite PK, snackbar 404/409                 |
| MenuPlan     | ✅      | ✅ cards        | 12-month card grid, route `/menuplan/{CustomerId:int}` (see below) |
| DayPlan      | ✅      | ✅ calendar     | FR dates, weekend/holiday coloring, drag & drop (see below)        |
| MealSlot     | ✅      | ❌ deleted      | Logic embedded in DayPlan/Index                                    |
| MealSlotItem | ✅      | ❌ deleted      | Logic embedded in DayPlan/Index                                    |

---

## MenuPlan/Index — 12-month card grid

Route: `/menuplan/{CustomerId:int}` — always scoped to a customer.
Navigation entry point: `Customer/Index` — CalendarMonth icon button per row.

- No MudDataGrid, no pending rows, no dirty tracking.
- 12 slots generated **client-side** from current month + 11 months ahead.
- Each slot is a `MudCard` displaying month title (FR uppercase) + start/end dates (FR long format).
- All date formatting uses `CultureInfo("fr-FR")`.
- Matching with DB: `_menuPlans.FirstOrDefault(p => p.Month == month && p.Year == year && p.CustomerId == CustomerId)`.
- Card without MenuPlan → "+ Créer" button → `CreateAsync` then navigate to `/dayplan/{id}`.
- Card with MenuPlan → "Voir le planning" button → navigate to `/dayplan/{id}`.
- Created plan name: `$"Planning {month:D2}/{year}"`.

---

## DayPlan/Index — monthly calendar view

### Layout
- CSS grid: 7 columns — Date + 5 MealTypes (Breakfast, MorningSnack, Lunch, AfternoonSnack, Dinner).
- One row per day of the month, generated client-side from MenuPlan.Month / MenuPlan.Year.

### French dates
- All day labels use `CultureInfo("fr-FR")` — short consistent format.

### Day coloring (priority order: holiday > weekend > normal)
- Helper `Client/Helpers/FrenchHolidays.cs` — static method `GetHolidays(int year)`
  returning `HashSet<DateOnly>` of all French public holidays (fixed + mobile via Easter algorithm).
- Colors use MudBlazor CSS variables (`--mud-palette-*`) — no hardcoded hex values.
- Weekend (Saturday/Sunday): light distinct tint.
- Public holiday: stronger tint + discrete visual indicator (tooltip or label).

### MealCell component (`Client/Components/MealCell.razor`)
- One cell per `(DateOnly date, MealType mealType)`.
- Displays ordered list of `MealSlotItem` for that slot (`Order` field respected).
- "+" button opens the MudDrawer (add item).
- Delete button per item.
- Each item element is draggable (SortableJS) — carries `data-item-id="{mealSlotItem.Id}"`, uses `@key="item.Id"`.
- The list container carries `data-date` and `data-mealtype` attributes.
- Parameters: `OnItemMoved` (cross-cell move), `OnItemRemoved` (delete), `OnAddRequested` (drawer), `OnOrderChanged` (same-slot reorder).
- `[JSInvokable] OnReorder(int[] orderedIds)` — called by JS on same-list sort, raises `OnOrderChanged`.

### MudDrawer (right anchor)
- Opens when user clicks "+" in any MealCell.
- Context: selected date + selected MealType displayed in drawer header.
- Real-time item search — client-side filter on `_allItems`.
- Clicking an item triggers the on-demand AddItemAsync flow.
- Width: 320px, Variant: Temporary.

### AddItemAsync logic (sequential, on-demand)
1. If no DayPlan exists for selected date → `CreateAsync` → store in `_dayPlanByDate`.
2. If no MealSlot exists for `(DayPlanId, MealType)` → `CreateAsync`.
3. `CreateAsync` MealSlotItem (Quantity default = 1, Order = current slot item count).
4. Silent success + `LoadAsync()`.

---

## Drag & drop (fully implemented)

### Backend
- `MealSlotItem.Order int default 0` — migration `AddMealSlotItemOrder` applied.
- `CreateAsync` auto-assigns `Order = currentCount + 1` (sequential).
- `MoveMealSlotItemRequest` in `Shared/DTOs/` — fields: `TargetDate`, `TargetMealType`, `NewOrder`.
  Backend resolves the target MealSlot itself (on-demand pattern) — no `TargetMealSlotId` exposed.
- `MealSlotItemService.MoveAsync(int id, MoveMealSlotItemRequest)`:
  - Loads item + current slot → 404 if missing.
  - Resolves target MealSlot by `(DayPlanId, TargetMealType)` — creates it if absent.
  - Moves item to target slot, then **renumbers both source and target slots** (gap-free 1-based Order).
  - Inserts at `NewOrder` position in target slot via `Math.Clamp`.
- `PATCH /mealslotitem/{id}/move` — returns 200 or 404.
- `ReorderMealSlotItemsRequest` in `Shared/DTOs/` — fields: `MealSlotId`, `OrderedItemIds` (List<int>).
- `MealSlotItemService.ReorderAsync(ReorderMealSlotItemsRequest)`:
  - Validates all IDs belong to the slot and list is complete (count match).
  - Assigns 1-based Order per provided sequence.
  - Returns `false` on empty slot, partial list, or unknown ID.
- `PATCH /mealslotitems/reorder` — returns 204 or 404.
- `MealSlotItemResponse` includes `Order` field (exposed to frontend).
- Tests: reorder (correct, unknown ID, empty, partial), sequential Order on create, renumber source/target on move, insert position.

### Frontend — deferred save pattern
- Drag & drop operations are **not** sent to the backend immediately.
- `_pendingMoves` (List<PendingMove>) — cross-cell moves buffered locally.
- `_pendingReorders` (Dictionary<int, List<int>>) — same-slot reorders buffered by MealSlotId.
- `HandleMoveItem` updates local `_dayPlanByDate` state (remove from source, insert in target) so Blazor stays in sync with SortableJS DOM — no `LoadAsync()` until Save.
- `HandleOrderChanged` stores the new order in `_pendingReorders`.
- **Save All toolbar button** — disabled when both buffers empty. Processes moves first, then reorders. Snackbar summary on completion ("X moved, Y reordered"). Then `LoadAsync()`.

### JS interop (`sortable-interop.js`)
- IIFE with `Map` registry (element → `{ sortable, moveHandler, dotNetRef }`).
- `initSortable(element, group)` — creates SortableJS instance, fires `sortcomplete` custom event on `onEnd`.
- `observeSortable(element, dotNetRef)` — listens to `sortcomplete`, distinguishes cross-cell (→ `OnDrop`) vs same-list (→ `OnReorder`).
- `destroySortable(element)` — cleans up instance + listeners + ref.

### Snackbar rule for drag & drop
- **Error only during Save All** — snackbar per failed operation.
- **Summary on success** — "X moved, Y reordered" (zeros omitted).
- **No snackbar on individual drag** — visual DOM feedback is sufficient.

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