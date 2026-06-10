# Codebase fixtures — the E2E proof matrix

Each folder is a **real, standalone .NET app** representing a codebase archetype Outlet
must handle. They are deliberately NOT part of `Outlet.slnx` and do NOT inherit the repo's
`Directory.Build.props`/`Directory.Packages.props`: the E2E harness
(`tests/Outlet.E2E.Tests`) copies each one to an isolated temp directory, runs the **real**
`outlet` CLI against it, then asserts the result **compiles** and **runs**.

A fixture's `Program.cs` references the cache types *before* they exist — they only appear
after `outlet add cache-memory` copies them in (namespace-rewritten to the project's root
namespace). So a fixture does not build until Outlet has run, by design.

| Fixture | Layout | CPM | DI container | TFM(s) | What it proves |
|---|---|---|---|---|---|
| `legacy-mono-nocpm` | single project | no | no (manual `new`) | net8.0 | inline `<PackageReference>`, hand-wired adapter, older LTS target |
| `modern-cpm-di` | single project | yes | yes (`AddInMemoryCache`) | net10.0 | versionless ref + central `<PackageVersion>`, DI resolution |
| `hexagonal-multi` | Application / Infrastructure / Host | no | yes | net10.0 | type routing (contract → Application, adapter+NuGet → Infrastructure) |
| `multitarget-tfm` | single project | no | no | net8.0;net10.0 | plural `<TargetFrameworks>`, TFM-compat pre-check, multi-TFM compile |

The matrix is run across **Linux, macOS and Windows** by `.github/workflows/e2e.yml`, so the
guarantee is: *whatever the codebase, whatever the OS — `outlet add` lands, the project
compiles, and it runs.*

The runtime proof uses the **in-memory cache** adapter (`cache-memory`) on purpose: it needs
no Docker and no network, so the "it runs" assertion holds identically on all three OSes.
