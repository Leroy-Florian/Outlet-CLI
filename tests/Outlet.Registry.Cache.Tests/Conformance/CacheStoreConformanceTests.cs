using System.Text;
using Outlet.Registry.Cache;

namespace Outlet.Registry.Cache.Tests.Conformance;

/// <summary>A configured <see cref="ICacheStore"/> wired to its backend (real or hermetic).</summary>
public interface ICacheStoreHarness : IAsyncDisposable
{
    ICacheStore Store { get; }
}

/// <summary>
/// Reusable PORT conformance suite — "every ICacheStore must behave this way".
/// Each adapter runs it against its own backend (in-memory in-process, or a real
/// Redis / Memcached server), so swappability is TESTED, not asserted. Keys are
/// uniquely generated per case so the suite is safe to run against a shared server.
/// </summary>
public abstract class CacheStoreConformanceTests
{
    protected abstract Task<ICacheStoreHarness> CreateHarnessAsync();

    [Fact]
    public async Task Should_ReturnNull_When_KeyIsMissing()
    {
        await using var harness = await CreateHarnessAsync();

        var value = await harness.Store.GetAsync(NewKey());

        value.Should().BeNull();
    }

    [Fact]
    public async Task Should_RoundTripValue_When_KeyWasSet()
    {
        await using var harness = await CreateHarnessAsync();
        var key = NewKey();
        var payload = Payload("hello-outlet");

        await harness.Store.SetAsync(key, payload);
        var value = await harness.Store.GetAsync(key);

        value.Should().Equal(payload);
    }

    [Fact]
    public async Task Should_OverwriteValue_When_KeyIsSetTwice()
    {
        await using var harness = await CreateHarnessAsync();
        var key = NewKey();

        await harness.Store.SetAsync(key, Payload("first"));
        await harness.Store.SetAsync(key, Payload("second"));
        var value = await harness.Store.GetAsync(key);

        value.Should().Equal(Payload("second"));
    }

    [Fact]
    public async Task Should_ReturnNull_When_KeyIsRemoved()
    {
        await using var harness = await CreateHarnessAsync();
        var key = NewKey();

        await harness.Store.SetAsync(key, Payload("doomed"));
        await harness.Store.RemoveAsync(key);
        var value = await harness.Store.GetAsync(key);

        value.Should().BeNull();
    }

    [Fact]
    public async Task Should_HonorCancellation()
    {
        await using var harness = await CreateHarnessAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await harness.Store.GetAsync(NewKey(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    protected static string NewKey() => $"outlet:conformance:{Guid.NewGuid():N}";

    protected static byte[] Payload(string text) => Encoding.UTF8.GetBytes(text);
}
