namespace Outlet.Registry.Email;

/// <summary>Options for the SMTP adapter, bound via <c>IOptions&lt;SmtpEmailOptions&gt;</c>.</summary>
public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;

    /// <summary>Use STARTTLS upgrade (typical for port 587). Set false for implicit TLS / plain.</summary>
    public bool UseStartTls { get; set; } = true;

    public string? Username { get; set; }
    public string? Password { get; set; }
}
