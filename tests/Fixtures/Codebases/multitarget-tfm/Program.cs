using System.Text;
using Acme.Multi;
using Microsoft.Extensions.Caching.Memory;

var cache = new MemoryCache(new MemoryCacheOptions());
ICacheStore store = new InMemoryCacheStore(cache);

await store.SetAsync("greeting", Encoding.UTF8.GetBytes("hello-multi"));
var bytes = await store.GetAsync("greeting");

Console.WriteLine(bytes is null ? "CACHE_MISS" : $"CACHE_OK:{Encoding.UTF8.GetString(bytes)}");
