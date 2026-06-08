namespace Outlet.Registry.Sms;

/// <summary>
/// Outcome of a send. Expected delivery failures are modelled here rather than
/// thrown, so calling code can branch without try/catch.
/// </summary>
public sealed class SmsResult
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    /// <summary>Provider message id when the send succeeded and one is available.</summary>
    public string? MessageId { get; }

    /// <summary>Human-readable reason when the send failed.</summary>
    public string? Error { get; }

    private SmsResult(bool isSuccess, string? messageId, string? error)
    {
        IsSuccess = isSuccess;
        MessageId = messageId;
        Error = error;
    }

    public static SmsResult Success(string? messageId = null) => new(true, messageId, null);

    public static SmsResult Failure(string error) => new(false, null, error);
}
