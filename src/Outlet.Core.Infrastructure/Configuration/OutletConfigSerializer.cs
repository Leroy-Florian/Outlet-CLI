using System.Text.Json;
using System.Text.Json.Serialization;
using Outlet.Core.Application.Configuration;
using Outlet.Kernel.Shared;

namespace Outlet.Core.Infrastructure.Configuration;

/// <summary>
/// Parses, validates and writes <c>outlet.json</c>. JSON lives only here; parsing
/// returns a <see cref="Result{T}"/> rather than throwing on a malformed file.
/// </summary>
public static class OutletConfigSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Result<OutletConfig> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Result<OutletConfig>.Failure("outlet.json is empty.");

        ConfigJson? raw;
        try
        {
            raw = JsonSerializer.Deserialize<ConfigJson>(json, Options);
        }
        catch (JsonException ex)
        {
            return Result<OutletConfig>.Failure($"outlet.json is not valid JSON: {ex.Message}");
        }

        return raw is null
            ? Result<OutletConfig>.Failure("outlet.json deserialized to null.")
            : Validate(raw);
    }

    public static string Serialize(OutletConfig config)
        => JsonSerializer.Serialize(config, Options);

    private static Result<OutletConfig> Validate(ConfigJson raw)
    {
        if (raw.Targets?.Contract is null || raw.Targets.Adapter is null)
            return Fail("'targets' must define both 'contract' and 'adapter'.");

        var contract = ToRoute(raw.Targets.Contract);
        var adapter = ToRoute(raw.Targets.Adapter);
        if (contract is null || adapter is null)
            return Fail("each target must declare a non-empty 'project' and 'namespace'.");

        var registries = new List<RegistryConfig>();
        foreach (var registry in raw.Registries ?? [])
        {
            if (string.IsNullOrWhiteSpace(registry.Name) || string.IsNullOrWhiteSpace(registry.Url))
                return Fail("each registry must declare a non-empty 'name' and 'url'.");
            if (!Uri.TryCreate(registry.Url, UriKind.Absolute, out _))
                return Fail($"registry '{registry.Name}' has a non-absolute url '{registry.Url}'.");
            registries.Add(new RegistryConfig(registry.Name, registry.Url, registry.Trusted ?? false));
        }

        var installed = new List<InstalledItem>();
        foreach (var item in raw.Installed ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Version))
                return Fail("each installed item must declare a non-empty 'name' and 'version'.");

            installed.Add(new InstalledItem(
                item.Name,
                item.Version,
                [.. (item.Files ?? []).Select(f => new InstalledFile(f.Path ?? "", f.Hash ?? ""))],
                [.. (item.Packages ?? []).Select(p => new InstalledPackage(p.Id ?? "", p.Version ?? ""))],
                [.. item.Dependencies ?? []]));
        }

        return Result<OutletConfig>.Success(new OutletConfig(
            registries,
            new OutletTargets(contract, adapter),
            installed));
    }

    private static TargetRoute? ToRoute(RouteJson route)
        => string.IsNullOrWhiteSpace(route.Project) || string.IsNullOrWhiteSpace(route.Namespace)
            ? null
            : new TargetRoute(route.Project, route.Namespace);

    private static Result<OutletConfig> Fail(string reason)
        => Result<OutletConfig>.Failure($"Invalid outlet.json: {reason}");

    private sealed class ConfigJson
    {
        public List<RegistryJson>? Registries { get; init; }
        public TargetsJson? Targets { get; init; }
        public List<InstalledJson>? Installed { get; init; }
    }

    private sealed class RegistryJson
    {
        public string? Name { get; init; }
        public string? Url { get; init; }

        // Absent ⇒ untrusted: a registry is trusted only when it explicitly says so,
        // so an unknown source can never silently bypass the install gate.
        public bool? Trusted { get; init; }
    }

    private sealed class TargetsJson
    {
        public RouteJson? Contract { get; init; }
        public RouteJson? Adapter { get; init; }
    }

    private sealed class RouteJson
    {
        public string? Project { get; init; }
        public string? Namespace { get; init; }
    }

    private sealed class InstalledJson
    {
        public string? Name { get; init; }
        public string? Version { get; init; }
        public List<FileJson>? Files { get; init; }
        public List<PackageJson>? Packages { get; init; }
        public List<string>? Dependencies { get; init; }
    }

    private sealed class FileJson
    {
        public string? Path { get; init; }
        public string? Hash { get; init; }
    }

    private sealed class PackageJson
    {
        public string? Id { get; init; }
        public string? Version { get; init; }
    }
}
