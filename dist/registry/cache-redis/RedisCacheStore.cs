using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Outlet.Registry.Cache;

/// <summary>
/// Redis adapter implementing both the generic <see cref="ICacheStore"/> and the
/// provider-specific <see cref="IRedisCacheStore"/> from one class — registered once and
/// forwarded to both interfaces (see AddRedisCache). Thin by design: resilience
/// (retry / circuit breaker) is composed over the port, never embedded here. The
/// connection multiplexer is established lazily on first use so registration never
/// touches the network.
/// </summary>
public sealed class RedisCacheStore : IRedisCacheStore, IDisposable, IAsyncDisposable
{
    private readonly RedisCacheOptions _options;
    private readonly Lazy<Task<ConnectionMultiplexer>> _connection;

    // non-primary: the lazy connection factory closes over the configured value (a local),
    // so the multiplexer is built on first use without re-reading IOptions at call time.
    public RedisCacheStore(IOptions<RedisCacheOptions> optionsAccessor)
    {
        _options = optionsAccessor.Value;

        var configuration = _options.Configuration;
        _connection = new Lazy<Task<ConnectionMultiplexer>>(() => ConnectionMultiplexer.ConnectAsync(configuration));
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = await GetDatabaseAsync();
        var value = await database.StringGetAsync(Qualify(key));
        return value.IsNull ? null : (byte[]?)value;
    }

    public async Task SetAsync(string key, byte[] value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = await GetDatabaseAsync();
        await database.StringSetAsync(Qualify(key), value, (options ?? CacheEntryOptions.None).TimeToLive, keepTtl: false);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = await GetDatabaseAsync();
        await database.KeyDeleteAsync(Qualify(key));
    }

    public async Task<long> IncrementAsync(string key, long by = 1, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = await GetDatabaseAsync();
        return await database.StringIncrementAsync(Qualify(key), by);
    }

    // Both sync and async disposal are supported so the adapter is forgiving however the
    // host disposes its container. Only a successfully-established multiplexer is disposed;
    // one that never connected has nothing live to release.
    public void Dispose()
    {
        if (_connection.IsValueCreated && _connection.Value.IsCompletedSuccessfully)
            _connection.Value.Result.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection.IsValueCreated && _connection.Value.IsCompletedSuccessfully)
            await _connection.Value.Result.DisposeAsync();
    }

    private async Task<IDatabase> GetDatabaseAsync() => (await _connection.Value).GetDatabase();

    private string Qualify(string key) => string.IsNullOrEmpty(_options.KeyPrefix) ? key : _options.KeyPrefix + key;
}
