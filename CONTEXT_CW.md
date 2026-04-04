# CW Context — MenuManager

## Rôles
- **Lead Dev / Architecte** : décide de tout.
- **CW (toi)** : conseille, explique, forme. Intention + contraintes uniquement — jamais de code dans les briefs.
- **CC (Claude Code)** : exécute. Reçoit les briefs du CW, lit CLAUDE.md au démarrage de chaque session.

## Profil Lead Dev
Développeur C++, débutant .NET. Objectif : expert .NET (certification AZ-204 visée).
Pilote des agents IA pour livrer vite, mais veut comprendre et défendre chaque décision technique.

## Stack
Blazor WASM (PWA) + MudBlazor · ASP.NET Core Web API (.NET 9) · EF Core 9 + PostgreSQL 16 (Docker) · xUnit + bUnit + FluentAssertions + Moq

```
/MenuManager
  /Client   → Blazor WASM
  /Server   → ASP.NET Core Web API
  /Shared   → Entités, DTOs, Validators, Enums (class library pure — zéro dépendance EF)
  /Tests    → xUnit + bUnit
  docker-compose.yml
```

## État du projet

### Backend — toutes les slices complètes
Category · Item · Supplier · Customer · ItemSupplier · MenuPlan · DayPlan · MealSlot · MealSlotItem
Chaque slice : DTO / Validator / Service / Controller / Tests (SQLite in-memory)

### Frontend — toutes les slices complètes
| Slice        | Notes                                                                  |
|--------------|------------------------------------------------------------------------|
| Layout       | MainLayout, NavMenu, RightPanelState, 4 providers MudBlazor            |
| Category     | Référence du pattern Index/Save                                        |
| Item         | FK CategoryId, enum MeasurementUnit, decimal PackageSize               |
| Supplier     | Party fields + CompanyName, Siret                                      |
| Customer     | Bouton CalendarMonth par ligne → `/menuplan/{id}`                      |
| ItemSupplier | PK composite, pattern 404/409                                          |
| MenuPlan     | 12 cards mensuelles, route `/menuplan/{CustomerId:int}`                |
| DayPlan      | Calendrier mensuel, dates FR, coloring weekend/fériés, drag & drop     |
| MealSlot     | Pas de page — logique embarquée dans DayPlan/Index                     |
| MealSlotItem | Pas de page — logique embarquée dans DayPlan/Index                     |
| ShoppingCart | Panneau droit global (RightPanelState), agrégation items + prix        |

### Migrations appliquées
| Nom                            | Description                                    |
|--------------------------------|------------------------------------------------|
| InitialCreate                  | Schéma initial complet                         |
| RefactorItemUnitAndPackageSize | Unit → enum, PackageSize ajouté                |
| AddMealSlotItemOrder           | Order int default 0 sur MealSlotItem           |

## Décisions d'architecture clés

- **Shared** = class library pure, zéro EF Core.
- **Toute config EF** dans `AppDbContext.OnModelCreating()` Fluent API.
- **Pas de repository pattern** — les services utilisent AppDbContext directement.
- **Pas d'AutoMapper** — mapping manuel dans les services.
- **Tests SQLite in-memory uniquement** — EF InMemory interdit (n'applique pas les contraintes).
- **On-demand** — DayPlan et MealSlot créés uniquement au premier MealSlotItem. Jamais pré-générés.
- **Pattern Index** établi et harmonisé sur toutes les slices (référence : Category/Index.razor) — pending rows + dirty tracking + Save All.
- **Enums dans Shared/Enums/** — source de vérité unique Client + Server.
- **Drag & drop** — SortableJS via JS interop (IIFE + registry). Deux opérations :
  - **Move cross-cell** : `PATCH /mealslotitem/{id}/move` — renumérote source et target slots.
  - **Reorder same-slot** : `PATCH /mealslotitems/reorder` — `ReorderMealSlotItemsRequest(MealSlotId, OrderedItemIds)`.
  - **Deferred save** : les drags ne sont pas envoyés immédiatement. Buffers locaux (`_pendingMoves`, `_pendingReorders`) + bouton Save All.
  - Snackbar sur erreur uniquement pendant Save All, résumé ("X moved, Y reordered") sur succès.
- **Order auto-séquentiel** — `CreateAsync` assigne `Order = count + 1`. `MoveAsync` renumérote les deux slots (gap-free, 1-based).
- **Jours fériés FR** — calculés côté client dans `Client/Helpers/FrenchHolidays.cs` (algorithme de Pâques). Zéro appel API externe.
- **Shopping Cart** — panneau droit global via `RightPanelState` (service scoped). `ShoppingCart.razor` agrège les items par `ItemId`, calcule `ceil(qty / PackageSize)` paquets à acheter, affiche prix unitaire (1er fournisseur dispo) et total EUR.
  - `MealSlotItemResponse` enrichi : `UnitPrice` (nullable), `PackageSize`, `Unit` — tous les services incluent `ItemSuppliers` dans les queries.
  - DayPlan/Index pousse le contenu dans le panneau droit ; dispose à la sortie de page.
- **MudIconButton** n'accepte pas Title/Tooltip → toujours envelopper dans `<MudTooltip>` (règle MUD0002).

## Signatures de service
| Cas                           | Signature        |
|-------------------------------|------------------|
| Create sans FK à valider      | Task\<T\>        |
| Create avec FK à valider      | Task\<T?\>       |
| Create ambiguïté 404/409      | Task\<ResultType\> |
| Update                        | Task\<T?\>       |
| Delete                        | Task\<bool\>     |

## Logique métier clé
- `MealSlotItem.Quantity` = quantité consommée · `Item.PackageSize` = unités par conditionnement
- Calcul achat : `ceil(total_needed / PackageSize)`
- Migration future vers les grammes : ajouter `UnitWeightG` nullable — zéro breaking change.

## Config réseau dev
- Server : `http://localhost:5075`
- Client : lit `ServerUrl` depuis `Client/wwwroot/appsettings.json`
- Docker : PostgreSQL sur port 5432, DB `menumanager`, user `admin`

## Règles du CW
1. **Intention + contraintes** dans les briefs — jamais de pseudo-code.
2. **Trancher avant de briefer** — toute question d'architecture se résout avec le Lead Dev avant que CC touche au code.
3. **Options avec trade-offs** quand c'est ambigu — c'est lui qui décide.
4. **Ne pas sur-détailler** — CC a le contexte via CLAUDE.md, il n'a pas besoin d'être guidé pas à pas.
5. **Former, pas juste livrer** — expliquer le pourquoi pour que le Lead Dev puisse défendre ses choix.