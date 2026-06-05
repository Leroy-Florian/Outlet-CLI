using Outlet.Kernel.Shared;

namespace Outlet.Core.Domain.RegistryItems;

/// <summary>
/// A direct NuGet dependency required by a registry item.
/// Versions are FLOORS (minimum acceptable), never locks ("[x]") —
/// transitive resolution is left to the NuGet resolver.
/// </summary>
public sealed class PackageDependency : ValueObject
{
    public string PackageId { get; }
    public string MinimumVersion { get; }

    private PackageDependency(string packageId, string minimumVersion)
    {
        PackageId = packageId;
        MinimumVersion = minimumVersion;
    }

    public static PackageDependency From(string packageId, string minimumVersion)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            throw new ArgumentException("PackageId cannot be empty.", nameof(packageId));

        if (string.IsNullOrWhiteSpace(minimumVersion))
            throw new ArgumentException("MinimumVersion cannot be empty.", nameof(minimumVersion));

        return new PackageDependency(packageId, minimumVersion);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PackageId;
        yield return MinimumVersion;
    }

    public override string ToString() => $"{PackageId} >= {MinimumVersion}";
}
