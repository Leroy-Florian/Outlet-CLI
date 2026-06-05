using System.Diagnostics;

namespace Outlet.Core.Infrastructure.Projects;

/// <summary>
/// SECONDARY ADAPTER — evaluates a project by shelling out to
/// <c>dotnet msbuild &lt;proj&gt; -getProperty:… -getItem:PackageReference</c>, which
/// returns MSBuild-EVALUATED values (imports, Directory.Build.props and conditions
/// applied) as JSON — never the raw csproj XML.
/// </summary>
public sealed class DotnetMsBuildEvaluator : IMsBuildEvaluator
{
    public async Task<MsBuildEvaluation> EvaluateAsync(string projectFilePath, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(projectFilePath)),
        };

        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(projectFilePath);
        startInfo.ArgumentList.Add("-getProperty:TargetFramework");
        startInfo.ArgumentList.Add("-getProperty:TargetFrameworks");
        startInfo.ArgumentList.Add("-getProperty:RootNamespace");
        startInfo.ArgumentList.Add("-getProperty:ManagePackageVersionsCentrally");
        startInfo.ArgumentList.Add("-getItem:PackageReference");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start 'dotnet msbuild'.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"'dotnet msbuild' failed for '{projectFilePath}' (exit {process.ExitCode}): {stderr}");

        return MsBuildOutputParser.Parse(stdout);
    }
}
