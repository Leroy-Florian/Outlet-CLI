namespace Outlet.Registry.Email;

/// <summary>
/// A simple in-memory attachment — the common ~80% case. For streaming or very
/// large payloads, edit your owned copy (that is the whole point of Outlet).
/// </summary>
public sealed record EmailAttachment(string FileName, string ContentType, ReadOnlyMemory<byte> Content);
