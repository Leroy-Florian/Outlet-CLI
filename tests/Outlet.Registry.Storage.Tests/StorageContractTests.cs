using System.Text;
using Outlet.Registry.Storage;

namespace Outlet.Registry.Storage.Tests;

public sealed class StorageContractTests
{
    [Fact]
    public void Should_DefaultMetadataToEmpty_When_OnlyKeyIsSet()
    {
        var info = new StorageObjectInfo { Key = "k" };

        info.Size.Should().Be(0);
        info.ContentType.Should().BeNull();
        info.ETag.Should().BeNull();
        info.LastModified.Should().BeNull();
        info.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Should_DefaultPutOptionsToEmptyMetadata()
    {
        var options = new PutObjectOptions();

        options.ContentType.Should().BeNull();
        options.Metadata.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_DisposeContentStream_When_StorageObjectIsDisposed()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("payload"));
        var storageObject = new StorageObject(new StorageObjectInfo { Key = "k" }, stream);

        await storageObject.DisposeAsync();

        stream.CanRead.Should().BeFalse("disposing the object must release its content stream");
    }

    [Fact]
    public void Should_ExposeInfoAndContent()
    {
        var info = new StorageObjectInfo { Key = "report.csv", Size = 3, ContentType = "text/csv" };
        using var storageObject = new StorageObject(info, new MemoryStream([1, 2, 3]));

        storageObject.Info.Should().BeSameAs(info);
        storageObject.Content.Length.Should().Be(3);
    }
}
