namespace Outlet.Registry.Email;

/// <summary>Options for the SendGrid adapter, bound via <c>IOptions&lt;SendGridEmailOptions&gt;</c>.</summary>
public sealed class SendGridEmailOptions
{
    public string ApiKey { get; set; } = "";
}
