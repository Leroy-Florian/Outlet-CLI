namespace Outlet.Registry.Email;

/// <summary>An e-mail participant: an address with an optional display name.</summary>
public sealed record EmailAddress(string Address, string? DisplayName = null)
{
    public override string ToString()
        => string.IsNullOrWhiteSpace(DisplayName) ? Address : $"{DisplayName} <{Address}>";
}
