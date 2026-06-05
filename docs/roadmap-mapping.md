# Mapping Linear ↔ état du repo (Outlet)

> Photo au **2026-06-05**. Croise les 2 projets Linear (team Hijoxx) avec l'état réel du dépôt.
> Source Linear : « Outlet — MVP (email · CLI · registre distant) » + « Outlet — Suite / Roadmap ».

## Légende d'état

| Symbole | Signification |
|---|---|
| ✅ | Fait (au niveau attendu pour cette phase) |
| 🟡 | Partiel : port/stub/échafaudage présent, logique métier manquante |
| ⬜ | À faire : rien dans le repo |

---

## 1. État actuel du repo (résumé)

Le **squelette compilable** est en place (cf. `CLAUDE.md`) :

- `src/Kernel.Shared/Outlet.Kernel.Shared` : building blocks DDD + Mediator + Result.
- `src/Outlet.Core.{Domain,Application,Infrastructure}` : langage du domaine minimal + 5 ports + 1 use case (`ListRegistryItemsUseCase`) + adapters **stubs**.
- `src/Outlet.Cli` : `dotnet tool` `outlet` (`PackAsTool`, `ToolCommandName=outlet`). `list` câblé, `init`/`add` = stubs renvoyant exit code 1.
- `tests/` : `Outlet.Core.UnitTests` + `Outlet.Core.Infrastructure.UnitTests` + `Outlet.Registry.Email.Tests` + `Outlet.ArchitectureTests` (le gate des conventions). **141 tests verts** sur .NET 10.0.300 (47 Kernel + 18 Core + 32 Infra + 8 Registry email + 36 Architecture), build Release **0 warning / 0 erreur**.
- `packages/` : `@outlet/hateoas` + `@outlet/effect-react` (briques front portées de WOW).
- `.github/workflows/ci.yml` : lane .NET (build Release + test `Category!=Live`) + lane front (lint/test/build).
- **Vide** : `registry/`, `samples/`, `docs/` ne contiennent qu'un `README.md`. **Pas de `dist/`.** Aucun contenu de registre, aucun manifeste, aucun playground.

### Ports & adapters — état détaillé

| Port (Application) | Adapter (Infrastructure) | État |
|---|---|---|
| `IRegistryClient` | `MultiSourceRegistryClient` (+ `HttpRegistrySource`) | ✅ fan-out multi-source + transport HTTP (HIJ-492) |
| `IProjectInspector` | `MsBuildProjectInspector` (+ `IMsBuildEvaluator`) | ✅ preflight réel via valeurs MSBuild évaluées (HIJ-510) |
| `INamespaceRewriter` | `RoslynNamespaceRewriter` | 🟡 réécrit les déclarations de namespace (file-scoped + block). Manque : `using` croisés + tests (HIJ-493) |
| `INuGetEditor` | `ProjectNuGetEditor` | 🟡 stub : `throw NotSupported` (HIJ-495) |
| `IFileSystem` | `PhysicalFileSystem` | ✅ implémenté (réel) ; fake main côté tests |

---

## 2. Projet « Outlet — MVP » — mapping des 19 issues

Trié par n° de séquence (01→19). Priorité = priorité Linear.

