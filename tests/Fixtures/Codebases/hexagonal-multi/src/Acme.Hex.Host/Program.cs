using System.Text;
using Acme.Hex;
using Microsoft.Extensions.DependencyInjection;

// The port (ICacheStore) is routed into Acme.Hex.Application and the adapter
// (AddInMemoryCache) into Acme.Hex.Infrastructure — both under the Acme.Hex namespace.
var provider = new ServiceCollection()
    .AddInMemoryCache()
    .BuildServiceProvider();

var store = provider.GetRequiredService<ICacheStore>();

await store.SetAsync("greeting", Encoding.UTF8.GetBytes("hello-hex"));
var bytes = await store.GetAsync("greeting");

Console.WriteLine(bytes is null ? "CACHE_MISS" : $"CACHE_OK:{Encoding.UTF8.GetString(bytes)}");
