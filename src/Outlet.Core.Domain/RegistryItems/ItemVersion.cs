using Outlet.Kernel.Shared;

namespace Outlet.Core.Domain.RegistryItems;

/// <summary>
/// The semantic version of a registry item ("major.minor.patch"). Distinct from a NuGet
/// package floor: this versions the SHIPPED item code itself, so 'outlet diff'/'update' can
/// announce that the registry moved an item forward (1.0.0 → 1.1.0) — orthogonal to the
/// content hash, which detects the user's own local edits.
/// </summary>
public sealed class ItemVersion : ValueObject, IComparable<ItemVersion>
{
    public static ItemVersion Default { get; } = new(1, 0, 0);

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    private ItemVersion(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public static ItemVersion From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ItemVersion cannot be empty.", nameof(value));

        var parts = value.Split('.');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out var major)
            || !int.TryParse(parts[1], out var minor)
            || !int.TryParse(parts[2], out var patch)
            || major < 0 || minor < 0 || patch < 0)
        {
            throw new ArgumentException(
                $"ItemVersion '{value}' must be 'major.minor.patch' with non-negative integers.", nameof(value));
        }

        return new ItemVersion(major, minor, patch);
    }

    public int CompareTo(ItemVersion? other)
    {
        if (other is null) return 1;
        if (Major != other.Major) return Major.CompareTo(other.Major);
        if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
        return Patch.CompareTo(other.Patch);
    }

    public bool IsNewerThan(ItemVersion other) => CompareTo(other) > 0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Major;
        yield return Minor;
        yield return Patch;
    }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
