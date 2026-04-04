# DayPlan — Analyse détaillée (MealCell & Index)

> Fichier de référence pour CW et Lead Dev.
> Couvre l'état visuel, fonctionnel et les décisions de conception de la vue DayPlan.
> À lire avant tout brief portant sur `DayPlan/Index.razor` ou `MealCell.razor`.

---

## Architecture de la cellule (rappel rapide)

```
dayplan-grid (CSS Grid 7 colonnes)
  └── dayplan-grid-cell          ← wrapper de colonne (padding, min-height)
        └── <MealCell>           ← composant autonome par (date × MealType)
              ├── <ul> .meal-cell-list
              │     └── <li> .meal-cell-item  (×N items)
              │           ├── span.meal-cell-item-name
              │           ├── span.meal-cell-item-price
              │           └── boutons : Copy | Move | Delete
              └── <div> .meal-cell-footer
                    ├── bouton FileCopy (copie cellule)
                    ├── bouton DriveFileMove (déplace cellule)
                    ├── span.meal-cell-slot-total  (total + Add icon flottant)
                    ├── [fantôme FileCopy]   ← spacer invisible pour équilibre
                    └── [fantôme DriveFileMove]
```

**Deux niveaux d'action copy/move :**
- **Item** : un seul `MealSlotItem` (boutons sur chaque ligne)
- **Cellule** : tous les items d'un slot (boutons dans le footer)

**Déclenchement 2 étapes (immédiat, non différé) :**
1. Clic source → `_pendingAction` posé dans Index, chip toolbar affiché
2. Clic cible → `HandleTargetSelectedAsync` → `ExecuteCellActionAsync` ou `ExecuteItemActionAsync`
3. Second clic sur la même source → annulation (`_pendingAction = null`)

---

## Problèmes visuels identifiés

### 1. Couleurs de débogage inline encore présentes

Tous ces styles sont des placeholders de prototypage, pas des couleurs finales :

| Élément | Style inline | Couleur |
|---------|-------------|---------|
| `<ul class="meal-cell-list">` | `background:#fff3cd` | Jaune pâle |
| `<span class="meal-cell-item-name">` | `background:#d8f5d8` | Vert menthe |
| `<span class="meal-cell-item-price">` | `background:#f5d8f5` | Mauve pâle |
| Bouton Delete (Close) | `Style="background:#ffd8b0"` | Pêche |
| Bouton Copy cell (FileCopy) | `Style="background:#b0d8ff"` | Bleu pâle |

**Impact** : rendu prototype, manque de cohérence avec le thème MudBlazor.
**Action à prévoir** : retirer tous ces inline styles, définir les couleurs finales via CSS classes ou thème.

### 2. `.meal-cell-item-source` mixe avec une couleur supprimée

```css
.meal-cell-item-source {
    background: color-mix(in srgb, var(--mud-palette-info) 20%, #d0f0ff) !important;
}
```

`#d0f0ff` était l'ancien fond bleu du `<li>` (supprimé). Le mix donne maintenant un bleu-info mélangé à un bleu fixe au lieu du surface. À remplacer par `var(--mud-palette-surface)` ou `white`.

### 3. `.meal-cell-add-icon:hover` est une règle morte

```css
.meal-cell:hover .meal-cell-add-icon:hover {
    opacity: 0.9;
    transform: scale(1.18);   /* ← ne se déclenche jamais */
}
```

L'icône Add a `pointer-events:none` dans `AddIconStyle`. Or `pointer-events:none` désactive aussi le pseudo-sélecteur CSS `:hover` sur l'élément lui-même. La règle de scale ne s'applique donc jamais. La règle parente `.meal-cell:hover .meal-cell-add-icon` fonctionne bien (opacity fade), seul le direct hover de l'icône est mort.

**Correction possible** : déclencher le scale via `.meal-cell-footer:hover .meal-cell-add-icon` à la place.

### 4. `.meal-cell-empty` sans règle CSS

La classe est ajoutée quand `Items.Count == 0` mais aucune règle ne l'utilise. Actuellement inerte. Prévoir soit une règle visuelle (ex. cursor, couleur hint, opacité), soit la retirer jusqu'à utilisation.

### 5. `.meal-cell-item:hover` actif en mode cible

Quand une cellule est en `IsActionTarget`, les items de cette cellule reçoivent encore le wash vert au hover (`.meal-cell-item:hover`). Les boutons item sont cachés, mais le glow vert indique une interactivité inexistante (les items ne sont pas cliquables individuellement en mode cible).

**Correction possible** : ajouter `:not(.meal-cell-target-copy):not(.meal-cell-target-move)` à la règle `.meal-cell-item:hover`, ou supprimer le glow item quand `HasPendingAction`.

---

## Problèmes fonctionnels / quirks code

### 6. Footer entièrement cliquable comme "Add"

`HandleFooterClick` est sur le footer div. Quand `!IsActionTarget`, il appelle `OnAddRequested` **sans condition** — même si des items existent. Cliquer sur le texte du total ou dans le vide du footer ouvre le drawer d'ajout.

C'est **intentionnel** mais peut surprendre. À documenter comme décision de UX.

### 7. Layout par éléments fantômes (footer + toolbar)

Deux patterns de centrage "fantôme" dans le projet :

**Footer MealCell** :
```html
[FileCopy réel][DriveFileMove réel][Total flex:1][FileCopy fantôme][DriveFileMove fantôme]
```

**Toolbar DayPlan/Index** :
```html
[Save All invisible][Chip centré flex:1][Save All réel]
```

Fonctionnel mais fragile si les tailles des éléments changent (ex. ajout d'un 3e bouton). Alternative propre : `position: absolute` sur le contenu centré dans un conteneur `position: relative`.

### 8. `PendingSourceItemId` null pour les actions cellule

Quand l'action est une copie/déplacement de cellule (pas d'item), `_pendingAction.ItemId` est null. MealCell reçoit `PendingSourceItemId="null"`. La logique `IsSource && PendingSourceItemId == item.Id` est alors `false` pour tous les items (null ≠ int). Résultat : aucun item individuel n'est stylé comme source — c'est la cellule entière via `CellCssClass` qui prend le style source. **Comportement correct.**

### 9. Annulation de l'action seulement depuis le chip ou la source

Quand `_pendingAction` est actif :
- Cellule source → ses boutons copy/move restent visibles (`IsActionTarget = false`) → clic annule
- Cellules cibles → boutons cachés (`IsActionTarget = true`) → pas d'annulation directe
- Seul le chip dans la toolbar (bouton fermer) annule depuis une cellule non-source

UX correcte mais à préciser dans les briefs si on veut ajouter un "Échap" clavier.

---

## État des couleurs du thème (référence)

| Rôle | Valeur | Usage |
|------|--------|-------|
| Success | `#1B5E20` | Hover cell, mois courant, bouton Add |
| Secondary | `#7C3AED` | Move actions |
| Info | `#1565C0` | Copy actions |
| Warning | utilisé pour jours fériés | |

---

## Priorité suggérée pour les prochaines sessions

1. **Retirer les 5 couleurs debug inline** (MealCell) — quick win visuel majeur
2. **Fixer `.meal-cell-item-source`** → remplacer `#d0f0ff` par `var(--mud-palette-surface)`
3. **Fixer hover item en mode cible** → `:not()` guards sur `.meal-cell-item:hover`
4. **Décider de `meal-cell-empty`** → style ou suppression
5. **`.meal-cell-add-icon:hover`** → déclencher via parent plutôt que self
