using Outlet.Kernel.Shared;

namespace Outlet.Core.Domain.RegistryItems;

/// <summary>
/// The infrastructure concern an item belongs to, e.g. "email", "cache", "storage".
/// One concern groups one contract item plus its interchangeable adapters.
/// </summary>
public sealed class ConcernName : ValueObject
{
    public string Value { get; }

    private ConcernName(string value)
    {
        Value = value;
    }

    public static ConcernName From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ConcernName cannot be empty.", nameof(value));

        if (!value.All(char.IsAsciiLetterLower))
            throw new ArgumentException(
                $"ConcernName '{value}' must be a single lowercase word (a-z).", nameof(value));

        return new ConcernName(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
