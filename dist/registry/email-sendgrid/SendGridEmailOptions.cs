namespace Outlet.Registry.Email;

/// <summary>Options for the SendGrid adapter, bound via <c>IOptions&lt;SendGridEmailOptions&gt;</c>.</summary>
public sealed class SendGridEmailOptions
{
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Override the API base URL — for EU data residency, an outbound proxy, or pointing
    /// the adapter at a local stub in tests. Null uses SendGrid's default host.
    /// </summary>
    public string? Host { get; set; }
}
