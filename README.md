# Outlet

**A copy-paste registry for .NET backend infrastructure — shadcn/ui, but for ports & adapters.**

For each infrastructure concern (email, cache, resilience, storage…) Outlet exposes a
**generic port** (a minimal business interface) and several **interchangeable adapters**
(one per provider/library). You **copy the code into your repo and own it**: swap providers
behind the same port, and edit the code freely.

> Metaphor: a *port* (an outlet on the wall) into which you *plug* a provider.

This is **not** a library you depend on at runtime. Uninstalling Outlet breaks nothing in
your project — you keep the code.

## Why

- **Ownership** — the code lives in your repo, no runtime dependency on Outlet.
- **Swappability** — a generic, identical port across adapters; changing provider is ideally a
  one-line DI change.
- **Honest manifests** — every registry item is real, compiled and tested code.

## Quickstart

```bash
# 1. Install the CLI as a global dotnet tool
dotnet tool install -g Outlet.Cli      # then invoke it simply as: outlet

# 2. Initialize outlet.json in your project (detects your project + namespace)
outlet init

# 3. Add an item and its dependencies (copies the code into your project)
outlet add email-smtp

# 4. List what a registry offers
outlet list
```

> Not published to NuGet.org yet? Install from a local pack:
> ```bash
> dotnet pack src/Outlet.Cli -c Release -o ./nupkg
> dotnet tool install -g Outlet.Cli --add-source ./nupkg
> ```

Then wire it up — and swap providers with a single line:

```csharp
// SMTP today…
services.AddSmtpEmail(o => { o.Host = "smtp.example.com"; o.Port = 587; });

// …SendGrid tomorrow — one line changes, the rest of your code is untouched:
services.AddSendGridEmail(o => o.ApiKey = config["SendGrid:ApiKey"]);

// Application code only ever sees the generic port:
var result = await emailSender.SendAsync(message);
```

See the runnable demo: [`samples/SwapDemo`](samples/SwapDemo) ·
[how to run](samples/README.md). Or the interactive **playground** ("Swagger UI for infra
ports"): [`playground/`](playground/README.md) — pick an adapter, fill its options, send,
and watch the swap behind one `IEmailSender`.

## What's in the registry today

Concern **email** (`registry/email/`):

| Item | Type | Provider | DI |
|---|---|---|---|
| `email-abstractions` | contract | — (zero dependency) | — |
| `email-smtp` | adapter | MailKit | `AddSmtpEmail(...)` |
| `email-sendgrid` | adapter | SendGrid | `AddSendGridEmail(...)` |

## How it works

The `outlet` CLI is a thin front-end over a reusable core engine:

1. **detect** the target environment (mono/multi-project, CPM, TFMs, existing refs) from
   *evaluated* MSBuild values;
2. **resolve** an item and its registry dependencies;
3. **fetch** manifests + files from one or more HTTP registries (multi-source by design);
4. **rewrite** the registry namespace into your project's namespace with Roslyn (never
   find/replace);
5. **write** the files to the routed target and add direct NuGet packages (CPM-aware, floor
   versions, conflicts reported never overwritten);
6. **lock** what was installed in `outlet.json`.

## Project layout

```
registry/<concern>/<item>/   real, compilable, tested code (source of truth)
registry/.../*.registry.json  explicit per-item manifest (validated against the JSON schema)
src/Outlet.Core.{Domain,Application,Infrastructure}/   the engine (hexagonal + DDD)
src/Outlet.Cli/              the `outlet` dotnet tool
samples/SwapDemo/            the one-line swap demo
tests/                       unit, infrastructure, registry and architecture tests
```

## Build & test

```bash
dotnet build Outlet.slnx -c Release          # 0 warning expected
dotnet test  Outlet.slnx --filter "Category!=Live"
dotnet run   --project samples/SwapDemo -- smtp
```

## Status

MVP in progress: the email concern (contract + SMTP + SendGrid adapters), the install
engine (resolve · fetch · rewrite · write · NuGet/CPM · lockfile) and the `init`/`add`/`list`
CLI are in place and tested. Roadmap (TFM compatibility matrix, registry publishing, the
interactive playground, production-readiness) is tracked in Linear.
