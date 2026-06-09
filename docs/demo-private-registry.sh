#!/usr/bin/env bash
# Real end-to-end demo of a PRIVATE Outlet registry pulled by the CLI.
#
# It boots the Outlet Cloud host on local SQLite, then via its HTTP API:
#   registers a user -> creates an org -> publishes an item -> issues a scoped PAT.
# Finally it writes an outlet.json pointing at the org's private registry and runs
# the real `outlet list`, first WITH the token (item is listed) then WITHOUT
# (request is rejected — auth is enforced).
#
# Usage: ./docs/demo-private-registry.sh   (from the repo root, .NET 10 SDK on PATH)
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CLI="$ROOT/src/Outlet.Cli/bin/Release/net10.0/Outlet.Cli.dll"
HOST="$ROOT/src/Outlet.Cloud.Web/bin/Release/net10.0/Outlet.Cloud.Web.dll"
WORK="$(mktemp -d)"
JAR="$WORK/cookies"

dotnet build "$ROOT/Outlet.slnx" -c Release >/dev/null

rm -f "$ROOT/outlet_identity.db" "$ROOT/outlet_cloud.db"
( cd "$ROOT" && Outlet__UseSqlite=true ASPNETCORE_URLS=http://localhost:5280 \
    ASPNETCORE_ENVIRONMENT=Development dotnet "$HOST" >"$WORK/host.log" 2>&1 ) &
HOST_PID=$!
trap 'kill $HOST_PID 2>/dev/null || true' EXIT

for _ in $(seq 1 90); do
  curl -s -o /dev/null http://localhost:5280/organizations/acme/registry.json && break
  sleep 1
done

base=http://localhost:5280
curl -s -c "$JAR" -X POST "$base/auth/register" -H 'content-type: application/json' \
  -d '{"email":"demo@acme.test","password":"Str0ng!pwd","displayName":"Demo"}' >/dev/null
org=$(curl -s -b "$JAR" -c "$JAR" -X POST "$base/organizations" -H 'content-type: application/json' \
  -d '{"slug":"acme","name":"Acme Corp"}' | grep -oE '"organizationId":"[^"]+"' | sed -E 's/.*:"([^"]+)"/\1/')
curl -s -b "$JAR" -X POST "$base/organizations/$org/registry/items" -H 'content-type: application/json' \
  -d '{"name":"email-smtp","manifest":{"name":"email-smtp","type":"outlet:adapter","concern":"email","targetFrameworks":["net10.0"],"registryDependencies":[],"nugetDependencies":[{"id":"MailKit","version":"4.16.0"}],"files":[{"path":"SmtpEmailSender.cs","target":"adapter"}]},"files":[{"path":"SmtpEmailSender.cs","content":"public sealed class SmtpEmailSender {}"}]}' >/dev/null
secret=$(curl -s -b "$JAR" -X POST "$base/organizations/$org/tokens" -H 'content-type: application/json' \
  -d '{"name":"ci"}' | grep -oE '"secret":"[^"]+"' | sed -E 's/.*:"([^"]+)"/\1/')

cat > "$WORK/outlet.json" <<JSON
{
  "registries": [
    { "name": "acme", "url": "$base/organizations/acme/",
      "auth": { "scheme": "Bearer", "tokenEnv": "OUTLET_TOKEN" } }
  ],
  "targets": {
    "contract": { "project": "App.csproj", "namespace": "App" },
    "adapter":  { "project": "App.csproj", "namespace": "App.Infrastructure" }
  },
  "installed": []
}
JSON

cd "$WORK"
echo "==== outlet list  (private registry, with token) ===="
OUTLET_TOKEN="$secret" dotnet "$CLI" list
echo
echo "==== outlet list  (no token -> rejected) ===="
dotnet "$CLI" list || true
