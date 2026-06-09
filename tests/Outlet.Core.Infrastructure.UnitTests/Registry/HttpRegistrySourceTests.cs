using System.Net.Http.Headers;
using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.Infrastructure.Registry;
using Outlet.Core.Infrastructure.UnitTests.Fakes;

namespace Outlet.Core.Infrastructure.UnitTests.Registry;

public sealed class HttpRegistrySourceTests
{
    private const string BaseUrl = "https://registry.test/";
    private const string IndexUrl = "https://registry.test/registry.json";

    private const string IndexJson = """
        {
          "items": [
            {
              "name": "email-abstractions",
              "type": "outlet:contract",
              "concern": "email",
              "targetFrameworks": ["net10.0"],
              "files": [{ "path": "IEmailSender.cs", "target": "contract" }]
            },
            {
              "name": "email-smtp",
              "type": "outlet:adapter",
              "concern": "email",
              "targetFrameworks": ["net10.0"],
              "registryDependencies": ["email-abstractions"],
              "nugetDependencies": [{ "id": "MailKit", "version": "4.16.0" }],
              "files": [{ "path": "SmtpEmailSender.cs", "target": "adapter" }]
            }
          ]
        }
        """;

    private static (HttpRegistrySource Source, StubHttpMessageHandler Handler) Build()
    {
        var handler = new StubHttpMessageHandler();
        var source = new HttpRegistrySource(new HttpClient(handler), new Uri(BaseUrl));
        return (source, handler);
    }

    [Fact]
    public async Task Should_ReturnMappedItems_When_IndexIsServed()
    {
        var (source, handler) = Build();
        handler.Map(IndexUrl, IndexJson);

        var items = await source.GetItemsAsync();

        items.Select(i => i.Id.Value).Should().BeEquivalentTo(["email-abstractions", "email-smtp"]);
        items.Single(i => i.Id.Value == "email-smtp").NugetDependencies
            .Should().ContainSingle().Which.PackageId.Should().Be("MailKit");
    }

    [Fact]
    public async Task Should_ReturnItemById_When_IndexContainsIt()
    {
        var (source, handler) = Build();
        handler.Map(IndexUrl, IndexJson);

        var item = await source.GetItemAsync(RegistryItemId.From("email-smtp"));

        item.Should().NotBeNull();
        item!.Type.Should().Be(RegistryItemType.Adapter);
    }

    [Fact]
    public async Task Should_ReturnNull_When_ItemIsNotInIndex()
    {
        var (source, handler) = Build();
        handler.Map(IndexUrl, IndexJson);

        var item = await source.GetItemAsync(RegistryItemId.From("email-mailgun"));

        item.Should().BeNull();
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_IndexIsAbsent()
    {
        var (source, _) = Build();

        var items = await source.GetItemsAsync();

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_IndexIsInvalidJson()
    {
        var (source, handler) = Build();
        handler.Map(IndexUrl, "{ not json");

        var act = async () => await source.GetItemsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_DownloadFileContent_When_FileExists()
    {
        var (source, handler) = Build();
        handler.Map("https://registry.test/email-smtp/SmtpEmailSender.cs", "public sealed class SmtpEmailSender;");

        var content = await source.GetFileContentAsync(RegistryItemId.From("email-smtp"), "SmtpEmailSender.cs");

        content.Should().Contain("SmtpEmailSender");
    }

    [Fact]
    public async Task Should_Throw_When_FileIsAbsent()
    {
        var (source, _) = Build();

        var act = async () => await source.GetFileContentAsync(RegistryItemId.From("email-smtp"), "Missing.cs");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_AttachAuthorizationHeader_When_Provided()
    {
        var handler = new StubHttpMessageHandler().Map(IndexUrl, IndexJson);
        var source = new HttpRegistrySource(
            new HttpClient(handler),
            new Uri(BaseUrl),
            new AuthenticationHeaderValue("Bearer", "secret-123"));

        await source.GetItemsAsync();

        handler.LastAuthorization.Should().NotBeNull();
        handler.LastAuthorization!.Scheme.Should().Be("Bearer");
        handler.LastAuthorization.Parameter.Should().Be("secret-123");
    }

    [Fact]
    public async Task Should_NotAttachAuthorization_When_Anonymous()
    {
        var handler = new StubHttpMessageHandler().Map(IndexUrl, IndexJson);
        var source = new HttpRegistrySource(new HttpClient(handler), new Uri(BaseUrl));

        await source.GetItemsAsync();

        handler.LastAuthorization.Should().BeNull();
    }
}
