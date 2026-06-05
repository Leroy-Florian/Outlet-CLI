using Outlet.Kernel.Shared;

namespace Outlet.Core.Domain.RegistryItems;

/// <summary>
/// Strongly-typed identity of a registry item, e.g. "email-smtp" or "email-abstractions".
/// Lowercase kebab-case, unique across the registry.
/// </summary>
public sealed class RegistryItemId : ValueObject
{
    public string Value { get; }

    private RegistryItemId(string value)
    {
        Value = value;
    }

    public static RegistryItemId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("RegistryItemId cannot be empty.", nameof(value));

        if (!value.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))
            throw new ArgumentException(
                $"RegistryItemId '{value}' must be lowercase kebab-case (a-z, 0-9, '-').", nameof(value));

        return new RegistryItemId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
