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
8 slices : Category, Item, Supplier, Customer, ItemSupplier, DailyMenu, Meal, MealItem.
Chaque slice : DTO / Validator / Service / Controller / Tests (SQLite in-memory).

### Frontend — complet

| Page         | Points clés                                                                      |
|--------------|----------------------------------------------------------------------------------|
| Category     | Référence pattern Index (pending rows + dirty + Save All)                        |
| Item         | FK Category, enum Unit, PackageSize                                              |
| Supplier     | Party + CompanyName, Siret                                                       |
| Customer     | Bouton CalendarMonth → `/menuplan/{id}`                                          |
| ItemSupplier | PK composite, pattern 404/409                                                    |
| MenuPlan/Index | Route `/menuplan/{CustomerId}`. Cards sur 3 ans. Données via `GET /api/dailymenus/{customerId}/monthly-summary` (HasMeals, MonthlyCost). Bouton "Voir le planning" → navigation directe `dayplans?customerId=X&year=Y&month=M`, pas de création serveur. |
| DayPlan/Index | Query params : `customerId`, `year`, `month`. Calendrier mensuel, barre nav mois (±6, HasMeals via monthly-summary), SortableJS reorder/move/**copy** (Ctrl au lâcher), panier, **cell-drag copy/move** (Ctrl=copie, zone latérale footer), **clic item=ajouter**, **Ctrl+clic clone**, **dbl-clic item=suppr**, **dbl-clic total=vider cellule** (total-primed), **row-primed** (mousedown sur date = rouge sur toute la ligne, prépare vider-ligne), overlay sauvegarde, grille 7 col (80px dates) |
| Layout       | Thème: Success=#1B5E20, Secondary=#7C3AED, Info=#1565C0, AppBar dégradé bleu-violet, NavMenu splitté (principal haut / admin bas) |

---

## Décisions d'architecture clés

Voir `CLAUDE.md` pour les détails. Résumé :
- **Shared** pur (zéro EF), pas de repository, on-demand DailyMenu/Meal.
- **Shopping Cart** (panneau droit global, `RightPanelState`).
- **MonthlyCost** calculé serveur-side (`ceil(qty / PackageSize) * UnitPrice`), affiché sur les cards MenuPlan et par item/cellule dans MealCell.
- **Copy/Move cellule** — cellule entière draggable (zones latérales footer). Ctrl tenu = copie. Plus de trash-zone : clear via dbl-clic sur le total.
- **Drag & drop immédiat** — plus de "Save All" / `_pendingMoves`. Tout appel API se fait au moment du drop. Overlay sombre pendant l'async.
- **SortableJS copy** — Ctrl au lâcher d'un item cross-cell = copie (ghost visible à la source pendant le drag).
- **Tests** — SQLite in-memory uniquement.

---

## Prochains chantiers (backlog CW)

### MenuPlan/Index
- Duplication d'un planning vers un autre mois
- Option voir les mois passés

### DayPlan/Index
- Coût par type de repas (ligne de total par colonne MealType)
- Total par semaine
- Vider-ligne (action confirmée par row-primed — UI déjà en place)

### Détails financiers
- Coût moyen et médian par jour/semaine/mois

### Style global
- Clic sur toute la carte MenuPlan → voir le planning (pas seulement le bouton)
- Revue globale du style

### Paramétrage utilisateur
- Dialogue de confirmation (destructive actions)
- 3 thèmes : Light / Medium / Dark

---

## Règles du CW

1. **Intention + contraintes** — jamais de pseudo-code.
2. **Trancher avant de briefer** — l'archi se résout avec Lead Dev avant que CC code.
3. **Options avec trade-offs** quand c'est ambigu — c'est lui qui décide.
4. **Ne pas sur-détailler** — CC a CLAUDE.md.
5. **Former, pas juste livrer** — expliquer le pourquoi.
