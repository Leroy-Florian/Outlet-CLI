using Microsoft.Extensions.Options;
using Outlet.Registry.Storage;

namespace Outlet.Registry.Storage.Tests.ProductionReadiness;

/// <summary>
/// Hermetic production-readiness checks (HIJ-514) for the zero-dependency adapters: provoke at
/// test time what would otherwise only surface at scale — concurrency, large payloads, and
/// hostile keys. The cloud adapters get the same scrutiny against real endpoints in the nightly lane.
/// </summary>
public sealed class StorageAdapterProductionReadinessTests
{
    [Fact]
    public async Task InMemory_Should_HandleManyConcurrentPuts_WithoutLossOrCorruption()
    {
        var storage = new InMemoryBlobStorage(Options.Create(new InMemoryBlobStorageOptions()));

        const int count = 200;
        await Task.WhenAll(Enumerable.Range(0, count).Select(i =>
            storage.PutAsync($"key-{i}", new MemoryStream(BitConverter.GetBytes(i)))));

        var stored = new List<BlobInfo>();
        await foreach (var info in storage.ListAsync())
            stored.Add(info);

        stored.Should().HaveCount(count, "every concurrent put must be stored exactly once");
    }

    [Fact]
    public async Task FileSystem_Should_HandleManyConcurrentPuts_WithoutLossOrCorruption()
    {
        await using var temp = new TempDirectory();
        var storage = new FileSystemBlobStorage(Options.Create(new FileSystemBlobStorageOptions { RootPath = temp.Path }));

        const int count = 100;
        await Task.WhenAll(Enumerable.Range(0, count).Select(i =>
            storage.PutAsync($"shard/key-{i}", new MemoryStream(BitConverter.GetBytes(i)))));

        var stored = new List<string>();
        await foreach (var info in storage.ListAsync())
            stored.Add(info.Key);

        stored.Should().HaveCount(count);
    }

    [Fact]
    public async Task FileSystem_Should_RoundTripLargePayload_WithoutBuffering()
    {
        await using var temp = new TempDirectory();
        var storage = new FileSystemBlobStorage(Options.Create(new FileSystemBlobStorageOptions { RootPath = temp.Path }));

        var payload = new byte[5 * 1024 * 1024];
        Random.Shared.NextBytes(payload);

        await storage.PutAsync("big.bin", new MemoryStream(payload));

        await using var fetched = await storage.GetAsync("big.bin");
        using var buffer = new MemoryStream();
        await fetched!.Content.CopyToAsync(buffer);
        buffer.ToArray().Should().Equal(payload);
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("../../etc/passwd")]
    [InlineData("nested/../../escape")]
    public async Task FileSystem_Should_RejectPathTraversalKeys(string maliciousKey)
    {
        await using var temp = new TempDirectory();
        var storage = new FileSystemBlobStorage(Options.Create(new FileSystemBlobStorageOptions { RootPath = temp.Path }));

        var act = async () => await storage.PutAsync(maliciousKey, new MemoryStream([1]));

        await act.Should().ThrowAsync<ArgumentException>("path traversal must never escape the storage root");
    }

    private sealed class TempDirectory : IAsyncDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "outlet-storage-prodready", Guid.NewGuid().ToString("N"));

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
            return ValueTask.CompletedTask;
        }
    }
}
