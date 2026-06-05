using Outlet.Kernel.Shared;

namespace Outlet.Core.Domain.RegistryItems;

/// <summary>
/// A valid C# namespace into which copied item files are rewritten
/// (via Roslyn, never find/replace), e.g. "MyApp.Infrastructure.Email".
/// </summary>
public sealed class TargetNamespace : ValueObject
{
    public string Value { get; }

    private TargetNamespace(string value)
    {
        Value = value;
    }

    public static TargetNamespace From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TargetNamespace cannot be empty.", nameof(value));

        var segments = value.Split('.');
        var allValid = segments.All(s =>
            s.Length > 0
            && (char.IsLetter(s[0]) || s[0] == '_')
            && s.All(c => char.IsLetterOrDigit(c) || c == '_'));

        if (!allValid)
            throw new ArgumentException(
                $"TargetNamespace '{value}' is not a valid C# namespace.", nameof(value));

        return new TargetNamespace(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
