using System.Diagnostics;
using System.Text;

namespace Outlet.E2E.Tests.Support;

/// <summary>Result of a child process: exit code plus captured stdout/stderr.</summary>
public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr)
{
    /// <summary>stdout and stderr concatenated — handy as an assertion failure message.</summary>
    public string Combined => StdOut + StdErr;
}

/// <summary>
/// Runs a child process (the real <c>outlet</c> CLI, or <c>dotnet</c>) and captures its
/// output. Drains stdout/stderr asynchronously so a chatty build can never deadlock the
/// pipe, and caps every call at 10 minutes so a hang fails the test instead of the job.
/// </summary>
public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        // Keep the child hermetic and quiet — no telemetry, no first-run banner.
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

        using var process = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                stderr.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        await process.WaitForExitAsync(linked.Token);

        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
