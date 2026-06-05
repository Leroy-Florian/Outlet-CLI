namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — file system access used by the engine (writing copied
/// item files, reading/writing outlet.json). Abstracted so use cases stay
/// hermetic in tests (hand-written fake, zero disk I/O).
/// </summary>
public interface IFileSystem
{
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
    Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default);
    bool FileExists(string path);
    void CreateDirectory(string path);
}
