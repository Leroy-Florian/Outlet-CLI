using System.Text.RegularExpressions;
using Outlet.Core.Application.Ports;

namespace Outlet.Core.Infrastructure.Projects;

/// <summary>
/// Turns the output of <c>dotnet restore</c> into a <see cref="RestoreResult"/>:
/// success is the process exit code, and any <c>NUxxxx</c> diagnostic (e.g. NU1107
/// version conflict, NU1605 downgrade) is extracted, de-duplicated and surfaced.
/// Pure, so the shape of NuGet's output is pinned by hermetic tests.
/// </summary>
public static partial class RestoreOutputParser
{
    public static RestoreResult Parse(int exitCode, string standardOutput, string standardError)
    {
        var diagnostics = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in $"{standardOutput}\n{standardError}".Split('\n'))
        {
            var match = DiagnosticPattern().Match(line);
            if (!match.Success)
                continue;

            var diagnostic = $"{match.Groups["code"].Value}: {match.Groups["message"].Value.Trim()}";
            if (seen.Add(diagnostic))
                diagnostics.Add(diagnostic);
        }

        return new RestoreResult(exitCode == 0, diagnostics);
    }

    // Matches MSBuild/NuGet diagnostic lines like
    // "App.csproj : error NU1107: Version conflict detected for ...".
    [GeneratedRegex(@"(?:error|warning)\s+(?<code>NU\d+):\s*(?<message>.+)", RegexOptions.IgnoreCase)]
    private static partial Regex DiagnosticPattern();
}
