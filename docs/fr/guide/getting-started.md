# Démarrage

::: warning Jeune mais fonctionnel
L'engine d'installation et la CLI sont implémentés et testés de bout en bout :
`init`, `add`, `list`, `remove`, `diff` et `update` fonctionnent. La première
préoccupation du registre — **email** (`email-abstractions`, `email-smtp`,
`email-sendgrid`) — est réelle, compilée et testée.

Ce qui n'est **pas encore fait**, c'est la distribution : le tool `outlet` n'est pas
encore publié sur NuGet, et aucun registre public n'est hébergé. En attendant, on lance
la CLI depuis les sources et on la pointe vers sa propre source de registre.
:::

## Prérequis

- **SDK .NET 10** (les projets ciblent .NET 10 / C# 14).
- **Node 22+** uniquement si vous voulez builder ce site de documentation ou les packages
  frontend.

## Lancer la CLI depuis les sources

En attendant la publication du tool global, lancez la CLI directement depuis le dépôt :

```bash
# Lister les items disponibles dans les registres configurés
dotnet run --project src/Outlet.Cli -- list

# Initialiser outlet.json à partir du projet/solution détecté
dotnet run --project src/Outlet.Cli -- init

# Copier un item (et ses dépendances) dans le projet
dotnet run --project src/Outlet.Cli -- add email-smtp
```

Autres commandes déjà fonctionnelles :

```bash
outlet remove <item>   # supprime les fichiers d'un item installé, nettoie les NuGet inutilisés
outlet diff <item>     # montre comment votre copie locale diffère de la version du registre
outlet update <item>   # met à jour un item en préservant vos éditions (conflits → <fichier>.outlet-new)
```

## Le modèle mental

1. Vous choisissez une **préoccupation** (aujourd'hui : email).
2. `init` écrit `outlet.json` (routage + lockfile) à partir du projet détecté.
3. `add` du **contrat** (le port générique + DTOs) — zéro dépendance externe — puis d'un
   **adapter** pour le provider voulu (par exemple `email-smtp` ou `email-sendgrid`).
   Outlet résout les dépendances, réécrit les namespaces avec Roslyn, écrit les fichiers
   au target routé, et ajoute les packages NuGet (CPM-aware).
4. Vous le câblez avec l'extension `AddXxx()` de l'adapter dans votre composition root.
5. Pour changer de provider plus tard, faites `add` d'un autre adapter et modifiez une
   seule ligne `AddXxx()` — le port reste identique. Comme vous possédez le code,
   `diff`/`update` préservent vos éditions locales quand une nouvelle version du registre
   arrive.

## Builder le tout en local

```bash
dotnet build Outlet.slnx -c Release          # 0 warning attendu
dotnet test Outlet.slnx --filter "Category!=Live"
```

Pour les conventions et règles d'architecture du projet, voir les pages
[Testing strategy](/testing) et [Production readiness](/production-readiness)
(documentation contributeurs, en anglais).
