#!/bin/bash
# SessionStart hook — provisions the .NET 10 SDK so `dotnet build` / `dotnet test`
# work in Claude Code on the web (ephemeral containers ship without the SDK).
# Idempotent and non-interactive; safe to run on every session start.
set -euo pipefail

# Only needed in the remote (web) environment. Local machines already have tooling.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

DOTNET_DIR="$HOME/.dotnet"
DOTNET_CHANNEL="10.0"
PROJECT_DIR="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"

# All logs go to stderr to keep the hook's stdout clean.
log() { echo "[session-start] $*" >&2; }

# Persist tool path + quiet flags for the whole session (when the harness provides
# an env file). Exporting here too covers this hook's own restore step.
export PATH="$DOTNET_DIR:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo "export PATH=\"$DOTNET_DIR:\$PATH\""
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
    echo "export DOTNET_NOLOGO=1"
  } >> "$CLAUDE_ENV_FILE"
fi

# Install the SDK only when the expected major isn't already present.
if dotnet --version 2>/dev/null | grep -q '^10\.'; then
  log ".NET SDK $(dotnet --version) already present — skipping install."
else
  log "Installing .NET SDK (channel $DOTNET_CHANNEL)…"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_DIR" 1>&2
  log ".NET SDK $(dotnet --version) installed."
fi

# Warm the NuGet cache so the first in-session build/test is fast (the container
# snapshot taken after this hook completes keeps the restored packages).
if [ -f "$PROJECT_DIR/Outlet.slnx" ]; then
  log "Restoring NuGet packages…"
  dotnet restore "$PROJECT_DIR/Outlet.slnx" 1>&2
fi

log "Ready."
