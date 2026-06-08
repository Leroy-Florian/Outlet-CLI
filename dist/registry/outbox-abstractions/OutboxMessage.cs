namespace Outlet.Registry.Outbox;

/// <summary>
/// A pending outbound message captured by the transactional outbox — the common
/// ~80% case: a logical <see cref="Type"/>, an already-serialized <see cref="Payload"/>
/// and optional routing metadata. The shape is deliberately flat so it maps to a
/// single table/row across every storage adapter (EF Core, Dapper, …). For richer
/// needs (structured headers, partitioning) edit your owned copy — that is the point.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Stable identity, stamped at creation; the relay uses it to mark dispatch.</summary>
    public Guid Id { get; set; }

    /// <summary>Logical message type/name, e.g. <c>"order.placed"</c>.</summary>
    public required string Type { get; set; }

    /// <summary>Already-serialized body (e.g. JSON) — storage stays format-agnostic.</summary>
    public required string Payload { get; set; }

    /// <summary>Optional routing hint (topic / queue / exchange) for the relay.</summary>
    public string? Destination { get; set; }

    /// <summary>Optional serialized headers/metadata (e.g. JSON), flat for storage.</summary>
    public string? Headers { get; set; }

    /// <summary>When the business event occurred — supplied by the caller (inject your clock).</summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>When the relay published it; <c>null</c> means still pending.</summary>
    public DateTimeOffset? DispatchedAt { get; set; }

    /// <summary>Creates a pending message with a fresh id. Pass the occurrence time explicitly
    /// (no hidden clock) so the value stays testable.</summary>
    public static OutboxMessage Create(
        string type,
        string payload,
        DateTimeOffset occurredAt,
        string? destination = null,
        string? headers = null) => new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            OccurredAt = occurredAt,
            Destination = destination,
            Headers = headers,
        };
}
