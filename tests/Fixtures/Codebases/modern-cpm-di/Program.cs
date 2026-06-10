using System.Text;
using Acme.Modern;
using Microsoft.Extensions.DependencyInjection;

// DI container: the adapter is wired through the AddInMemoryCache() extension that
// `outlet add cache-memory` copies in (rewritten into the Acme.Modern namespace).
var provider = new ServiceCollection()
    .AddInMemoryCache()
    .BuildServiceProvider();

var store = provider.GetRequiredService<ICacheStore>();

await store.SetAsync("greeting", Encoding.UTF8.GetBytes("hello-modern"));
var bytes = await store.GetAsync("greeting");

Console.WriteLine(bytes is null ? "CACHE_MISS" : $"CACHE_OK:{Encoding.UTF8.GetString(bytes)}");
