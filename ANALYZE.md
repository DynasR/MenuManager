# ANALYZE — Script d'audit automatique (CC)

> Lancé à la demande du Lead Dev. Produit trois fichiers : `TODO.md`, `CONTEXT_CW.md` compacté, `CLAUDE.md` compacté.
> Toujours un **full** (écrase la version précédente). Ne pas faire d'incrémental.

---

## Phase 0 — Inventaire

Avant toute analyse, lister les fichiers présents :

```
find . -type f \( -name "*.cs" -o -name "*.razor" -o -name "*.razor.cs" \) \
  | grep -v "bin/" | grep -v "obj/" | sort
```

Note le nombre total. Cela calibre la profondeur d'analyse.

---

## Phase 1 — Analyse statique du code

Parcours **tous** les fichiers C# et Razor. Pour chaque finding, note :
- Fichier + ligne approximative
- Catégorie (voir liste ci-dessous)
- Sévérité : ★★★ bloquant / ★★ important / ★ mineur
- Description concise (1–2 lignes max)

### Catégories à chercher

**BUGS**
- Null reference non protégé sur un nullable
- Logique métier incohérente (ex : condition inversée, off-by-one)
- Race condition possible (appels async non awaités, state mutation sans lock)
- Contrainte DB non reflétée côté client (ex : unique non vérifiée avant POST)

**PERFORMANCE / OPTIMISATION**
- N+1 queries (boucle avec appel DB/HTTP à l'intérieur)
- `SaveChangesAsync` dans une boucle (cf. KI-4 connu)
- `ToList()` suivi d'un `.Where()` (filtre côté client au lieu de DB)
- Re-render Blazor inutile (composant sans `ShouldRender`, `EventCallback` non optimisé)
- Chargement réseau non mis en cache alors qu'il pourrait l'être
- `await` dans une boucle qui pourrait être `Task.WhenAll`

**FACTORISATION / DRY**
- Chaîne `ThenInclude` dupliquée (déjà connue — noter les occurrences exactes)
- Logique de calcul de coût copiée dans plusieurs composants
- Mapping inline qui devrait passer par `MealItemMapper`
- Validation dupliquée entre frontend et backend

**CODE MORT**
- Méthode / propriété / composant non référencé
- `using` inutilisé
- Paramètre de méthode jamais utilisé
- Commentaire `// TODO` ou `// FIXME` déjà dans le code source
- Feature flag / booléen qui ne varie plus

**MAINTENABILITÉ**
- Magic string / magic number (seuil, délai, nom de route en dur)
- Composant Razor > 400 lignes (candidat à la décomposition)
- Service avec trop de responsabilités (> 3 méthodes publiques de natures différentes)
- Couplage fort là où une interface suffirait

**SÉCURITÉ / ROBUSTESSE**
- Input non validé côté serveur alors que le validator FluentValidation existe
- Endpoint sans gestion d'erreur (pas de try/catch ni de ProblemDetails)
- Seed data avec valeurs sensibles codées en dur

---

## Phase 2 — Vérification des Bugs connus

Relire la section **Bugs connus** de `CONTEXT_CW.md`.
Pour chaque entrée :
- Confirmer que le bug est **toujours présent** dans le code actuel
- Si corrigé : le noter comme "résolu, à retirer du backlog"
- Si aggravé ou étendu : mettre à jour la description

---

## Phase 3 — Écriture de TODO.md

Écraser `TODO.md` à la racine. Format :

```markdown
# TODO — Audit automatique
> Généré le {DATE}. Full scan. Remplace la version précédente.

---

## ★★★ Bloquant

| # | Fichier | Catégorie | Description |
|---|---------|-----------|-------------|
| B1 | ... | BUG | ... |

## ★★ Important

| # | Fichier | Catégorie | Description |
|---|---------|-----------|-------------|

## ★ Mineur / Quick win

| # | Fichier | Catégorie | Description |
|---|---------|-----------|-------------|

---

## Bugs connus — statut

| KI | Statut | Note |
|----|--------|------|
| KI-2 | Toujours présent | ... |
| KI-4 | Toujours présent | ... |

---

## Duplications résiduelles confirmées

Liste les fichiers exacts + numéros de ligne pour chaque duplication connue ou nouvelle.
```

Règles de rédaction :
- Une ligne = un finding. Pas de sous-bullets.
- Fichier = chemin relatif depuis la racine, sans préfixe `./`.
- Si plusieurs fichiers pour un même finding : lister le plus représentatif, noter "(+ N autres)" dans la description.
- Maximum **40 entrées** au total. Au-delà, regrouper les similaires.

---

## Phase 4 — Mise à jour partielle de CONTEXT_CW.md

> CC ne touche qu'à deux sections. Tout le reste (décisions d'archi, rôles, règles CW) est hors périmètre.

