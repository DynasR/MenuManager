# INSTALL.md — MenuManager : Installation de l'environnement de développement

Guide d'installation locale de **MenuManager** sur un poste Windows.
Stack : Blazor WASM (PWA) + ASP.NET Core Web API .NET 9 + EF Core 9 + PostgreSQL 16 (Docker).

---

## 1. Prérequis

| Outil | Version | Usage |
|-------|---------|-------|
| Windows 10/11 | 64 bits | OS hôte |
| .NET SDK | 9.0+ | Build & exécution Server/Client |
| Docker Desktop | 4.x | Conteneur PostgreSQL |
| Git | 2.40+ | Clone & push |
| WSL2 | Activé | Backend Docker Desktop |
| VS Code | dernière | IDE |
| C# Dev Kit | extension | IntelliSense + debug |

Claude Code peut être installé en parallèle pour assister le développement.

---

## 2. Installation des outils

Toutes les commandes ci-dessous sont à lancer dans **PowerShell**.

### 2.1 Activer WSL2 (PowerShell admin)

```powershell
wsl --update
wsl --version
```

Si WSL n'est pas installé du tout :
```powershell
wsl --install
```
Puis redémarrer le PC.

### 2.2 Installer .NET / Docker / Git via winget (PowerShell admin)

```powershell
winget install --id Microsoft.DotNet.SDK.9 -e
winget install --id Docker.DockerDesktop -e
winget install --id Git.Git -e
```

> **Erreur "must be owned by an elevated account"** lors de l'install Docker :
> ```powershell
> takeown /f "C:\ProgramData\DockerDesktop" /r /d y
> icacls "C:\ProgramData\DockerDesktop" /grant Administrators:F /t
> winget install --id Docker.DockerDesktop -e
> ```
> Si le dossier ne peut pas être réparé, le supprimer puis relancer winget :
> ```powershell
> Remove-Item -Recurse -Force "C:\ProgramData\DockerDesktop"
> ```

### 2.3 Démarrer Docker Desktop

Lancer **Docker Desktop** depuis le menu Démarrer une première fois → accepter l'EULA → attendre l'icône baleine fixe (non animée) dans la barre des tâches.

### 2.4 Vérifier les installations

Fermer puis rouvrir un PowerShell (pour rafraîchir le PATH) :

```powershell
dotnet --version    # → 9.0.x
docker --version    # → 28.x ou +
git --version       # → 2.40+
```

---

## 3. Cloner le projet

```powershell
mkdir C:\Dynas
cd C:\Dynas
git clone https://github.com/DynasR/MenuManager.git
cd MenuManager
```

### 3.1 Branche de développement

La branche par défaut est `main`. Pour basculer sur la branche de travail :

```powershell
git fetch origin
git checkout claude/setup-coding-environment-ONmOZ
```

Si la branche n'existe pas encore sur le remote, la créer localement :

```powershell
git checkout -b claude/setup-coding-environment-ONmOZ
git push -u origin claude/setup-coding-environment-ONmOZ
```

### 3.2 Authentification GitHub

Au premier `git push`, **Git Credential Manager** ouvre une fenêtre navigateur (OAuth). Si tu préfères un Personal Access Token :
- `https://github.com/settings/tokens` → scope `repo`
- Coller le token comme password à la première demande.

---

## 4. Démarrer l'environnement

### 4.1 PostgreSQL (Docker)

Depuis `C:\Dynas\MenuManager` :

```powershell
docker compose up -d
docker compose ps     # vérifier "running" sur port 5432
```

| Paramètre | Valeur |
|-----------|--------|
| Host | `localhost` |
| Port | `5432` |
| Database | `menumanager` |
| User | `admin` |
| Password | `admin123` |
| Volume | `pgdata` (persistant) |

> Les migrations EF Core sont appliquées automatiquement au démarrage du Server (`db.Database.Migrate()` dans `Server/Program.cs`). Aucun `dotnet ef` à lancer manuellement.

### 4.2 API Server (terminal 1)

```powershell
cd C:\Dynas\MenuManager
dotnet run --project Server
# → http://localhost:5075
```

À la première exécution, Windows demandera l'autorisation pare-feu — accepter (réseau Privé).

### 4.3 Client Blazor (terminal 2)

```powershell
cd C:\Dynas\MenuManager
dotnet run --project Client
# → http://localhost:5068
```

Ouvrir `http://localhost:5068` dans le navigateur.

---

## 5. VS Code

```powershell
code C:\Dynas\MenuManager\MenuManager.sln
```

### Extensions recommandées

| Extension | Auteur | Rôle |
|-----------|--------|------|
| **C# Dev Kit** | Microsoft | Solution explorer, debug, tests |
| **C#** | Microsoft | IntelliSense (dépendance de C# Dev Kit) |
| **Docker** | Microsoft | Gestion conteneurs (optionnel) |

### Debug (F5)

`F5` → choisir le profil `MenuManager.Server` (généré depuis `Server/Properties/launchSettings.json`).
Pour debug le Client en parallèle, ouvrir un second profil ou utiliser le launch composé.

---

## 6. Vérification end-to-end

1. `docker compose ps` → conteneur `postgres` healthy.
2. `curl http://localhost:5075/api/categories` → renvoie `[]` (DB vide) ou JSON.
3. Naviguer sur `http://localhost:5068/categories` → page chargée, aucune erreur réseau (F12 → Console).
4. Créer une catégorie via l'UI → refresh → la donnée persiste (round-trip DB OK).
5. `git status` propre, `git pull` / `git push` fonctionnels sur la branche de travail.

---

## 7. Points d'attention

- **WSL2 obligatoire** pour Docker Desktop. Premier setup ≈ 10 min (kernel + redémarrage).
- **Port 5432** : libre par défaut sur Windows (pas de Postgres natif).
- **HTTPS dev cert** : non requis avec les profils HTTP. Si besoin :
  ```powershell
  dotnet dev-certs https --trust
  ```
- **Volume Postgres** :
  - `docker compose down` → conserve les données.
  - `docker compose down -v` → **efface** les données.
- **Fins de ligne Git** : Git for Windows utilise CRLF par défaut. Si tu vois des diffs vides après clone :
  ```powershell
  git config --global core.autocrlf input
  git checkout -- .
  ```
- **Configuration client** : `Client/wwwroot/appsettings.json` pointe sur `ServerUrl: http://localhost:5075`. Modifier si l'API tourne ailleurs.

---

## 8. Arrêter l'environnement

```powershell
# Dans chaque terminal Server/Client : Ctrl+C
docker compose down       # arrête Postgres, conserve les données
```

---

## 9. Réinitialiser la base

```powershell
docker compose down -v    # supprime le volume pgdata
docker compose up -d      # recrée la base vide
dotnet run --project Server   # réapplique toutes les migrations
```

---

## Référence rapide

| Commande | Usage |
|----------|-------|
| `docker compose up -d` | Démarre PostgreSQL |
| `docker compose down` | Arrête PostgreSQL |
| `docker compose logs -f postgres` | Logs DB |
| `dotnet run --project Server` | Lance l'API |
| `dotnet run --project Client` | Lance le client Blazor |
| `dotnet test` | Lance les tests xUnit |
| `dotnet build` | Build complet |

---

Pour le contexte projet (architecture, slices, conventions), voir [`CLAUDE.md`](./CLAUDE.md).
