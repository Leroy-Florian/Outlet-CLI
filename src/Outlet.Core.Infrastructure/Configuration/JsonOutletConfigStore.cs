using Outlet.Core.Application.Configuration;
using Outlet.Core.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Infrastructure.Configuration;

/// <summary>
/// SECONDARY ADAPTER — persists <c>outlet.json</c> through the <see cref="IFileSystem"/>
/// port, so use cases (and their tests) stay hermetic. JSON shaping is delegated to
/// <see cref="OutletConfigSerializer"/>.
/// </summary>
public sealed class JsonOutletConfigStore(IFileSystem fileSystem) : IOutletConfigStore
{
    public const string FileName = "outlet.json";

    public bool Exists(string projectDirectory)
        => fileSystem.FileExists(PathFor(projectDirectory));

    public async Task<Result<OutletConfig>> LoadAsync(string projectDirectory, CancellationToken cancellationToken = default)
    {
        var path = PathFor(projectDirectory);
        if (!fileSystem.FileExists(path))
            return Result<OutletConfig>.Failure($"No {FileName} found at '{projectDirectory}'. Run 'outlet init' first.");

        var json = await fileSystem.ReadAllTextAsync(path, cancellationToken);
        return OutletConfigSerializer.Parse(json);
    }

    public async Task SaveAsync(string projectDirectory, OutletConfig config, CancellationToken cancellationToken = default)
    {
        fileSystem.CreateDirectory(projectDirectory);
        await fileSystem.WriteAllTextAsync(PathFor(projectDirectory), OutletConfigSerializer.Serialize(config), cancellationToken);
    }

    private static string PathFor(string projectDirectory)
        => Path.Combine(projectDirectory, FileName);
}
