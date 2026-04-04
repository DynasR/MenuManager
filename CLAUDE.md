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
MealSlotItem   — Id, Quantity(10,3), Notes, MealSlotId, ItemId

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

## Architecture rules (non-negotiable)

- **Shared** = pure class library. Zero EF Core dependency.
- **Enums** in `Shared/Enums/` — single source of truth for Client and Server.
- **DTOs** in `Shared/DTOs/` — manual mapping only, no AutoMapper.
- **FluentValidation** in `Shared/Validators/`.
- **No repository pattern** — services use `AppDbContext` directly.
- **No business logic** in Blazor components — all HTTP calls go through a service.
- **Tests**: SQLite in-memory only. EF InMemory provider is **forbidden** (doesn't enforce constraints).

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
- No navigation to a separate Create page — creation is exclusively via pending rows.

### Pending rows (two-phase commit UI)
- A **draft grid** (same columns and widths as the main grid) sits above the main grid.
- Each "Add row" click appends a typed `XxxDraft` instance to `_pendingRows` (local state, no Id).
- Each draft row has a **Validate** button and a **Cancel** button in the Actions column.
- `ValidateRow(draft)` → builds the request DTO → calls `Service.CreateAsync()`:
  - On success: remove from `_pendingRows`, reload main grid.
  - On failure (`null` or error): display a **snackbar error** — never fail silently.
- Draft class is a private `sealed class` defined in `@code`, not in `Shared/`.

### Main grid (inline edit)
- `MudDataGrid` with `EditMode="DataGridEditMode.Cell"`.
- `CommittedItemChanges` → calls `Service.UpdateAsync()` with the modified item.
- FK display columns: `MudSelect` with `EditTemplate` — **editable inline**.
- Enum columns: `MudSelect<TEnum>` with `EditTemplate` — **editable inline**.
- Business field columns: `EditTemplate` with `MudTextField`, `MudNumericField`, or `MudCheckBox`.
- Each row has an inline **Delete** button (`TemplateColumn` with `MudIconButton`).
- **Composite PK (ItemSupplier)**: extract `ItemId` + `SupplierId` from committed item
  to call `UpdateAsync(item.ItemId, item.SupplierId, dto)`.

### Deleted artifacts
- All `Create.razor` pages have been removed — creation is via pending rows only.
- All separate `Edit.razor` pages have been removed — editing is inline.

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

<MudSelect T="int" @bind-Value="draft.CategoryId" Label="Category">
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
<MudSelect T="MeasurementUnit" @bind-Value="draft.Unit" Label="Unit">
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

| Slice        | Service | Index (inline edit + pending rows) | Notes                                              |
|--------------|---------|-------------------------------------|----------------------------------------------------|
| Layout       | —       | —                                   | MainLayout, NavMenu, 4 MudBlazor providers         |
| Category     | ✅      | ✅                                  | ParentCategoryId editable inline (nullable select) |
| Item         | ✅      | ✅                                  | FK CategoryId, enum Unit, decimal PackageSize      |
| Supplier     | ✅      | ✅                                  | Party fields + CompanyName, Siret                  |
| Customer     | ✅      | ✅                                  | Party fields only, no password exposure            |
| ItemSupplier | ✅      | ✅                                  | Double FK dropdown, composite PK, snackbar 404/409 |

---

## Next steps

Frontend slices to build: **MenuPlan → DayPlan → MealSlot → MealSlotItem**
Apply the established Index pattern (pending rows + inline edit) on each.

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