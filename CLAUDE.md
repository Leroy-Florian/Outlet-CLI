# Outlet — Guide projet (CLAUDE.md)

> Nom de travail du repo : `HexaKit`. **Nom produit retenu : `Outlet`** (rename non encore effectué — cf. Linear HIJ-486).

## Ce qu'est Outlet

Un **registre de code « copier-coller » à la shadcn/ui, mais pour l'infrastructure backend en .NET** (envoi d'email, cache, résilience, storage…).

Idée centrale : pour chaque **préoccupation**, on expose un **port générique** (interface métier minimale) et plusieurs **adapters interchangeables** (un par provider/lib). L'utilisateur **copie le code dans son repo et le possède** : il peut changer de provider derrière le même port et éditer le code librement.

**Ce n'est PAS une librairie consommée en dépendance.** C'est du code qu'on s'approprie. Métaphore du nom : une *prise* (port) dans laquelle on *branche* un provider (adapter).

## État actuel (important)

Le repo est à l'**amorçage** : un projet console `HexaKit` quasi vide (`Program.cs` = Hello World), l'outillage GitHub Spec Kit (`.specify/`, templates non remplis), et la maquette du playground (`docs/playground-mockup.html`). **Aucun code de domaine Outlet n'existe encore.** Le cadrage complet vit dans Linear (voir plus bas).

## Principes de design (décisions verrouillées)

1. **Ownership / copier-coller** : aucune dépendance runtime à Outlet. Désinstaller Outlet ne casse rien chez l'utilisateur.
2. **Séparation contrat / adapter** : le contrat (port + DTOs) a **zéro dépendance externe** et vit côté Application/Domain ; l'adapter dépend du contrat + de la lib provider. L'app ne référence jamais l'adapter concret.
3. **Port générique minimal et identique** entre adapters → c'est ce qui garantit la swappabilité. Aucune spécificité provider dans le port générique.
4. **Spécifique à côté du générique** : une feature provider passe par une 2e interface dédiée (même classe adapter) ou par l'édition de la copie — jamais dans le port commun.
5. **DI explicite** : chaque adapter fournit `AddXxx()`. Pas de scan d'assembly. Générique + spécifique → **une seule instance concrète** forwardée vers plusieurs interfaces.
6. **Triptyque d'adapter** : classe adapter + classe d'Options (`IOptions<T>`) + extension `AddXxx`.
7. **Modèles génériques = cas commun (~80 %)** + soupape (édition de la copie), pas de god-model.
8. **Discipline de scope** : une préoccupation à la fois. **Cible v1 = email uniquement** (port + 2 adapters).
9. **Adapters minces** : la résilience (retry/CB) est une préoccupation séparée (Polly) composée *par-dessus* le port, jamais embarquée dans l'adapter.

## Architecture décidée

- **Distribution** : **registre distant** (manifeste + fichiers servis en HTTP). Conçu **multi-sources** dès le départ → registres privés d'entreprise possibles (auth = post-v1).
- **Front-end** : **CLI uniquement** au v1 (`dotnet tool` global), au-dessus d'un **core engine** réutilisable. MCP différé.
- **Manifeste** : un fichier JSON **explicite par item** (`*.registry.json`), validé + généré en CI. Champs : `name`, `type` (`outlet:contract` | `outlet:adapter`), `targetFrameworks`/`minTfm`, `registryDependencies`, `nugetDependencies` (`{id, version}`), `files`.
- **Config projet utilisateur** : `outlet.json` (équivalent `components.json` de shadcn) — `registries`, `targets` (routage par type d'item), `installed` (lockfile).
- **Réécriture de namespace** : via **Roslyn** (`CSharpSyntaxRewriter`), pas de find/replace.
- **Routage** : mono-projet par défaut ; multi-projets hexagonal optionnel (contrat → Application/Domain, adapter + NuGet → Infrastructure).
- **NuGet** : deps **directes uniquement** (transitifs laissés au resolver), version **plancher** (pas de verrou `[x]`), conflit direct → avertir. **Détection CPM** (`Directory.Packages.props`) → version au central + `PackageReference` versionless.
- **Détection d'environnement (preflight)** : lire les valeurs **évaluées MSBuild** (`dotnet msbuild -getProperty/-getItem`), jamais le XML brut. Détecte mono/multi, CPM + fichier central gouvernant, TFM par projet, refs existantes.

## Structure cible du monorepo

```
registry/<concern>/<item>/   ← vrai code compilable + testé (source de vérité)
registry/.../<item>.registry.json   ← manifeste explicite
src/Outlet.Core/             ← engine (résolution, fetch, Roslyn, NuGet/CPM, détection)
src/Outlet.Cli/              ← dotnet tool (init, add, list)
dist/registry/               ← manifeste GÉNÉRÉ + publié (consommé en HTTP par la CLI)
samples/                     ← démo (swap SMTP↔SendGrid en 1 ligne de DI)
docs/playground-mockup.html  ← maquette du playground interactif
tests/
```

## Stratégie de tests

- **Outillage (Core/CLI)** : unitaire/intégration, **zéro réseau**.
- **Adapters**, 3 niveaux : (A) frontière HTTP mockée (WireMock/`HttpMessageHandler`) ; (B) protocole réel via émulateur Testcontainers (smtp4dev…) ; (C) provider réel sandbox.
- **Lanes CI** : lane PR = A+B+matrice TFM, **hermétique et rapide, sans secret** (`dotnet test --filter "Category!=Live"`) ; lane planifiée (nightly) = C live, non bloquante.
- **Conformité de port** : suite réutilisable (« tout `IEmailSender` se comporte ainsi ») rejouée par chaque adapter → swappabilité testée.
- **Production-readiness** : tests de concurrence + injection de fautes (Toxiproxy) + débit/alloc (BenchmarkDotNet/NBomber), tous hermétiques.

## Conventions de nommage

- Port : `IEmailSender` · Adapter : `SendGridEmailSender` · Options : `SendGridEmailOptions` · Extension : `AddSendGridEmail()`.
- Items registre : `email-abstractions`, `email-smtp`, `email-sendgrid`. Divergence de majeure provider → items distincts (ex. `resilience-polly-v8`).
- Le swap de provider doit idéalement se résumer à changer une ligne `AddXxx()`.

## Roadmap & suivi

La planification détaillée est dans **Linear** (team Hijoxx) :
- **Projet « Outlet — MVP »** : ~19 issues (HIJ-486 → HIJ-499, HIJ-509/510/512/513/514) — distribution, schéma, contenu email, engine, CLI, compat TFM, tests, production-readiness, playground.
- **Projet « Outlet — Suite / Roadmap »** : update/diff, registres privés, MCP, nouvelles préoccupations (Polly, cache, storage), bot GitHub d'auto-update, audit catalogue (HIJ-515).

## Garde-fous pour toute contribution

- Ne pas introduire de dépendance runtime à Outlet dans le code des items.
- Ne pas polluer un port générique avec une spécificité provider.
- Ne pas élargir le scope au-delà de l'email tant que la tranche v1 n'est pas propre et livrable.
- Tout item registre doit compiler + être testé (le manifeste ne doit jamais mentir).
