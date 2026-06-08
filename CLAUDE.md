# Outlet — Guide projet (CLAUDE.md)

> Nom de travail du repo : `HexaKit`. **Nom produit retenu : `Outlet`** (les projets C# sont déjà nommés `Outlet.*` ; le rename du repo GitHub reste à faire — cf. Linear HIJ-486).

## Ce qu'est Outlet

Un **registre de code « copier-coller » à la shadcn/ui, mais pour l'infrastructure backend en .NET** (envoi d'email, cache, résilience, storage…).

Idée centrale : pour chaque **préoccupation**, on expose un **port générique** (interface métier minimale) et plusieurs **adapters interchangeables** (un par provider/lib). L'utilisateur **copie le code dans son repo et le possède** : il peut changer de provider derrière le même port et éditer le code librement.

**Ce n'est PAS une librairie consommée en dépendance.** C'est du code qu'on s'approprie. Métaphore du nom : une *prise* (port) dans laquelle on *branche* un provider (adapter).

## État actuel

Le **MVP est livré** (engine d'installation + CLI + premier concern `email`) :
- `Outlet.Kernel.Shared` : building blocks DDD + Mediator + Result (porté de WOW, adapté).
- `Outlet.Core.{Domain,Application,Infrastructure}` : langage du domaine + **engine d'installation complet** — résolution de dépendances (DFS, cycles, dédoublonnage), fetch HTTP multi-source, réécriture de namespace par Roslyn, détection d'environnement via valeurs MSBuild évaluées, writer NuGet/CPM (version plancher, conflits avertis), lockfile `outlet.json`, compat TFM. **Adapters réels** (plus de stubs).
- `Outlet.Cli` : dotnet tool `outlet` — `init`, `add`, `list`, `remove`, `diff`, `update` **fonctionnels** (robuste : une faute infra → erreur d'une ligne + exit code non nul).
- Registre : concern **`email` livré** (`email-abstractions`, `email-smtp`, `email-sendgrid`) — vrai code compilé + testé, prouvé bout-en-bout (`outlet add email-sendgrid` → projet généré qui compile et tourne).
- Tests : suite verte (unitaire + intégration hermétique + conformité de port + production-readiness) dont **36 tests d'architecture** (NetArchTest + scans textuels) qui verrouillent toutes les conventions ci-dessous.
- Frontend : packages npm `@outlet/hateoas` et `@outlet/effect-react` (copies locales portées de WOW — une extraction en lib publiée est envisagée).

**Reste à faire = distribution** : publier `Outlet.Cli` sur NuGet.org et héberger `dist/registry/` en HTTP (le contenu existe, le déploiement non) ; compléter la lane nightly Live (clé sandbox provider). Le cadrage complet vit dans Linear (voir Roadmap).

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

## Architecture produit décidée

- **Distribution** : **registre distant** (manifeste + fichiers servis en HTTP). Conçu **multi-sources** dès le départ → registres privés d'entreprise possibles (auth = post-v1).
- **Front-end** : **CLI uniquement** au v1 (`dotnet tool` global), au-dessus d'un **core engine** réutilisable. MCP différé.
- **Manifeste** : un fichier JSON **explicite par item** (`*.registry.json`), validé + généré en CI. Champs : `name`, `type` (`outlet:contract` | `outlet:adapter`), `targetFrameworks`/`minTfm`, `registryDependencies`, `nugetDependencies` (`{id, version}`), `files`.
- **Config projet utilisateur** : `outlet.json` (équivalent `components.json` de shadcn) — `registries`, `targets` (routage par type d'item), `installed` (lockfile).
- **Réécriture de namespace** : via **Roslyn** (`CSharpSyntaxRewriter`), pas de find/replace.
- **Routage** : mono-projet par défaut ; multi-projets hexagonal optionnel (contrat → Application/Domain, adapter + NuGet → Infrastructure).
- **NuGet** : deps **directes uniquement** (transitifs laissés au resolver), version **plancher** (pas de verrou `[x]`), conflit direct → avertir. **Détection CPM** (`Directory.Packages.props`) → version au central + `PackageReference` versionless.
- **Détection d'environnement (preflight)** : lire les valeurs **évaluées MSBuild** (`dotnet msbuild -getProperty/-getItem`), jamais le XML brut. Détecte mono/multi, CPM + fichier central gouvernant, TFM par projet, refs existantes.

## Structure du monorepo

```
registry/<concern>/<item>/          ← vrai code compilable + testé (source de vérité)
registry/.../<item>.registry.json   ← manifeste explicite
src/Kernel.Shared/Outlet.Kernel.Shared/        ← building blocks DDD partagés
src/Kernel.Shared/Outlet.Kernel.Shared.UnitTests/
src/Outlet.Core.Domain/             ← agrégats + VOs de l'engine
src/Outlet.Core.Application/        ← ports + use cases de l'engine
src/Outlet.Core.Infrastructure/     ← adapters (HTTP, Roslyn, MSBuild, NuGet, FS)
src/Outlet.Cli/                     ← dotnet tool `outlet` (init, add, list)
dist/registry/                      ← manifeste GÉNÉRÉ + publié (consommé en HTTP par la CLI)
samples/                            ← démo (swap SMTP↔SendGrid en 1 ligne de DI)
packages/outlet-hateoas/            ← lib front HATEOAS (@outlet/hateoas)
packages/outlet-effect-react/       ← bridge Effect↔React (@outlet/effect-react)
docs/                               ← maquette playground, etc.
tests/Outlet.Core.UnitTests/
tests/Outlet.ArchitectureTests/     ← LE gate : toute convention est testée
```

## Architecture technique (hexagonal + DDD)

### Layering (vérifié par `LayeredArchitectureTests`)

| Couche | Peut dépendre de |
|---|---|
| `Outlet.Kernel.Shared` | rien (jamais du Core) |
| `Outlet.Core.Domain` | Kernel uniquement |
| `Outlet.Core.Application` | Domain + Kernel |
| `Outlet.Core.Infrastructure` | Application + Domain + Kernel |
| `Outlet.Cli` | tout (composition root) |

### Langage du domaine engine

- **Agrégat** : `RegistryItem` (id kebab-case, concern, type contract/adapter, files, deps). Futur : `InstallationPlan` (résolution d'un `add`).
- **Value Objects** : `RegistryItemId`, `ConcernName`, `TargetNamespace`, `PackageDependency` — sealed, ctor privé + factory `From(...)` qui valide.
- **Ports (Application/Ports/)** : `IRegistryClient`, `IProjectInspector`, `INamespaceRewriter`, `IFileSystem`, `INuGetEditor`. Les ports acceptent des VOs, pas des primitives (Tell, Don't Ask).
- **Use cases** : `{Action}{Entity}UseCase` implémentant `IUseCase<TCommand[, TResult]>`, retournent `Result`/`Result<T>`.

### Règles par couche (toutes vérifiées par les tests d'architecture)

**Domain** :
- AUCUNE dépendance technique : pas de HTTP, JSON, DB, logging, Roslyn, MSBuild (`TechnicalDependencyTests`).
- Pas de `DateTime.Now`/`UtcNow` → injecter `ICurrentDateTimeProvider` (`DateTimeProviderConventionTests`).
- Synchrone uniquement, classes `sealed`, pas de setters publics, IDs fortement typés (`SealedAndSynchronousDomainTests`).
- Agrégats : ctor privé + factory statique `Create` retournant l'agrégat ou `Result<T>` (`DddAggregateTests`).
- Les agrégats se référencent par ID, jamais par objet.
- Exceptions domaine `sealed`, suffixe `Exception`.

**Application** :
- Mêmes interdits techniques que Domain (HTTP/JSON/DB/logging/Roslyn/MSBuild).
- Use cases : jamais d'exception pour une erreur métier → `Result`/`Result<T>` (`UseCaseConventionTests`). Le `string` d'erreur est un message/code (pas de TranslationKey : la CLI n'a pas d'i18n).
- Commands/queries = records immuables.
- Pas de logique métier (déléguer au Domain).

**Infrastructure** :
- Implémente les ports, `sealed`, primary constructors.
- C'est ICI (et seulement ici) que vivent HttpClient, System.Text.Json, Roslyn, MSBuild.

**Cli** :
- Composition root : DI explicite (`AddMediator()`, `AddHandlersFromAssembly(...)`, `AddOutletCoreInfrastructure()`).
- Mince : parse args → `IMediator` → mappe `Result` vers stdout/stderr + exit code.

### Style C# (build = gate)

- **.NET 10, C# 14**, `ImplicitUsings`, `Nullable`, `EnforceCodeStyleInBuild` (Directory.Build.props).
- **Collection expressions obligatoires** : IDE0300→0306 en `error` (.editorconfig). `[.. xs.Where(...)]`, jamais `.ToList()` assigné.
- **Primary constructors obligatoires hors Domain** (`PrimaryConstructorConventionTests`). Opt-out rare : commentaire `// non-primary: <raison>` au-dessus du ctor. Ctors privés/protégés (factories) exemptés.
- **CPM** : toute version de package vit dans `Directory.Packages.props`, jamais dans un csproj.
- Interfaces préfixées `I`, naming vérifié par `NamingConventionTests`.

## Stratégie de tests

- **Outillage (Core/CLI)** : unitaire/intégration, **zéro réseau**.
- **Fakes écrits main uniquement** — AUCUN framework de mock (pas de Moq/NSubstitute). Ex. `tests/Outlet.Core.UnitTests/Fakes/FakeRegistryClient.cs`.
- **Nommage** : `Should_<Effet>_When_<Condition>`. Pas de commentaires Given/When/Then — séparation visuelle par lignes vides.
- **Adapters du registre**, 3 niveaux : (A) frontière HTTP mockée (WireMock/`HttpMessageHandler`) ; (B) protocole réel via émulateur Testcontainers (smtp4dev…) ; (C) provider réel sandbox.
- **Lanes CI** : lane PR = A+B+matrice TFM, **hermétique et rapide, sans secret** (`dotnet test --filter "Category!=Live"`) ; lane planifiée (nightly) = C live, non bloquante.
- **Conformité de port** : suite réutilisable (« tout `IEmailSender` se comporte ainsi ») rejouée par chaque adapter → swappabilité testée.
- **Production-readiness** : tests de concurrence + injection de fautes (Toxiproxy) + débit/alloc (BenchmarkDotNet/NBomber), tous hermétiques.
- **Barre qualité** : ≥ 90 % de couverture Domain+Application ; mutation score Stryker ≥ 80 (seuils 80/60/50, `stryker-config.json`). Pas d'exception « c'est du partiel » — un BC inachevé est une régression.
- **Tests d'architecture** = non négociables : ils encodent ce document. Si un test d'archi gêne, on discute de la règle, on ne contourne pas le test.

## Conventions de nommage

- Port : `IEmailSender` · Adapter : `SendGridEmailSender` · Options : `SendGridEmailOptions` · Extension : `AddSendGridEmail()`.
- Items registre : `email-abstractions`, `email-smtp`, `email-sendgrid`. Divergence de majeure provider → items distincts (ex. `resilience-polly-v8`).
- Le swap de provider doit idéalement se résumer à changer une ligne `AddXxx()`.
- Engine : use case `{Action}{Entity}UseCase` · repo impl `EfCore{Entity}Repository` (si persistance un jour) · event `{Entity}{Action}Event` · ID `{Entity}Id`.

## Frontend (futur playground)

npm workspaces à la racine (`package.json`), TypeScript **strict**, Vite, **Vitest**, ESLint flat config :

- **`@outlet/hateoas`** (`packages/outlet-hateoas`) : types `HateoasResource<T>`/`HateoasAction`, helpers `can()`/`actionFor()`, `follow()` (fetch) et `followEffect()` (Effect + Schema). Copie locale portée de WOW — **toute modif doit être répercutée dans WOW** tant que l'extraction en lib publiée n'est pas faite.
- **`@outlet/effect-react`** (`packages/outlet-effect-react`) : `makeEffectHooks({runtime})` → `useEffectQuery`/`useEffectFn`, type `RemoteData<E, A>`.
- Conventions Effect : un seul `ManagedRuntime` partagé, réponses JSON validées par `Schema`, tokens en `Redacted<string>`, annulation via interruption de Fiber.

### Différé (à porter de WOW quand l'API playground existera)

- **HATEOAS backend** : `HateoasLink`/`HateoasLinks`, `IHateoasLinkPolicy<TResponse>`, `HateoasService` + `HateoasEndpointFilter` + `.WithHateoas()`, policies par réponse, routes nommées. Source : `WOW/src/Kernel.Shared/Wow.Kernel.Shared/Hateoas/`.
- Outbox/UoW EF Core, snapshot pattern de persistance : à porter de WOW si une vraie persistance apparaît. Le port `IUnitOfWork` (abstrait) est déjà dans le kernel.

## Roadmap & suivi

La planification détaillée est dans **Linear** (team Hijoxx) :
- **Projet « Outlet — MVP »** : ~19 issues (HIJ-486 → HIJ-499, HIJ-509/510/512/513/514) — distribution, schéma, contenu email, engine, CLI, compat TFM, tests, production-readiness, playground.
- **Projet « Outlet — Suite / Roadmap »** : update/diff, registres privés, MCP, nouvelles préoccupations (Polly, cache, storage), bot GitHub d'auto-update, audit catalogue (HIJ-515).

## Garde-fous pour toute contribution

- Ne pas introduire de dépendance runtime à Outlet dans le code des items.
- Ne pas polluer un port générique avec une spécificité provider.
- Ne pas élargir le scope au-delà de l'email tant que la tranche v1 n'est pas propre et livrable.
- Tout item registre doit compiler + être testé (le manifeste ne doit jamais mentir).
- **Zéro dette** : pas de « noté pour plus tard ». Un finding est corrigé dans la session ou explicitement différé avec ticket. Un refactor inachevé est une régression, pas une étape.

## Workflow de PR

- **Créer des PRs de façon proactive** dès que c'est utile — ne pas attendre une demande explicite.
- **Découper au maximum** : préférer plusieurs petites PRs faciles à suivre et à review plutôt qu'une grosse. 1 session = idéalement plusieurs petites PRs.
- **Chaque PR est indépendante et idempotente** : mergeable seule, dans n'importe quel ordre, sans dépendre d'une autre PR de la session (pas de PRs empilées). Si deux changements ne sont pas séparables proprement, ils vont dans la même PR.

## Commandes utiles

```bash
dotnet build Outlet.slnx -c Release        # 0 warning exigé (IDE03xx = erreurs)
dotnet test Outlet.slnx --filter "Category!=Live"
dotnet run --project src/Outlet.Cli -- list
npm run lint && npm run test && npm run build   # workspaces front
dotnet stryker                              # mutation testing (stryker-config.json)
```
