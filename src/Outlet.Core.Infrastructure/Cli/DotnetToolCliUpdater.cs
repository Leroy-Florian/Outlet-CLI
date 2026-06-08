using System.Diagnostics;
using Outlet.Core.Application.Cli;
using Outlet.Core.Application.Ports;

namespace Outlet.Core.Infrastructure.Cli;

/// <summary>
/// SECONDARY ADAPTER — performs the self-update by shelling out to
/// <c>dotnet tool update --global &lt;packageId&gt;</c>, the supported way to
/// upgrade a global .NET tool. A non-zero exit is reported as a failed outcome
/// (the use case maps it to a Result failure), not thrown.
/// </summary>
public sealed class DotnetToolCliUpdater(string packageId) : ICliUpdater
{
    public async Task<CliUpdateOutcome> UpdateAsync(CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("tool");
        startInfo.ArgumentList.Add("update");
        startInfo.ArgumentList.Add("--global");
        startInfo.ArgumentList.Add(packageId);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start 'dotnet tool update'.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();

        if (process.ExitCode == 0)
            return new CliUpdateOutcome(Succeeded: true, stdout.Length > 0 ? stdout : $"{packageId} is up to date.");

        var detail = stderr.Length > 0 ? stderr : stdout;
        return new CliUpdateOutcome(
            Succeeded: false,
            detail.Length > 0 ? detail : $"'dotnet tool update' failed (exit {process.ExitCode}).");
    }
}