| # | Issue | Titre court | Prio | État | Ce qu'il reste à faire |
|---|---|---|---|---|---|
| 01 | HIJ-486 | Init monorepo (nom Outlet) | Urgent | ✅ | Rename repo GitHub (`outlet-cli` → `outlet`), vérifier dispo du nom `dotnet tool`/NuGet `outlet` + org GitHub. `dist/` à créer (généré). |
| 02 | HIJ-487 | Schéma manifeste `*.registry.json` + validation | Urgent | ✅ | `registry/registry-item.schema.json` (JSON Schema 2020-12) + `Infrastructure/Manifests` (`RegistryItemManifest` DTO + `RegistryItemManifestSerializer` parse/validate/serialize + mapping vers l'agrégat Domain). 12 tests. **Reste (HIJ-498)** : génération du manifeste agrégé en CI. Champ `concern` ajouté explicitement. |
| 03 | HIJ-488 | Item `email-abstractions` (port + DTOs) | Urgent | ✅ | `IEmailSender`, `EmailMessage`, `EmailAddress`, `EmailAttachment`, `EmailResult` (zéro dépendance) + manifeste. |
| 04 | HIJ-489 | Adapter `email-smtp` (triptyque) | High | ✅ | `SmtpEmailSender` (MailKit 4.16.0) + `SmtpEmailOptions` + `AddSmtpEmail()`, `registryDependency: email-abstractions` + manifeste. |
| 05 | HIJ-490 | Adapter `email-sendgrid` (forwarding DI) | High | ✅ | `SendGridEmailSender` + `ISendGridEmailSender` (templates) forwardés vers une seule instance, `AddSendGridEmail()` + manifeste. Swap = 1 ligne de DI. Compilés + testés par `tests/Outlet.Registry.Email.Tests`. |
| 06 | HIJ-491 | Engine — résolution item + registry-deps | Urgent | ✅ | `ResolveItemDependenciesUseCase` (Application) : DFS post-order, dédoublonnage, détection de cycles → liste ordonnée. 7 tests (diamant, cycle, manquants…). |
| 07 | HIJ-492 | Engine — fetch HTTP + multi-source | Urgent | ✅ | `IRegistrySource` + `HttpRegistrySource` (index `registry.json` + fichiers) + `MultiSourceRegistryClient` (fan-out, premier gagne). 13 tests hermétiques (StubHttpMessageHandler). Sources alimentées par `outlet.json` → HIJ-494. |
| 08 | HIJ-493 | Engine — réécriture namespace Roslyn | Urgent | 🟡 | Déclarations de namespace **faites**. Reste : réécriture des `using` croisés entre fichiers/items + tests dédiés. |
| 09 | HIJ-494 | Config projet `outlet.json` | Urgent | ⬜ | Modèle `registries` / `targets` (routage par type) / `installed` (lockfile) + lecture/écriture. |
| 10 | HIJ-495 | Engine — `PackageReference` + CPM + conflits | Urgent | 🟡 | Port `INuGetEditor` OK ; `ProjectNuGetEditor` = stub. Reste : ajout deps directes (version plancher), détection CPM → version au central + ref versionless, avertissement sur conflit. Dépend de 16. |
| 11 | HIJ-496 | Engine — écriture fichiers + orchestration `add` | High | 🟡 | `IFileSystem`/`PhysicalFileSystem` réels. Reste : use case d'orchestration (résoudre→fetch→rewrite→écrire→NuGet→lockfile), idempotence, collisions de fichiers. |
| 12 | HIJ-497 | CLI `dotnet tool` (`init`/`add`/`list`) | High | 🟡 | Packaging OK, `list` câblé (vide faute de source). Reste : implémenter `init` + `add` (aujourd'hui stubs), `list` contextuel (catalogue × compat TFM). |
| 13 | HIJ-498 | CI — build+test registre + génération/publication manifeste + lanes | High | 🟡 | `ci.yml` build+test + filtre `Category!=Live`. Reste : tests du contenu registre (rien à tester encore), génération+publication du manifeste agrégé (`dist/`), matrice TFM, lane planifiée nightly live. |
| 14 | HIJ-499 | Sample + README — swap SMTP↔SendGrid | High | ⬜ | Appli démo dans `samples/`, swap en 1 ligne de DI, README produit (pitch + quickstart + ownership). Dépend de 03/04/05 + CLI. |
| 15 | HIJ-509 | Compat TFM/.NET — déclaration + pré-check CLI + matrice CI | Urgent | ⬜ | Champ `targetFrameworks` au manifeste (← 02), pré-check CLI avant écriture, matrice CI par TFM. |
| 16 | HIJ-510 | Détection d'environnement (preflight) | Urgent | ✅ | `MsBuildProjectInspector` réel : mono/multi, CPM + fichier central gouvernant, TFMs, refs existantes, via valeurs **évaluées** (`IMsBuildEvaluator` + `dotnet msbuild -getProperty/-getItem`, jamais le XML). Parseur pur testé + inspecteur (workspace temp) + 1 test d'intégration du vrai évaluateur. **Alimente 09/10/12/15.** |
| 17 | HIJ-512 | Stratégie de tests + segmentation CI | Urgent | 🟡 | Posé : filtre `Category!=Live`, `stryker-config.json`, fakes main. Reste : formaliser la stratégie (niveaux A HTTP mocké / B émulateur Testcontainers smtp4dev / C live), suite de **conformité de port** réutilisable, lane nightly non bloquante. |
| 18 | HIJ-513 | Playground interactif d'adapters (démo phare) | High | ⬜ | Maquette `docs/playground-mockup.html` **non committée** (docs/ = README). Playground consommant les adapters via DI, formulaire généré depuis les Options. Briques front présentes (`@outlet/*`). |
| 19 | HIJ-514 | Production-readiness adapters (charge/concurrence/fault-injection) | High | ⬜ | Tests hermétiques : concurrence, injection de fautes (Toxiproxy), débit/alloc (BenchmarkDotNet/NBomber), checklist. Dépend des adapters. |

### Synthèse MVP

- ✅ Fait : **8** (01–07 sauf rien, + 16 ; soit 01, 02, 03, 04, 05, 06, 07, 16).
- 🟡 Partiel : **6** (08, 10, 11, 12, 13, 17). → stubs/CI échafaudés.
- ⬜ À faire : **5** (09, 14, 15, 18, 19).

**Tranche 1 livrée** (2026-06-05) : schéma de manifeste + contenu email complet (contract + 2 adapters).

**Tranche 2 livrée** (2026-06-05) : engine côté lecture — résolution de dépendances (06), fetch HTTP multi-source (07), détection d'environnement (16). Restent pour boucler un `add` de bout en bout : **08** (finir le rewriter Roslyn), **09** (`outlet.json`), **10** (NuGet/CPM), **11** (orchestration), **12** (CLI).

---

## 3. Projet « Outlet — Suite / Roadmap » (post-MVP)

> **Aucune ligne à démarrer tant que la tranche email MVP n'est pas propre et démontrable.** Tous ⬜.

| Issue | Titre court | Prio | Dépend de |
|---|---|---|---|
| HIJ-500 | Commandes `update` / `diff` (code possédé/édité) | High | MVP (lockfile `installed`) |
| HIJ-502 | Registres privés d'entreprise — auth + multi-source avancée | High | 07 (`IRegistrySource`) |
| HIJ-501 | Commande `remove` / `uninstall` | Medium | MVP (lockfile) |
| HIJ-504 | Élargir l'email — adapters Mailgun/SES/MailKit | Medium | 03 (port email) |
| HIJ-505 | Nouvelle préoccupation — résilience (Polly) | Medium | Engine MVP stable |
| HIJ-507 | Découvrabilité — `search`/`browse` + site de docs | Medium | 02 (manifeste généré) |
| HIJ-508 | Versioning des items + politique de compat | Medium | Manifeste + lockfile |
| HIJ-511 | Bot GitHub d'auto-update (PR sur code possédé) | Medium | Lockfile committé + versioning |
| HIJ-515 | Audit catalogue — préoccupations + top 5 packages (doc) | Medium | — (recherche, indépendant) |
| HIJ-503 | Front-end MCP (wrapper core engine) | Low | Core engine |
| HIJ-506 | Nouvelles préoccupations — cache & storage | Low | Après résilience |

---

## 4. Chemin critique proposé (ordonnancement MVP)

Respecte les dépendances + les priorités Linear. Deux fronts parallélisables.

**Front A — Contenu & contrat (peu de dépendances, débloque tout le reste)**
1. **02** schéma manifeste `*.registry.json` + validation.
2. **03** `email-abstractions` (port + DTOs) — premier contenu réel.
3. **04** `email-smtp` puis **05** `email-sendgrid` (gabarit triptyque + swappabilité).

**Front B — Engine (lecture → écriture)**
4. **16** détection d'environnement (preflight) — alimente 07/09/10/12/15.
5. **07** fetch HTTP + `IRegistrySource` (s'appuie sur 02) ; **08** finir le rewriter Roslyn (`using` croisés + tests).
6. **06** résolution + registry-deps (s'appuie sur 02/07).
7. **09** `outlet.json` ; **10** NuGet/CPM (s'appuie sur 16) ; **15** compat TFM (s'appuie sur 02/16).
8. **11** orchestration `add` (assemble 06/07/08/09/10/16).

**Convergence — Front-end & garanties**
9. **12** CLI `init`/`add` + `list` contextuel.
10. **13** CI (manifeste généré/publié + matrice TFM) ; **17** stratégie de tests (niveaux A/B/C + conformité de port) — transversal, à câbler au fil de l'eau.
11. **14** sample + README ; **18** playground ; **19** production-readiness.

> Note transverse : **17 (stratégie de tests)** et **15 (compat TFM)** ne sont pas des étapes terminales — à intégrer dès l'écriture des premiers adapters (04/05) pour éviter la dette.

---

## 5. Observations / écarts notés

- **Nommage config** : ✅ harmonisé sur `outlet.json` (2026-06-05). Le code du repo était déjà sur `outlet.json` ; les références résiduelles `hexakit.json` dans Linear (descriptif projet MVP, HIJ-493, HIJ-500) ont été corrigées. Seule subsiste la note historique volontaire dans `CLAUDE.md` (nom de travail du repo `HexaKit` vs produit `Outlet`, rename GitHub = HIJ-486).
- **`docs/playground-mockup.html`** est référencé comme « commité » par l'issue 18 mais **absent** du repo (`docs/` = README seul).
- **`dist/`** (manifeste généré/publié) n'existe pas encore — normal tant que 02/13 ne sont pas faits.
- **Rename repo GitHub** (`leroy-florian/outlet-cli` → Outlet) reste à faire (issue 01 / note `CLAUDE.md`).
- Environnement de cette session : **.NET 10.0.300 installé** → build Release + 94 tests rejoués et verts.
