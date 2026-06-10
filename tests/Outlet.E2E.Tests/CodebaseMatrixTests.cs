using Outlet.E2E.Tests.Support;

namespace Outlet.E2E.Tests;

/// <summary>
/// The proof matrix: for every codebase archetype, the REAL <c>outlet</c> CLI must
/// install <c>cache-memory</c>, the project must then <b>compile</b>, and it must
/// <b>run</b> (printing the round-tripped cache value). Run across Linux, macOS and
/// Windows by <c>.github/workflows/e2e.yml</c>, this is the cross-OS / any-codebase
/// guarantee in executable form.
/// </summary>
[Collection(E2ECollection.Name)]
[Trait("Category", "E2E")]
public sealed class CodebaseMatrixTests(E2EEnvironment environment)
{
    public static IEnumerable<object?[]> Fixtures =>
    [
        // fixture folder            run project (relative)                                 init?  run TFM    expected stdout
        ["legacy-mono-nocpm", "Acme.Legacy.csproj", true, null, "CACHE_OK:hello-legacy"],
        ["modern-cpm-di", "Acme.Modern.csproj", true, null, "CACHE_OK:hello-modern"],
        ["hexagonal-multi", "src/Acme.Hex.Host/Acme.Hex.Host.csproj", false, null, "CACHE_OK:hello-hex"],
        ["multitarget-tfm", "Acme.Multi.csproj", true, "net10.0", "CACHE_OK:hello-multi"],
    ];

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task Should_install_compile_and_run_on_every_codebase(
        string fixture, string runProject, bool runInit, string? runFramework, string expected)
    {
        var workspace = CopyFixtureToTemp(fixture);
        try
        {
            // `init` detects the environment (target project + namespace); the hexagonal
            // fixture ships a hand-routed outlet.json instead, so it skips init.
            if (runInit)
            {
                var init = await environment.RunCliAsync(["init"], workspace);
                init.ExitCode.Should().Be(0, init.Combined);
            }

            PointRegistryAtLocalServer(workspace);

            var add = await environment.RunCliAsync(["add", "cache-memory"], workspace);
            add.ExitCode.Should().Be(0, add.Combined);

            // The lockfile recorded the adapter and its transitively-resolved contract.
            var lockfile = await File.ReadAllTextAsync(Path.Combine(workspace, "outlet.json"));
            lockfile.Should().Contain("cache-memory").And.Contain("cache-abstractions");

            // Compiles.
            var build = await environment.RunDotnetAsync(["build", runProject], workspace);
            build.ExitCode.Should().Be(0, build.Combined);

            // Runs.
            IReadOnlyList<string> runArgs = runFramework is null
                ? ["run", "--project", runProject, "--no-build"]
                : ["run", "--project", runProject, "-f", runFramework, "--no-build"];
            var run = await environment.RunDotnetAsync(runArgs, workspace);
            run.ExitCode.Should().Be(0, run.Combined);
            run.StdOut.Should().Contain(expected);
        }
        finally
        {
            TryDelete(workspace);
        }
    }

    private string CopyFixtureToTemp(string fixture)
    {
        var source = Path.Combine(environment.RepoRoot, "tests", "Fixtures", "Codebases", fixture);
        var destination = Path.Combine(Path.GetTempPath(), "outlet-e2e-" + Guid.NewGuid().ToString("N"));
        CopyDirectory(source, destination);
        return destination;
    }

    private void PointRegistryAtLocalServer(string workspace)
    {
        var path = Path.Combine(workspace, "outlet.json");
        var json = File.ReadAllText(path)
            .Replace("https://registry.outlet.dev/", environment.RegistryUrl, StringComparison.Ordinal);
        File.WriteAllText(path, json);
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (IsBuildArtifact(relative))
                continue;

            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static bool IsBuildArtifact(string relativePath)
        => relativePath
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj");

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best-effort cleanup of a temp directory
        }
    }
}
