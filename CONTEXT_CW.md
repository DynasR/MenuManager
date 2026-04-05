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

## État du projet (2026-04-05)

### Backend — complet
9 slices : Category, Item, Supplier, Customer, ItemSupplier, MenuPlan, DayPlan, MealSlot, MealSlotItem.
Chaque slice : DTO / Validator / Service / Controller / Tests (SQLite in-memory).

### Frontend — complet

| Page         | Points clés                                                                      |
|--------------|----------------------------------------------------------------------------------|
| Category     | Référence pattern Index (pending rows + dirty + Save All)                        |
| Item         | FK Category, enum Unit, PackageSize                                              |
| Supplier     | Party + CompanyName, Siret                                                       |
| Customer     | Bouton CalendarMonth → MenuPlan                                                  |
| ItemSupplier | PK composite, pattern 404/409                                                    |
| MenuPlan     | 3 ans de cards groupées par année, HasData, MonthlyCost, bouton unifié           |
| DayPlan      | Calendrier, barre nav mois (±6), SortableJS reorder/move, panier, **footer-drag copy/move cellule** (Ctrl=copie, défaut=déplace), **Ctrl+clic clone item** (même slot), **dbl-clic item=suppr**, **dbl-clic total=vider cellule**, trash-zone sur colonnes date/header pendant drag, grille 7 colonnes |
| Layout       | Thème: Success=#1B5E20, Secondary=#7C3AED, Info=#1565C0, AppBar dégradé bleu-violet, NavMenu splitté (principal haut / admin bas) |

---

## Décisions d'architecture clés

Voir `CLAUDE.md` pour les détails. Résumé :
- **Shared** pur (zéro EF), pas de repository, on-demand DayPlan/MealSlot.
- **Deferred drag & drop**, **Shopping Cart** (panneau droit global).
- **MonthlyCost** calculé serveur-side (`ceil(qty / PackageSize) * UnitPrice`), affiché sur les cards MenuPlan et par item/cellule dans MealCell.
- **Copy/Move cellule** — HTML5 drag-and-drop natif (footer draggable). Ctrl tenu = copie, défaut = déplace. Drop sur trash-zone = clear. Plus de `CellPendingAction`, plus de boutons par item.
- **Tests** — SQLite in-memory uniquement.

---

## Règles du CW

1. **Intention + contraintes** — jamais de pseudo-code.
2. **Trancher avant de briefer** — l'archi se résout avec Lead Dev avant que CC code.
3. **Options avec trade-offs** quand c'est ambigu — c'est lui qui décide.
4. **Ne pas sur-détailler** — CC a CLAUDE.md.
5. **Former, pas juste livrer** — expliquer le pourquoi.
