using System.Text;
using Acme.Legacy;
using Microsoft.Extensions.Caching.Memory;

// No DI container: the adapter is constructed by hand. The cache types below
// (ICacheStore, InMemoryCacheStore) do not exist until `outlet add cache-memory`
// copies them into this project under the Acme.Legacy namespace.
var cache = new MemoryCache(new MemoryCacheOptions());
ICacheStore store = new InMemoryCacheStore(cache);

await store.SetAsync("greeting", Encoding.UTF8.GetBytes("hello-legacy"));
var bytes = await store.GetAsync("greeting");

Console.WriteLine(bytes is null ? "CACHE_MISS" : $"CACHE_OK:{Encoding.UTF8.GetString(bytes)}");
