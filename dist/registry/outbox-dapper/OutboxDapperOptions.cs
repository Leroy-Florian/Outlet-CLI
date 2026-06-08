namespace Outlet.Registry.Outbox;

/// <summary>Options for the Dapper outbox adapter, bound via <c>IOptions&lt;OutboxDapperOptions&gt;</c>.</summary>
public sealed class OutboxDapperOptions
{
    /// <summary>
    /// Table the SQL targets, optionally schema-qualified (e.g. <c>"dbo.OutboxMessages"</c>).
    /// The identifier is emitted verbatim into the SQL, so set it from configuration you
    /// control — never from untrusted input.
    /// </summary>
    public string TableName { get; set; } = "OutboxMessages";
}
