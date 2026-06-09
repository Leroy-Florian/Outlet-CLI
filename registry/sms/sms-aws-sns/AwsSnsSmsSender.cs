using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Options;

namespace Outlet.Registry.Sms;

/// <summary>
/// Amazon SNS adapter for <see cref="ISmsSender"/>, backed by the AWS SDK. SNS sends an
/// SMS via <c>Publish</c> with a destination phone number. Thin by design: resilience
/// (retry / circuit breaker) is composed over the port, never embedded here.
/// </summary>
public sealed class AwsSnsSmsSender(IOptions<AwsSnsSmsOptions> options) : ISmsSender, IDisposable
{
    private const string SenderIdAttribute = "AWS.SNS.SMS.SenderID";

    private readonly AwsSnsSmsOptions _options = options.Value;
    private readonly IAmazonSimpleNotificationService _sns = BuildClient(options.Value);

    public async Task<SmsResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        var request = new PublishRequest
        {
            PhoneNumber = message.To,
            Message = message.Body,
        };

        var senderId = message.From ?? _options.DefaultSenderId;
        if (!string.IsNullOrWhiteSpace(senderId))
            request.MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                [SenderIdAttribute] = new MessageAttributeValue { DataType = "String", StringValue = senderId },
            };

        try
        {
            var response = await _sns.PublishAsync(request, cancellationToken);
            return SmsResult.Success(response.MessageId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return SmsResult.Failure(Describe(ex));
        }
    }

    public void Dispose() => _sns.Dispose();

    private static IAmazonSimpleNotificationService BuildClient(AwsSnsSmsOptions options)
    {
        var region = string.IsNullOrWhiteSpace(options.Region) ? "us-east-1" : options.Region;
        var config = new AmazonSimpleNotificationServiceConfig();

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
            // SigV4 still needs a region even when pointed at a custom endpoint (proxy / stub).
            config.AuthenticationRegion = region;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
        }

        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
        return new AmazonSimpleNotificationServiceClient(credentials, config);
    }

    private static string Describe(Exception ex)
        => ex is AmazonSimpleNotificationServiceException sns
            ? $"Amazon SNS returned {(int)sns.StatusCode}: {sns.Message}"
            : ex.Message;
}
