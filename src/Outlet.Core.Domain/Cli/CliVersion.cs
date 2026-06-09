using Outlet.Kernel.Shared;

namespace Outlet.Core.Domain.Cli;

/// <summary>
/// The version of the Outlet CLI tool itself, as a comparable semantic version
/// (<c>major.minor.patch</c> with an optional pre-release suffix).
///
/// This is the one place that knows the rule "is version A newer than version B",
/// so the update-check use case can stay free of parsing/comparison details.
/// Build metadata (anything after '+') is ignored, per SemVer precedence rules.
/// </summary>
public sealed class CliVersion : ValueObject
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    /// <summary>The pre-release label (e.g. "beta.1"), or null for a stable release.</summary>
    public string? PreRelease { get; }

    public bool IsPreRelease => PreRelease is not null;

    private CliVersion(int major, int minor, int patch, string? preRelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
    }

    /// <summary>Parses a version string, throwing when it is not a valid version.</summary>
    public static CliVersion From(string value)
        => TryParse(value)
           ?? throw new ArgumentException($"'{value}' is not a valid CLI version.", nameof(value));

    /// <summary>
    /// Best-effort parse: returns null instead of throwing, so an adapter can skip
    /// unrecognised entries coming from an external feed without failing the whole check.
    /// </summary>
    public static CliVersion? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var core = value.Trim();

        // Drop build metadata ('+...'), then split off the pre-release label ('-...').
        var plus = core.IndexOf('+');
        if (plus >= 0)
            core = core[..plus];

        string? preRelease = null;
        var dash = core.IndexOf('-');
        if (dash >= 0)
        {
            preRelease = core[(dash + 1)..];
            core = core[..dash];
            if (preRelease.Length == 0)
                return null;
        }

        // Accept 1–4 components: System.Version (and thus Assembly.GetName().Version)
        // renders as major.minor.build.revision — we keep the first three and ignore
        // the revision. Every present component must still be a non-negative integer.
        var parts = core.Split('.');
        if (parts.Length is < 1 or > 4)
            return null;

        if (!parts.All(p => int.TryParse(p, out var n) && n >= 0))
            return null;

        if (!TryComponent(parts, 0, out var major)
            || !TryComponent(parts, 1, out var minor)
            || !TryComponent(parts, 2, out var patch))
            return null;

        return new CliVersion(major, minor, patch, preRelease);
    }

    /// <summary>True when this version supersedes <paramref name="other"/>.</summary>
    public bool IsNewerThan(CliVersion other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var core = CompareCore(other);
        if (core != 0)
            return core > 0;

        // Same major.minor.patch: a stable release outranks a pre-release of it,
        // and between two pre-releases the labels are compared lexically.
        return ComparePreRelease(other) > 0;
    }

    private int CompareCore(CliVersion other)
    {
        if (Major != other.Major) return Major.CompareTo(other.Major);
        if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
        return Patch.CompareTo(other.Patch);
    }

    private int ComparePreRelease(CliVersion other)
    {
        if (PreRelease is null && other.PreRelease is null) return 0;
        if (PreRelease is null) return 1;
        if (other.PreRelease is null) return -1;
        return string.CompareOrdinal(PreRelease, other.PreRelease);
    }

    private static bool TryComponent(string[] parts, int index, out int value)
    {
        if (index >= parts.Length)
        {
            value = 0;
            return true;
        }

        return int.TryParse(parts[index], out value) && value >= 0;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Major;
        yield return Minor;
        yield return Patch;
        yield return PreRelease;
    }

    public override string ToString()
        => PreRelease is null ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{PreRelease}";
}
