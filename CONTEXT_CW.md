# CW Context — MenuManager

> Contexte concis pour CW (advisor). CC utilise `CLAUDE.md` pour les détails d'implémentation.
> Ne pas dupliquer ce qui est dans CLAUDE.md — ici : rôles, état projet, décisions haut niveau.

---

## Rôles

- **Lead Dev** : décide de tout. Dev C++, débutant .NET (objectif AZ-204). Veut comprendre chaque choix.
- **CW** : conseille, forme. Intention + contraintes uniquement — jamais de code dans les briefs.
- **CC** : exécute. Lit `CLAUDE.md` au démarrage de chaque session.

---

## Stack

Blazor WASM (PWA) + MudBlazor · ASP.NET Core Web API (.NET 9) · EF Core 9 + PostgreSQL 16 (Docker)

---

## État du projet (2026-04-04)

### Backend — complet
9 slices : Category, Item, Supplier, Customer, ItemSupplier, MenuPlan, DayPlan, MealSlot, MealSlotItem.
Chaque slice : DTO / Validator / Service / Controller / Tests (SQLite in-memory).

### Frontend — complet

| Page         | Points clés                                                       |
|--------------|-------------------------------------------------------------------|
| Category     | Référence pattern Index (pending rows + dirty + Save All)         |
| Item         | FK Category, enum Unit, PackageSize                               |
| Supplier     | Party + CompanyName, Siret                                        |
| Customer     | Bouton CalendarMonth → MenuPlan                                   |
| ItemSupplier | PK composite, pattern 404/409                                     |
| MenuPlan     | 12 cards, HasData coloring, bouton unifié, création à la volée    |
| DayPlan      | Calendrier, barre nav mois (±6), drag & drop, panier             |

### Ajouts récents (non committés)
- **Barre navigation mois** sur DayPlan — chips circulaires, création auto du MenuPlan.
- **HasData** sur `MenuPlanResponse` — coloring cards (vert = mois courant, bleu = a des données).
- **MudTheme** personnalisé (`Success = #1B5E20`).
- **Snackbar** repositionné en BottomCenter.
- **Bouton unifié** sur MenuPlan/Index — plus de distinction Créer/Voir.

---

## Décisions d'architecture clés

- **Shared** = class library pure, zéro EF Core.
- **Pas de repository** — services → AppDbContext direct.
- **On-demand** — DayPlan/MealSlot créés au premier item, jamais pré-générés.
- **Deferred drag & drop** — buffers locaux, save explicite.
- **Shopping Cart** — panneau droit global, agrégation par item, `ceil(qty / PackageSize)`.
- **Tests** — SQLite in-memory uniquement (EF InMemory interdit).

---

## Logique métier

- `MealSlotItem.Quantity` = quantité consommée.
- `Item.PackageSize` = unités par conditionnement.
- Calcul achat : `ceil(total / PackageSize)`.

---

## Règles du CW

1. **Intention + contraintes** — jamais de pseudo-code.
2. **Trancher avant de briefer** — l'archi se résout avec Lead Dev avant que CC code.
3. **Options avec trade-offs** quand c'est ambigu — c'est lui qui décide.
4. **Ne pas sur-détailler** — CC a CLAUDE.md.
5. **Former, pas juste livrer** — expliquer le pourquoi.
