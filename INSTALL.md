# INSTALL.md — MenuManager : Installation de l'environnement de développement

Guide d'installation pas-à-pas de **MenuManager** sur un poste **Windows 10/11**.
Stack : Blazor WASM (PWA) + ASP.NET Core Web API .NET 9 + EF Core 9 + PostgreSQL 16 (Docker).

> Convention : toutes les commandes sont à exécuter dans **PowerShell**. Les blocs préfixés `# admin` exigent un terminal lancé en administrateur.

---

## Sommaire

1. [Prérequis](#1-prérequis)
2. [Activer WSL2](#2-activer-wsl2)
3. [Installer .NET, Docker, Git](#3-installer-net-docker-git)
4. [Démarrer Docker Desktop](#4-démarrer-docker-desktop)
5. [Vérifier les outils](#5-vérifier-les-outils)
6. [Cloner le projet](#6-cloner-le-projet)
7. [Authentification GitHub](#7-authentification-github)
8. [Démarrer PostgreSQL](#8-démarrer-postgresql)
9. [Lancer l'API Server](#9-lancer-lapi-server)
10. [Lancer le Client Blazor](#10-lancer-le-client-blazor)
11. [Configurer VS Code](#11-configurer-vs-code)
12. [Vérification end-to-end](#12-vérification-end-to-end)
13. [Commandes utiles au quotidien](#13-commandes-utiles-au-quotidien)
14. [Troubleshooting](#14-troubleshooting)

---

## 1. Prérequis

| Outil | Version | Rôle |
|-------|---------|------|
| Windows 10/11 64 bits | — | OS hôte |
| WSL2 | activé | Backend de Docker Desktop |
| Docker Desktop | 4.x | Conteneur PostgreSQL |
| .NET SDK | **9.0+** | Compilation et exécution de Server / Client |
| Git for Windows | 2.40+ | Clone, commit, push |
| VS Code | dernière | IDE |
| C# Dev Kit (extension) | dernière | IntelliSense + debug + tests |
| Claude Code (optionnel) | — | Assistant IA |

### Comment ouvrir un PowerShell admin

`Win + X` → cliquer sur **Terminal (Admin)** ou **Windows PowerShell (Admin)**.
La fenêtre **UAC** s'ouvre → cliquer **Oui**.
Le titre de la fenêtre doit afficher **Administrateur** ou **Admin**.

---

## 2. Activer WSL2

Docker Desktop sous Windows utilise **WSL2** comme backend Linux. Sans WSL2, Docker ne démarre pas.

### 2.1 Mettre à jour le kernel WSL (PowerShell admin)

```powershell
# admin
wsl --update
```

**Sortie attendue** :
```
Installing: Windows Subsystem for Linux
Windows Subsystem for Linux has been installed.
```

Si la commande répond `WSL is up to date`, c'est déjà bon.

### 2.2 Première installation de WSL (si jamais installé)

```powershell
# admin
wsl --install
```

Cette commande active la fonctionnalité Windows, télécharge le kernel et installe Ubuntu par défaut (que Docker n'utilisera pas mais qui est inoffensif). **Redémarrer le PC à la fin.**

### 2.3 Vérifier la version

```powershell
wsl --version
```

**Sortie attendue** (versions à titre indicatif) :
```
Version WSL : 2.x.x
Version du noyau : 5.15.x
...
```

---

## 3. Installer .NET, Docker, Git

`winget` (préinstallé sur Windows 10/11) télécharge et installe en gérant l'élévation tout seul.

### 3.1 .NET SDK 9

```powershell
# admin
winget install --id Microsoft.DotNet.SDK.9 -e
```

- `--id` : identifiant exact du package.
- `-e` : *exact match*, évite les ambiguïtés.

**Sortie attendue** se termine par `Successfully installed`.

### 3.2 Docker Desktop

```powershell
# admin
winget install --id Docker.DockerDesktop -e
```

À l'installation, Docker Desktop crée le dossier `C:\ProgramData\DockerDesktop`. Si une **install précédente a échoué**, ce dossier peut bloquer la nouvelle install (voir [Troubleshooting §14.1](#141-docker--for-security-reasons-cprogramdatadockerdesktop-must-be-owned-by-an-elevated-account)).

### 3.3 Git for Windows

```powershell
# admin
winget install --id Git.Git -e
```

L'installeur configure Git Credential Manager (utile pour l'auth GitHub via OAuth).

> Claude Code et VS Code ne sont pas (re)installés ici, ils sont supposés déjà présents.

---

## 4. Démarrer Docker Desktop

1. Menu Démarrer → chercher **Docker Desktop** → **Entrée**.
2. Accepter l'EULA à la première ouverture.
3. Attendre que l'icône **🐳 baleine** dans la barre des tâches devienne **fixe** (pas animée).
   - Animée = Docker démarre. Compter 30 s à 2 min selon la machine.
   - Fixe = daemon prêt.
4. Optionnel : Docker Desktop propose de se lancer au démarrage de Windows. Pratique en dev.

---

## 5. Vérifier les outils

**Fermer puis rouvrir un PowerShell** (sans admin cette fois). Cette étape est **indispensable** pour recharger le `PATH` modifié par les installeurs.

```powershell
dotnet --version
docker --version
git --version
wsl --version
```

**Sorties attendues** (numéros à titre d'exemple) :
```
9.0.100
Docker version 28.0.1, build abc1234
git version 2.45.0.windows.1
Version WSL : 2.x.x ...
```

Si `docker` répond `not recognized`, c'est presque toujours :
- Docker Desktop pas encore lancé une première fois → cf. §4.
- PATH pas rechargé → fermer / rouvrir PowerShell.

---

## 6. Cloner le projet

### 6.1 Créer le répertoire de travail

```powershell
mkdir C:\Dynas
cd C:\Dynas
```

`mkdir` crée le dossier. `cd` s'y déplace. Si `C:\Dynas` existe déjà, `mkdir` répond `Directory already exists` — ignorer et `cd` directement.

### 6.2 Cloner le dépôt

```powershell
git clone https://github.com/DynasR/MenuManager.git
cd MenuManager
```

**Sortie attendue** :
```
Cloning into 'MenuManager'...
remote: Enumerating objects: ..., done.
...
Resolving deltas: 100% (...), done.
```

### 6.3 Basculer sur la branche de développement

La branche de travail est `claude/setup-coding-environment-ONmOZ`.

**Cas 1 — la branche existe déjà sur GitHub** :

```powershell
git fetch origin
git checkout claude/setup-coding-environment-ONmOZ
```

`git fetch origin` rapatrie les références distantes sans modifier le code local. `git checkout` bascule sur la branche, qui est automatiquement liée à `origin/claude/setup-coding-environment-ONmOZ`.

**Cas 2 — la branche n'existe pas encore** (réponse `pathspec ... did not match any file(s) known to git`) :

```powershell
git checkout -b claude/setup-coding-environment-ONmOZ
git push -u origin claude/setup-coding-environment-ONmOZ
```

- `-b` crée la branche depuis le HEAD courant (`main`).
- `-u` lie la branche locale à son équivalent distant pour les `pull` / `push` futurs.

### 6.4 Vérifier l'état

```powershell
git status
git branch --show-current
git log --oneline -n 5
```

**Sortie attendue** :
```
On branch claude/setup-coding-environment-ONmOZ
nothing to commit, working tree clean
claude/setup-coding-environment-ONmOZ
<sha> <message>
...
```

---

## 7. Authentification GitHub

Au premier `git push`, Windows déclenche **Git Credential Manager** :
- Une fenêtre navigateur s'ouvre → s'authentifier sur GitHub (login / 2FA).
- Le credential est stocké dans le **Gestionnaire d'identification Windows** — plus jamais demandé.

### Alternative — Personal Access Token (PAT)

Si l'OAuth ne s'ouvre pas (machine sans navigateur, SSH, etc.) :
1. Aller sur `https://github.com/settings/tokens`.
2. Créer un PAT classique avec scope `repo`.
3. À la prochaine demande de password, **coller le token** (et non le mot de passe GitHub).

---

## 8. Démarrer PostgreSQL

Le fichier `docker-compose.yml` à la racine définit un service Postgres :

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: menumanager
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: admin123
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
volumes:
  pgdata:
```

### 8.1 Lancer le conteneur

Depuis `C:\Dynas\MenuManager` :

```powershell
docker compose up -d
```

- `up` : crée et démarre les conteneurs déclarés.
- `-d` : *detached*, laisse tourner en arrière-plan et libère le terminal.

**Sortie attendue à la première exécution** :
```
[+] Running 15/15
 ✔ postgres Pulled                ...
 ✔ Network menumanager_default    Created
 ✔ Volume "menumanager_pgdata"    Created
 ✔ Container menumanager-postgres-1  Started
```

### 8.2 Vérifier l'état

```powershell
docker compose ps
```

**Sortie attendue** :
```
NAME                       IMAGE         STATUS         PORTS
menumanager-postgres-1     postgres:16   Up 30 seconds  0.0.0.0:5432->5432/tcp
```

`STATUS` doit indiquer `Up ...` (et idéalement `(healthy)` si un healthcheck est défini).

### 8.3 Coordonnées de connexion

| Paramètre | Valeur |
|-----------|--------|
| Host | `localhost` |
| Port | `5432` |
| Database | `menumanager` |
| User | `admin` |
| Password | `admin123` |
| Volume Docker | `pgdata` (persistant) |

Cette config est référencée dans `Server/appsettings.json`.

> **Migrations EF Core** : appliquées automatiquement au démarrage du Server via `db.Database.Migrate()` dans `Server/Program.cs`. Aucun `dotnet ef database update` à lancer manuellement.

---

## 9. Lancer l'API Server

Dans un **premier terminal** PowerShell, depuis `C:\Dynas\MenuManager` :

```powershell
dotnet run --project Server
```

- `--project Server` cible le csproj `Server/Server.csproj`.
- Compile, applique les migrations, démarre Kestrel.

**Sortie attendue** :
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5075
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

À la **première exécution**, Windows affiche un dialogue **pare-feu** :
- Cocher **Réseaux privés** uniquement.
- Cliquer **Autoriser**.

Pour arrêter le serveur : `Ctrl + C` dans le terminal.

> Les profils sont définis dans `Server/Properties/launchSettings.json` (HTTP `5075`, HTTPS `7216`).

---

## 10. Lancer le Client Blazor

Dans un **deuxième terminal** PowerShell, depuis `C:\Dynas\MenuManager` :

```powershell
dotnet run --project Client
```

**Sortie attendue** :
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5068
...
```

Le navigateur s'ouvre automatiquement (cf. `launchBrowser: true` dans `launchSettings.json`).

> Le client lit `ServerUrl` dans `Client/wwwroot/appsettings.json`. Il doit pointer sur `http://localhost:5075`. Modifier ce fichier si l'API tourne ailleurs.

---

## 11. Configurer VS Code

### 11.1 Ouvrir la solution

```powershell
code C:\Dynas\MenuManager\MenuManager.sln
```

`code` est ajouté au PATH par l'installeur VS Code. Si la commande répond `not recognized`, ouvrir VS Code via le menu Démarrer puis `Fichier → Ouvrir un dossier` → `C:\Dynas\MenuManager`.

### 11.2 Extensions à installer

| Extension | Auteur | Pourquoi |
|-----------|--------|----------|
| **C# Dev Kit** | Microsoft | Solution explorer, debug, tests |
| **C#** | Microsoft | IntelliSense (dépendance auto de C# Dev Kit) |
| **Docker** | Microsoft | Gérer les conteneurs depuis VS Code (optionnel) |
| **MudBlazor Snippets** | MudBlazor | Snippets components (optionnel) |

Installation : `Ctrl + Shift + X` → chercher → **Install**.

### 11.3 Debug F5

`F5` → VS Code propose les profils de `Server/Properties/launchSettings.json` :
- **http** → port 5075, profil le plus simple.
- **https** → port 7216 (nécessite `dotnet dev-certs https --trust`).

Pour debug Server + Client en parallèle, créer un `launch.json` composé (non requis pour démarrer).

---

## 12. Vérification end-to-end

Lancer dans l'ordre Postgres → Server → Client, puis :

1. **DB** :
   ```powershell
   docker compose ps
   ```
   → `menumanager-postgres-1` en `Up`.

2. **API** :
   ```powershell
   curl http://localhost:5075/api/categories
   ```
   → réponse JSON `[]` si la base est vide, ou liste de catégories.
   *(Sous PowerShell, `curl` est un alias de `Invoke-WebRequest` — la sortie diffère mais le code 200 confirme l'accès.)*

3. **Front** : ouvrir `http://localhost:5068/categories` :
   - Page chargée.
   - Console F12 → onglet **Network** : aucune requête en erreur (ni rouge, ni 500).

4. **Round-trip DB** :
   - Créer une catégorie via l'UI.
   - `F5` (refresh navigateur).
   - La catégorie est toujours là → la persistance fonctionne.

5. **Git** :
   ```powershell
   git status
   git pull
   ```
   → `working tree clean`, `Already up to date.`.

---

## 13. Commandes utiles au quotidien

### Postgres / Docker

```powershell
docker compose up -d                  # démarrer
docker compose stop                   # stopper sans détruire le conteneur
docker compose start                  # redémarrer un conteneur stoppé
docker compose down                   # stopper + détruire conteneurs (volume conservé)
docker compose down -v                # stopper + détruire conteneurs ET volumes (efface la DB)
docker compose logs -f postgres       # suivre les logs en temps réel
docker exec -it menumanager-postgres-1 psql -U admin -d menumanager
                                       # ouvrir un psql interactif
```

### .NET

```powershell
dotnet build                          # build complet de la solution
dotnet run --project Server           # lancer l'API
dotnet run --project Client           # lancer le front
dotnet test                           # exécuter tous les tests xUnit
dotnet test --filter "FullyQualifiedName~CategoryService"
                                       # filtrer par nom
dotnet watch --project Server run     # hot-reload sur Server
```

### Git

```powershell
git status
git pull
git add <fichier>                     # ou git add -A pour tout
git commit -m "feat(slice): message"
git push                              # ou git push -u origin <branche> au premier push
git log --oneline -n 10
git diff                              # voir les modifs unstaged
git diff --staged                     # voir les modifs staged
git switch -c feature/xxx             # créer + basculer sur une nouvelle branche
git switch main                       # revenir sur main
```

### Réinitialiser la base

```powershell
docker compose down -v                # supprime le volume pgdata
docker compose up -d                  # recrée la DB vide
dotnet run --project Server           # ré-applique toutes les migrations EF Core
```

---

## 14. Troubleshooting

### 14.1 Docker — *"For security reasons C:\ProgramData\DockerDesktop must be owned by an elevated account"*

Cause : un dossier laissé par une install précédente avec une mauvaise propriété.

**Fix 1 — réparer la propriété** (PowerShell admin) :
```powershell
# admin
takeown /f "C:\ProgramData\DockerDesktop" /r /d y
icacls "C:\ProgramData\DockerDesktop" /grant Administrators:F /t
winget install --id Docker.DockerDesktop -e
```
- `takeown` change le propriétaire vers le groupe Administrateurs.
- `/r /d y` applique récursivement.
- `icacls` accorde le contrôle total au groupe Administrateurs.

**Fix 2 — supprimer puis réinstaller** :
```powershell
# admin
Remove-Item -Recurse -Force "C:\ProgramData\DockerDesktop"
winget install --id Docker.DockerDesktop -e
```

### 14.2 *"docker: not recognized"* après installation

PATH pas rechargé.
- Fermer **toutes** les fenêtres PowerShell.
- Lancer Docker Desktop une fois (icône baleine fixe).
- Rouvrir PowerShell, retester.

### 14.3 *"wsl --update : The requested operation requires elevation"*

Lancer la commande depuis un **PowerShell admin** (`Win + X` → Terminal Admin).

### 14.4 *"pathspec 'claude/...' did not match any file(s) known to git"*

La branche n'existe pas en local. Soit :
```powershell
git fetch origin
git checkout claude/setup-coding-environment-ONmOZ
```
Soit, si elle n'existe pas non plus sur le remote :
```powershell
git checkout -b claude/setup-coding-environment-ONmOZ
git push -u origin claude/setup-coding-environment-ONmOZ
```

### 14.5 Port 5432 déjà utilisé

Un Postgres natif Windows tourne déjà.
- Stopper le service : `services.msc` → chercher *postgresql* → arrêter.
- Ou modifier `docker-compose.yml` pour mapper sur un autre port (ex. `5433:5432`) **et** mettre à jour la connection string dans `Server/appsettings.json`.

### 14.6 Diffs Git "vides" après clone (CRLF / LF)

```powershell
git config --global core.autocrlf input
git checkout -- .
```
- `core.autocrlf input` : Git ne convertit que les LF en CRLF au commit, jamais à l'inverse.
- `checkout -- .` réinitialise les fins de ligne.

### 14.7 *"The certificate is not trusted"* en HTTPS

Si tu utilises les profils HTTPS :
```powershell
dotnet dev-certs https --trust
```
Une boîte de dialogue s'affiche → **Oui** pour faire confiance au certificat de dev.

### 14.8 Docker Desktop refuse de démarrer

- Vérifier que la **virtualisation matérielle** est activée dans le BIOS (Intel VT-x / AMD-V).
- Vérifier que **Hyper-V** ou **WSL2** est actif :
  ```powershell
  # admin
  bcdedit /enum | Select-String hypervisorlaunchtype
  ```
  → doit indiquer `Auto`.

### 14.9 Pare-feu bloque Server / Client

Dialogue Windows à la première exécution → **Réseaux privés** + **Autoriser**.
Si déjà refusé :
- `Panneau de configuration` → `Pare-feu Windows Defender` → `Autoriser une application`.
- Chercher `dotnet` → cocher **Privé**.

---

## Liens utiles

- Contexte projet (architecture, slices, conventions) : [`CLAUDE.md`](./CLAUDE.md)
- Liste des tâches : [`TODO.MD`](./TODO.MD)
- Doc .NET 9 : `https://learn.microsoft.com/dotnet/`
- Doc EF Core 9 : `https://learn.microsoft.com/ef/core/`
- Doc Blazor : `https://learn.microsoft.com/aspnet/core/blazor/`
- Doc MudBlazor : `https://mudblazor.com/`
