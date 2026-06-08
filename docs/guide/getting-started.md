# Getting started

::: warning Work in progress
Outlet is at an early stage. The compilable skeleton is in place: `outlet list` works,
while `outlet init` and `outlet add` are currently stubs. The first registry content
(the email item) is still being built. This page will grow with the v1 slice.
:::

## Requirements

- **.NET 10 SDK** (the projects target .NET 10 / C# 14).
- **Node 22+** only if you want to build this documentation site or the frontend
  packages.

## Run the CLI from source

Until the global tool is published, you can run the CLI straight from the repository:

```bash
# List the available registry items
dotnet run --project src/Outlet.Cli -- list
```

The other commands are scaffolded and will be filled in as the v1 slice lands:

```bash
outlet init   # set up outlet.json in your project (stub)
outlet add    # copy a registry item into your project (stub)
```

## The mental model

1. You pick a **concern** (v1: email).
2. You `add` the **contract** (the generic port + DTOs) — zero external dependencies.
3. You `add` an **adapter** for the provider you want (for example SMTP or SendGrid).
4. You wire it up with the adapter's `AddXxx()` extension in your composition root.
5. To switch providers later, you add a different adapter and change one `AddXxx()` line —
   the port stays the same.

## Build everything locally

```bash
dotnet build Outlet.slnx -c Release          # 0 warnings expected
dotnet test Outlet.slnx --filter "Category!=Live"
```

For the project conventions and architecture rules, see the
[Testing strategy](/testing) and [Production readiness](/production-readiness) pages.
