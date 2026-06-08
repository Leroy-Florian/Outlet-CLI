namespace Outlet.Registry.Sms;

/// <summary>Options for the Amazon SNS adapter, bound via <c>IOptions&lt;AwsSnsSmsOptions&gt;</c>.</summary>
public sealed class AwsSnsSmsOptions
{
    /// <summary>AWS access key id.</summary>
    public string AccessKeyId { get; set; } = "";

    /// <summary>AWS secret access key.</summary>
    public string SecretAccessKey { get; set; } = "";

    /// <summary>AWS region system name, e.g. "eu-west-1". Defaults to "us-east-1" when unset.</summary>
    public string? Region { get; set; }

    /// <summary>Default SMS sender id where supported (alphanumeric). Overridden per message by <see cref="SmsMessage.From"/>.</summary>
    public string? DefaultSenderId { get; set; }

    /// <summary>
    /// Override the service endpoint — for a VPC endpoint, an outbound proxy, or pointing
    /// the adapter at a local stub in tests. Null uses the regional SNS endpoint.
    /// </summary>
    public string? ServiceUrl { get; set; }
}
