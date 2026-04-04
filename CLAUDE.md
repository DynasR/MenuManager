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

---

## FK dropdown pattern (established on Item slice)

When a Create form references a FK, apply this pattern exactly:

```razor
@code {
    private List<CategoryResponse> _categories = new();

    protected override async Task OnInitializedAsync()
    {
        _categories = await CategoryService.GetAllAsync();
    }
}

<MudSelect T="int" @bind-Value="dto.CategoryId" Label="Category">
    @foreach (var cat in _categories)
    {
        <MudSelectItem Value="cat.Id">@cat.Name</MudSelectItem>
    }
</MudSelect>
```

Rules:
- `MudSelect<T>` type must match the DTO field type exactly (check `Shared/DTOs/` first — `int` vs `int?`).
- FK data loaded in `OnInitializedAsync()`, never in a button handler.
- The component holds only the list — no business logic.
- Both the entity and its FK dependencies are loaded in the **same** `OnInitializedAsync()` call (Edit page).

---

## Enum dropdown pattern (established on Item.Unit)

When a form field is bound to an enum, apply this pattern exactly:

```razor
<MudSelect T="MeasurementUnit" @bind-Value="dto.Unit" Label="Unit">
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

## Inline edit pattern (established on Category/Index)

All Index pages use `MudDataGrid` with inline cell editing. Reference implementation: `Category/Index.razor`.

```razor
<MudDataGrid T="CategoryResponse" Items="_items"
             EditMode="DataGridEditMode.Cell"
             CommittedItemChanges="OnCommit">
    <Columns>
        <PropertyColumn Property="x => x.Name">
            <EditTemplate>
                <MudTextField @bind-Value="context.Item.Name" />
            </EditTemplate>
        </PropertyColumn>
        <TemplateColumn>
            <CellTemplate>
                <MudIconButton Icon="@Icons.Material.Filled.Delete"
                               OnClick="() => OnDelete(context.Item)" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>
```

Rules:
- FK display columns (e.g. item name, category name): **no EditTemplate** — read-only.
- Enum columns: **no EditTemplate** — display as text only.
- Business field columns: EditTemplate with `MudTextField`, `MudNumericField`, or `MudCheckBox`.
- `CommittedItemChanges` receives the object **after modification** — call `Service.UpdateAsync()` here.
- The grid manages local state only — persistence is entirely the callback's responsibility.
- Each row has an inline Delete button (`TemplateColumn` with `MudIconButton`).
- "New" button at top navigates to `/slice/create` — Create page stays a separate form.
- **Composite PK (ItemSupplier)**: extract `ItemId` + `SupplierId` from the committed item
  to call `UpdateAsync(item.ItemId, item.SupplierId, dto)`.
- Separate Edit page is **dropped** for all slices using this pattern.

---

## Completed slices

### Backend (all complete: DTO / Validator / Service / Controller / Tests)
Category, Item, Supplier, Customer, ItemSupplier, MenuPlan, DayPlan, MealSlot, MealSlotItem

### Frontend (Client)

| Slice        | Service | Create | Index (inline edit) | Notes                                         |
|--------------|---------|--------|----------------------|-----------------------------------------------|
| Layout       | —       | —      | —                    | MainLayout, NavMenu, 4 MudBlazor providers    |
| Category     | ✅      | ✅     | ✅ MudDataGrid       | Pilot slice for inline edit pattern           |
| Item         | ✅      | ✅     | ✅ MudDataGrid       | FK dropdown CategoryId, enum Unit, PackageSize|
| Supplier     | ✅      | ✅     | 🔄 in progress       |                                               |
| Customer     | ✅      | ✅     | 🔄 in progress       | Party fields only, no password exposure       |
| ItemSupplier | ✅      | ✅     | ⬜ to do             | Double FK dropdown, composite PK route        |

---

## Next steps

1. Migrate Supplier/Index → MudDataGrid inline edit
2. Migrate Customer/Index → MudDataGrid inline edit
3. Build ItemSupplier/Index directly with MudDataGrid inline edit (composite PK pattern)

---

## Coding conventions

- All code in **English** (variables, functions, files, comments).
- One file per class/record/enum.
- Follow existing slice structure exactly — do not introduce new patterns without explicit approval from Lead Dev.

---

## How to use this file

1. Read it at the start of every session.
2. If a decision conflicts with a rule above → **stop and ask Lead Dev**.
3. After completing a task, remind Lead Dev to update this file if the architecture evolved.