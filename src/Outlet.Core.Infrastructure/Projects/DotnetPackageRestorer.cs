using System.Diagnostics;
using Outlet.Core.Application.Ports;

namespace Outlet.Core.Infrastructure.Projects;

/// <summary>
/// SECONDARY ADAPTER — restores a project's NuGet graph by shelling out to
/// <c>dotnet restore &lt;project&gt;</c>, which downloads the direct packages AND
/// their transitive closure and reports any version-conflict diagnostics (NUxxxx).
/// The exit code drives success; <see cref="RestoreOutputParser"/> extracts the
/// diagnostics for the user.
/// </summary>
public sealed class DotnetPackageRestorer : IPackageRestorer
{
    public async Task<RestoreResult> RestoreAsync(RestoreRequest request, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(request.ProjectFilePath)),
        };

        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(request.ProjectFilePath);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start 'dotnet restore'.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return RestoreOutputParser.Parse(process.ExitCode, await stdoutTask, await stderrTask);
    }
}
