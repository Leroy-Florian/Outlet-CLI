using System.Text;
using Outlet.Registry.Storage;

namespace Outlet.Registry.Storage.Tests.Conformance;

/// <summary>A configured <see cref="IBlobStorage"/> wired to its backing store for one test.</summary>
public interface IBlobStorageHarness : IAsyncDisposable
{
    IBlobStorage Storage { get; }
}

/// <summary>
/// Reusable PORT conformance suite — "every IBlobStorage must behave this way". Each adapter
/// runs it against its own backing store (in-memory, a temp directory, MinIO, Azurite…), so
/// swappability is TESTED, not asserted.
/// </summary>
public abstract class ObjectStorageConformanceTests
{
    /// <summary>
    /// Builds a harness, or returns null to SKIP (the backing store is unavailable — e.g. a
    /// cloud emulator whose endpoint env var is not set in this run). Hermetic adapters never
    /// return null; cloud adapters return null when their Live infrastructure is absent.
    /// </summary>
    protected abstract Task<IBlobStorageHarness?> CreateOrSkipAsync();

    [Fact]
    public async Task Should_StoreAndRetrieve_When_ObjectIsPut()
    {
        await using var harness = await CreateOrSkipAsync();
        if (harness is null)
            return;
        var payload = Bytes("the payload");

        await harness.Storage.PutAsync("docs/readme.txt", new MemoryStream(payload));

        await using var fetched = await harness.Storage.GetAsync("docs/readme.txt");
        fetched.Should().NotBeNull();
        (await ReadAllAsync(fetched!)).Should().Equal(payload);
        fetched!.Info.Key.Should().Be("docs/readme.txt");
        fetched.Info.Size.Should().Be(payload.Length);
    }

    [Fact]
    public async Task Should_ReturnNull_When_GettingMissingKey()
    {
        await using var harness = await CreateOrSkipAsync();
        if (harness is null)
            return;

        var fetched = await harness.Storage.GetAsync("does/not/exist");

        fetched.Should().BeNull();
    }

    [Fact]
    public async Task Should_ReportExistence_AcrossPutAndDelete()
    {
        await using var harness = await CreateOrSkipAsync();
        if (harness is null)
            return;

        (await harness.Storage.ExistsAsync("k")).Should().BeFalse();

        await harness.Storage.PutAsync("k", new MemoryStream(Bytes("v")));
        (await harness.Storage.ExistsAsync("k")).Should().BeTrue();

        (await harness.Storage.DeleteAsync("k")).Should().BeTrue();
        (await harness.Storage.ExistsAsync("k")).Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnFalse_When_DeletingMissingKey()
    {
        await using var harness = await CreateOrSkipAsync();
        if (harness is null)
            return;

        var deleted = await harness.Storage.DeleteAsync("never/created");

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task Should_OverwriteExisting_When_PuttingSameKeyTwice()
    {
        await using var harness = await CreateOrSkipAsync();
        if (harness is null)
            return;

        await harness.Storage.PutAsync("k", new MemoryStream(Bytes("first")));
        await harness.Storage.PutAsync("k", new MemoryStream(Bytes("second")));

        await using var fetched = await harness.Storage.GetAsync("k");
        Encoding.UTF8.GetString(await ReadAllAsync(fetched!)).Should().Be("second");
    }

    [Fact]
    public async Task Should_RoundTripContentTypeAndMetadata()
    {
        await using var harness = await CreateOrSkipAsync();
        if (harness is null)
            return;
        var options = new PutObjectOptions
        {
            ContentType = "application/pdf",
            Metadata = new Dictionary<string, string> { ["category"] = "invoice" },
        };

        await harness.Storage.PutAsync("invoices/march.pdf", new MemoryStream(Bytes("%PDF")), options);

        await using var fetched = await harness.Storage.GetAsync("invoices/march.pdf");
        fetched!.Info.ContentType.Should().Be("application/pdf");
        fetched.Info.Metadata.Should().Contain(new KeyValuePair<string, string>("category", "invoice"));
    }

    [Fact]
    public async Task Should_ListKeys_FilteredByPrefix()
    {
        await using var harness = await CreateOrSkipAsync();
        if (harness is null)
            return;
        await harness.Storage.PutAsync("logs/a.txt", new MemoryStream(Bytes("a")));
        await harness.Storage.PutAsync("logs/b.txt", new MemoryStream(Bytes("b")));
        await harness.Storage.PutAsync("images/c.png", new MemoryStream(Bytes("c")));

        var keys = new List<string>();
        await foreach (var info in harness.Storage.ListAsync("logs/"))
            keys.Add(info.Key);

        keys.Should().BeEquivalentTo(["logs/a.txt", "logs/b.txt"]);
    }

    [Fact]
    public async Task Should_HonorCancellation_OnPut()
    {
        await using var harness = await CreateOrSkipAsync();
        if (harness is null)
            return;
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await harness.Storage.PutAsync("k", new MemoryStream(Bytes("v")), null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private static async Task<byte[]> ReadAllAsync(StorageObject obj)
    {
        using var buffer = new MemoryStream();
        await obj.Content.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
