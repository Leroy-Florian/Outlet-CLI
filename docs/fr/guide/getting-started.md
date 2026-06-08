# Démarrage

::: warning En cours de construction
Outlet en est à ses débuts. Le squelette compilable est en place : `outlet list`
fonctionne, tandis que `outlet init` et `outlet add` sont pour l'instant des stubs. Le
premier contenu de registre (l'item email) est encore en construction. Cette page
s'étoffera avec la tranche v1.
:::

## Prérequis

- **SDK .NET 10** (les projets ciblent .NET 10 / C# 14).
- **Node 22+** uniquement si vous voulez builder ce site de documentation ou les packages
  frontend.

## Lancer la CLI depuis les sources

En attendant la publication du tool global, vous pouvez lancer la CLI directement depuis
le dépôt :

```bash
# Lister les items disponibles dans le registre
dotnet run --project src/Outlet.Cli -- list
```

Les autres commandes sont esquissées et seront complétées avec la tranche v1 :

```bash
outlet init   # initialise outlet.json dans votre projet (stub)
outlet add    # copie un item du registre dans votre projet (stub)
```

## Le modèle mental

1. Vous choisissez une **préoccupation** (v1 : email).
2. Vous faites `add` du **contrat** (le port générique + DTOs) — zéro dépendance externe.
3. Vous faites `add` d'un **adapter** pour le provider voulu (par exemple SMTP ou
   SendGrid).
4. Vous le câblez avec l'extension `AddXxx()` de l'adapter dans votre composition root.
5. Pour changer de provider plus tard, vous ajoutez un autre adapter et modifiez une seule
   ligne `AddXxx()` — le port reste identique.

## Builder le tout en local

```bash
dotnet build Outlet.slnx -c Release          # 0 warning attendu
dotnet test Outlet.slnx --filter "Category!=Live"
```

Pour les conventions et règles d'architecture du projet, voir les pages
[Testing strategy](/testing) et [Production readiness](/production-readiness)
(documentation contributeurs, en anglais).
