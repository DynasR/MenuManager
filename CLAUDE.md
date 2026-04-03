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
  /Shared   → Entities, DTOs, Validators (pure class library — NO EF dependency)
  /Tests    → xUnit + bUnit + FluentAssertions + Moq
  docker-compose.yml
```

---

## Network (dev)

| Service    | URL / Port              |
|------------|-------------------------|
| API Server | http://localhost:5075   |
| PostgreSQL | localhost:5432          |
| DB name    | menumanager             |
| DB user    | admin                   |

Client reads `ServerUrl` from `Client/wwwroot/appsettings.json`.

---

## Domain model (Shared/Entities/)

```
Party (abstract, TPT)
  ├── Customer   — PasswordHash, PasswordSalt, ICollection<MenuPlan>
  └── Supplier   — CompanyName, Siret, ICollection<ItemSupplier>

Category       — Id, Name, Description, ParentCategoryId (self-ref), SubCategories, Items
Item           — Id, Name, Description, Quantity, Unit, CategoryId, CreatedAt, UpdatedAt
ItemSupplier   — PK composite (ItemId+SupplierId), UnitPrice(10,2), SupplierRef, IsAvailable, UpdatedAt
MenuPlan       — Id, Name, Month, Year, CustomerId, CreatedAt, ICollection<DayPlan>
DayPlan        — Id, Date(DateOnly), MenuPlanId, ICollection<MealSlot>
MealSlot       — Id, MealType, DayPlanId, unique(DayPlanId+MealType), ICollection<MealSlotItem>
MealSlotItem   — Id, Quantity(10,3), Notes, MealSlotId, ItemId

MealType (enum) — Breakfast, MorningSnack, Lunch, AfternoonSnack, Dinner
```

All EF config (precision, indexes, TPT, composite PKs) lives **exclusively** in `AppDbContext.OnModelCreating()` Fluent API.

---

## Architecture rules (non-negotiable)

- **Shared** = pure class library. Zero EF Core dependency.
- **DTOs** in `Shared/DTOs/` — manual mapping only, no AutoMapper.
- **FluentValidation** in `Shared/Validators/`.
- **No repository pattern** — services use `AppDbContext` directly.
- **No business logic** in Blazor components.
- **Tests**: SQLite in-memory only. EF InMemory provider is **forbidden** (doesn't enforce constraints).

---

## Service method signatures

| Case                                    | Return type         |
|-----------------------------------------|---------------------|
| Create, no FK to validate               | `Task<T>`           |
| Create, FK(s) to validate               | `Task<T?>`          |
| Create, ambiguous 404 vs 409            | `Task<ResultType>`  |
| Update (entity may not exist)           | `Task<T?>`          |
| Delete                                  | `Task<bool>`        |

### 404 vs 409 pattern (ItemSupplier model)

When `CreateAsync` can fail for multiple distinct reasons, use a result type in `Shared/DTOs/`:

```csharp
public enum CreateItemSupplierError { ItemNotFound, SupplierNotFound, AlreadyExists }
public record CreateItemSupplierResult(ItemSupplierResponse? Response, CreateItemSupplierError? Error);
```

Controller switches on `Error` to return the correct HTTP status.

---

## Completed slices

### Backend (all complete: DTO / Validator / Service / Controller / Tests)
- Category, Item, Supplier, Customer, ItemSupplier
- MenuPlan, DayPlan, MealSlot, MealSlotItem

### Frontend Client
- Layout: MainLayout, NavMenu, 4 MudBlazor providers
- HttpClient: configured via `appsettings.json`
- **Category**: CategoryService + CRUD pages (Index, Create, Edit) ✅

---

## Current task

**Item slice — frontend.**
Pattern identical to Category.
`Client/Services/ItemService.cs` + `Client/Pages/Items/` (Index, Create, Edit).
⚠️ The Create/Edit form must include a **Category dropdown** populated from `GET /api/categories`.

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
