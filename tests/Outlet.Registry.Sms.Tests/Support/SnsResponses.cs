namespace Outlet.Registry.Sms.Tests.Support;

/// <summary>Canned Amazon SNS XML payloads the HTTP stub replays so the AWS SDK can unmarshal them.</summary>
public static class SnsResponses
{
    private const string Namespace = "http://sns.amazonaws.com/doc/2010-03-31/";

    public static string PublishSuccess(string messageId) => $"""
        <?xml version="1.0"?>
        <PublishResponse xmlns="{Namespace}">
          <PublishResult>
            <MessageId>{messageId}</MessageId>
          </PublishResult>
          <ResponseMetadata>
            <RequestId>00000000-0000-0000-0000-0000000000aa</RequestId>
          </ResponseMetadata>
        </PublishResponse>
        """;

    public static string Error(string code, string message) => $"""
        <?xml version="1.0"?>
        <ErrorResponse xmlns="{Namespace}">
          <Error>
            <Type>Sender</Type>
            <Code>{code}</Code>
            <Message>{message}</Message>
          </Error>
          <RequestId>00000000-0000-0000-0000-0000000000bb</RequestId>
        </ErrorResponse>
        """;
}
