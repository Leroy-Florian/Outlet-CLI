using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Cache;

// Outlet cache swap demo — same code, swappable provider behind the generic ICacheStore port.
// Run:  dotnet run --project samples/CacheSwapDemo -- memory
//       dotnet run --project samples/CacheSwapDemo -- redis
//       dotnet run --project samples/CacheSwapDemo -- memcached

var provider = args.FirstOrDefault() ?? "memory";

var services = new ServiceCollection();

// ── THE SWAP IS THIS ONE LINE ───────────────────────────────────────────────
// Switch the provider by changing the single AddXxxCache(...) registration.
// Everything below (resolving ICacheStore, set/get round-trip) stays identical.
switch (provider)
{
    case "redis":
        services.AddRedisCache(options =>
            options.Configuration = Environment.GetEnvironmentVariable("REDIS_CONFIGURATION") ?? "localhost:6379");
        break;

    case "memcached":
        services.AddMemcachedCache(options =>
        {
            options.Host = Environment.GetEnvironmentVariable("MEMCACHED_HOST") ?? "localhost";
            options.Port = int.TryParse(Environment.GetEnvironmentVariable("MEMCACHED_PORT"), out var port) ? port : 11211;
        });
        break;

    default:
        services.AddInMemoryCache();
        break;
}
// ────────────────────────────────────────────────────────────────────────────

using var serviceProvider = services.BuildServiceProvider();
var cache = serviceProvider.GetRequiredService<ICacheStore>();

Console.WriteLine($"Provider      : {provider}");
Console.WriteLine($"Active adapter: {cache.GetType().Name}  (behind ICacheStore)");

const string key = "outlet:greeting";
var payload = Encoding.UTF8.GetBytes("Swapping cache providers is a one-line change.");

try
{
    await cache.SetAsync(key, payload, CacheEntryOptions.ExpiresIn(TimeSpan.FromMinutes(5)));
    var roundTrip = await cache.GetAsync(key);

    Console.WriteLine(roundTrip is null
        ? "Cache miss (unexpected)."
        : $"Cached + read back ✅  \"{Encoding.UTF8.GetString(roundTrip)}\"");
}
catch (Exception ex)
{
    Console.WriteLine("Cache backend unavailable (expected without a running server):");
    Console.WriteLine($"  {ex.Message}");
    Console.WriteLine("Use the 'memory' provider for a no-server run, or start Redis / Memcached locally.");
}

return 0;
