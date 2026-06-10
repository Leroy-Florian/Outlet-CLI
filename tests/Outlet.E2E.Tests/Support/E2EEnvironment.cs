namespace Outlet.E2E.Tests.Support;

/// <summary>
/// Shared E2E fixture (one per test collection): locates the repo, publishes the real
/// <c>outlet</c> CLI once, and starts the loopback registry server. Tests then run the
/// published CLI as a subprocess against isolated fixture copies.
/// </summary>
public sealed class E2EEnvironment : IAsyncLifetime
{
    private LocalRegistryServer _registry = null!;
    private string _toolDirectory = null!;

    public string RepoRoot { get; private set; } = null!;

    /// <summary>Absolute path to the published <c>Outlet.Cli.dll</c> (invoked via <c>dotnet</c>).</summary>
    public string CliDll { get; private set; } = null!;

    /// <summary>Loopback registry url to inject into each fixture's outlet.json.</summary>
    public string RegistryUrl => _registry.BaseUrl;

    public async Task InitializeAsync()
    {
        RepoRoot = FindRepoRoot();
        _registry = await LocalRegistryServer.StartAsync(Path.Combine(RepoRoot, "dist", "registry"));

        _toolDirectory = Path.Combine(Path.GetTempPath(), "outlet-e2e-cli-" + Guid.NewGuid().ToString("N"));
        var cliProject = Path.Combine(RepoRoot, "src", "Outlet.Cli", "Outlet.Cli.csproj");

        var publish = await ProcessRunner.RunAsync(
            "dotnet",
            ["publish", cliProject, "-c", "Release", "-o", _toolDirectory],
            RepoRoot);

        if (publish.ExitCode != 0)
            throw new InvalidOperationException($"Failed to publish the Outlet CLI for the E2E suite:\n{publish.Combined}");

        CliDll = Path.Combine(_toolDirectory, "Outlet.Cli.dll");
    }

    public Task<ProcessResult> RunCliAsync(IReadOnlyList<string> args, string workingDirectory)
        => ProcessRunner.RunAsync("dotnet", [CliDll, .. args], workingDirectory);

    public Task<ProcessResult> RunDotnetAsync(IReadOnlyList<string> args, string workingDirectory)
        => ProcessRunner.RunAsync("dotnet", args, workingDirectory);

    public async Task DisposeAsync()
    {
        await _registry.DisposeAsync();

        try
        {
            if (Directory.Exists(_toolDirectory))
                Directory.Delete(_toolDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup of a temp directory
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Outlet.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repo root (no Outlet.slnx above the test binaries).");
    }
}

/// <summary>Binds <see cref="E2EEnvironment"/> to the E2E test collection so it is built once.</summary>
[CollectionDefinition(Name)]
public sealed class E2ECollection : ICollectionFixture<E2EEnvironment>
{
    public const string Name = "e2e";
}
