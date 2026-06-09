using System.Text.Json;

namespace Outlet.Cli;

// Shared options for the CLI's machine-readable output (--json): indented so a human can
// still read it, camelCase so it reads like conventional JSON to downstream tooling.
internal static class CliJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
