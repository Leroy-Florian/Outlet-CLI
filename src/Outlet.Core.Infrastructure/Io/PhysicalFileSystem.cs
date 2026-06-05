using Outlet.Core.Application.Ports;

namespace Outlet.Core.Infrastructure.Io;

/// <summary>
/// SECONDARY ADAPTER — direct file system access for real CLI runs.
/// Tests use a hand-written in-memory fake of <see cref="IFileSystem"/> instead.
/// </summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        => File.ReadAllTextAsync(path, cancellationToken);

    public Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(path, content, cancellationToken);

    public bool FileExists(string path) => File.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
}
