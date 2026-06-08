using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Storage;

// Outlet storage swap demo — same code, swappable provider behind the generic IObjectStorage port.
// Run:  dotnet run --project samples/StorageSwapDemo -- memory
//       dotnet run --project samples/StorageSwapDemo -- filesystem
//       dotnet run --project samples/StorageSwapDemo -- s3        (needs an S3-compatible endpoint)
//       dotnet run --project samples/StorageSwapDemo -- azure     (needs a storage connection string)

var provider = args.FirstOrDefault() ?? "memory";

var services = new ServiceCollection();

// ── THE SWAP IS THIS ONE LINE ───────────────────────────────────────────────
// Switch the backend by changing the single AddXxxObjectStorage(...) registration.
// Everything below (resolving IObjectStorage, writing and reading the object) stays identical.
switch (provider)
{
    case "filesystem":
        services.AddFileSystemObjectStorage(o =>
            o.RootPath = Environment.GetEnvironmentVariable("STORAGE_ROOT")
                ?? Path.Combine(Path.GetTempPath(), "outlet-storage-demo"));
        break;

    case "s3":
        services.AddS3ObjectStorage(o =>
        {
            o.BucketName = Environment.GetEnvironmentVariable("S3_BUCKET") ?? "outlet-demo";
            o.ServiceUrl = Environment.GetEnvironmentVariable("S3_SERVICE_URL");
            o.ForcePathStyle = o.ServiceUrl is not null;
            o.Region = Environment.GetEnvironmentVariable("S3_REGION");
        });
        break;

    case "azure":
        services.AddAzureBlobStorage(o =>
        {
            o.ConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") ?? "UseDevelopmentStorage=true";
            o.ContainerName = Environment.GetEnvironmentVariable("AZURE_CONTAINER") ?? "outlet-demo";
            o.CreateContainerIfNotExists = true;
        });
        break;

    default:
        services.AddInMemoryObjectStorage();
        break;
}
// ────────────────────────────────────────────────────────────────────────────

using var serviceProvider = services.BuildServiceProvider();
var storage = serviceProvider.GetRequiredService<IObjectStorage>();

Console.WriteLine($"Provider      : {provider}");
Console.WriteLine($"Active adapter: {storage.GetType().Name}  (behind IObjectStorage)");

const string key = "greetings/hello.txt";
var payload = Encoding.UTF8.GetBytes("Swapping storage providers is a one-line change.");

try
{
    await storage.PutAsync(key, new MemoryStream(payload), new PutObjectOptions { ContentType = "text/plain" });

    await using var fetched = await storage.GetAsync(key);
    if (fetched is null)
    {
        Console.WriteLine($"Put then Get returned nothing for '{key}' — unexpected.");
        return 1;
    }

    using var buffer = new MemoryStream();
    await fetched.Content.CopyToAsync(buffer);

    Console.WriteLine($"Stored + read back ✅  ({fetched.Info.Size} bytes, {fetched.Info.ContentType ?? "no content-type"})");
    Console.WriteLine($"  {Encoding.UTF8.GetString(buffer.ToArray())}");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine("Not completed (expected without a reachable backend / credentials):");
    Console.WriteLine($"  {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine("Try 'memory' or 'filesystem', or point S3_*/AZURE_* at a local MinIO / Azurite.");
    return 0;
}
