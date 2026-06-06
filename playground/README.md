# Outlet Playground — "Swagger UI for infra ports"

Test a registry **adapter interactively**: pick an adapter, fill its Options, hit send,
and see the raw result + provider response/error. The point is a **living proof of the
port**: swap the adapter, run the same action — it still works.

It consumes the adapters **exactly like a real app**: it registers `AddSmtpEmail(...)` /
`AddSendGridEmail(...)` on a fresh `IServiceCollection`, resolves the generic `IEmailSender`,
and sends. No special-casing.

## Architecture

- **`Outlet.Playground.Api`** — ASP.NET Minimal API. Compiles the `registry/email` sources,
  exposes:
  - `GET /api/catalog` — concerns + adapters + each adapter's **Options schema, reflected
    from the Options class** (secrets flagged → password fields).
  - `POST /api/email/send` — registers the chosen adapter via DI, resolves the port, sends,
    returns `{ success, messageId, error, elapsedMs, adapterType, port }`.
- **`web/`** — React + TypeScript (Vite). Renders the sidebar (Email active; Cache /
  Resilience / Storage shown as "soon"), the adapter picker, the generated Options form,
  the message form, and the result panel.

## Run it

Two terminals:

```bash
# 1. API (http://localhost:5180)
ASPNETCORE_URLS=http://localhost:5180 dotnet run --project playground/Outlet.Playground.Api

# 2. Web (http://localhost:5173, proxies /api to the API)
npm run dev -w @outlet/playground-web
```

Open <http://localhost:5173>, pick **SMTP** or **SendGrid**, fill the options and send.
Against a local [smtp4dev](https://github.com/rnwood/smtp4dev) (`host=localhost port=2525`)
the SMTP send succeeds; with no server you get a clean failure — the port still resolves,
which is the point. **No secret is stored**: credentials live only in the request.

## Scope (v1)

Email only, and the core loop: choose → configure → execute → result, with the swap
demonstrated in the UI. Fault injection (error rate / latency / forced 429) and the Polly
resilience wrap from the mockup are **deferred** (resilience is its own concern, Linear
HIJ-505); the UI is architected to grow into them.
