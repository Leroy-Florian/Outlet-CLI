using Outlet.Core.Application.Ports;

namespace Outlet.Core.Infrastructure.UnitTests.Fakes;

/// <summary>Hand-written in-memory <see cref="IFileSystem"/> — zero disk I/O, no mocking framework.</summary>
public sealed class FakeFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Files => _files;

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        => _files.TryGetValue(path, out var content)
            ? Task.FromResult(content)
            : throw new FileNotFoundException($"No such file: {path}");

    public Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        _files[path] = content;
        return Task.CompletedTask;
    }

    public bool FileExists(string path) => _files.ContainsKey(path);

    public void CreateDirectory(string path) => _directories.Add(path);

    public void DeleteFile(string path) => _files.Remove(path);
}