### Section "État du projet"
Mettre à jour le tableau Frontend et le statut Backend pour refléter l'état réel du code :
- Ajouter les pages/slices terminées depuis la dernière version.
- Corriger les descriptions incorrectes ou obsolètes.
- Ne pas modifier la structure du tableau ni les colonnes.

### Section "Bugs connus"
Synchroniser avec les résultats de la Phase 2 :
- Marquer les KI résolus avec ~~barré~~ + note "résolu" (ne pas supprimer — c'est le CW qui décide de les retirer).
- Mettre à jour la description des KI aggravés ou étendus.
- Ajouter les nouveaux bugs ★★★ détectés en Phase 1 qui méritent un suivi long terme.

**Ne pas toucher à** : Rôles, Stack, Décisions d'architecture, Duplications résiduelles, Règles CW.

---

## Phase 5 — Compactage de CLAUDE.md

> `CONTEXT_CW.md` n'est pas de ton ressort pour le reste — c'est le contexte du CW, géré hors session.

Objectif : **max signal, pas max volume**. Ne pas viser une taille cible — viser la densité.

Règles — supprimer uniquement ce qui est :
1. **Obsolète** : instructions liées à une slice terminée et stable, migrations déjà appliquées, patterns remplacés par un meilleur.
2. **Redondant** : même convention exprimée deux fois dans des sections différentes — garder la plus précise, supprimer l'autre.
3. **Évident** : convention que tu appliques naturellement sans avoir besoin de la relire (ex : "utiliser async/await").

Règles — garder impérativement :
- Tout ce qui est **contre-intuitif** ou spécifique au projet (ex : `ShouldRender` override, diff-based dialog).
- Tout ce qui **guide une décision future** (ex : structure des slices, règles EF Core, règles de test).
- Les **bugs connus actifs** (synchronisés avec Phase 2).
- Conventions de nommage, structure des slices, règles Blazor.

Ajouter en tête un **index de sections** (liens markdown) si le fichier dépasse 200 lignes.

Écraser `CLAUDE.md` avec la version compactée.

---

## Phase 5 — Rapport terminal

Afficher en fin d'exécution :

```
✅ TODO.md       — {N} findings ({B} bloquants, {I} importants, {M} mineurs)
✅ CONTEXT_CW.md — État du projet mis à jour / {N} KI modifiés
✅ CLAUDE.md     — {X} → {Y} lignes ({N} sections supprimées, {N} fusionnées)
⚠️  Bugs connus  — KI-2 toujours présent / KI-4 toujours présent
```

Si des sections résistent au compactage (denses mais non réductibles), le noter explicitement : c'est une information utile, pas un échec.

---

## Contraintes globales

- Ne pas modifier les fichiers source `.cs` / `.razor` — analyse seulement.
- Ne pas créer de nouveaux fichiers autres que `TODO.md` et `CLAUDE.md`. Modifier `CONTEXT_CW.md` uniquement sur les deux sections autorisées.
- Si une ambiguïté bloque l'analyse (fichier introuvable, structure inattendue), noter dans le rapport terminal et continuer.
- Durée estimée : longue. Ne pas interrompre entre les phases.