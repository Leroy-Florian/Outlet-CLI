using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.UnitTests.Domain;

public sealed class RegistryItemTests
{
    [Fact]
    public void Should_CreateItem_When_InputIsValid()
    {
        var result = RegistryItem.Create(
            RegistryItemId.From("email-smtp"),
            ConcernName.From("email"),
            RegistryItemType.Adapter,
            ["SmtpEmailSender.cs", "SmtpEmailOptions.cs"],
            registryDependencies: [RegistryItemId.From("email-abstractions")],
            nugetDependencies: [PackageDependency.From("MailKit", "4.0.0")]);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value!;
        item.Id.Value.Should().Be("email-smtp");
        item.Concern.Value.Should().Be("email");
        item.Type.Should().Be(RegistryItemType.Adapter);
        item.Files.Should().HaveCount(2);
        item.RegistryDependencies.Should().ContainSingle()
            .Which.Value.Should().Be("email-abstractions");
        item.NugetDependencies.Should().ContainSingle()
            .Which.PackageId.Should().Be("MailKit");
    }

    [Fact]
    public void Should_Fail_When_ItemHasNoFiles()
    {
        var result = RegistryItem.Create(
            RegistryItemId.From("email-smtp"),
            ConcernName.From("email"),
            RegistryItemType.Adapter,
            []);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least one file");
    }

    [Fact]
    public void Should_Fail_When_ContractDeclaresNugetDependencies()
    {
        var result = RegistryItem.Create(
            RegistryItemId.From("email-abstractions"),
            ConcernName.From("email"),
            RegistryItemType.Contract,
            ["IEmailSender.cs"],
            nugetDependencies: [PackageDependency.From("MailKit", "4.0.0")]);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("zero external dependencies");
    }

    [Fact]
    public void Should_RejectId_When_NotKebabCase()
    {
        var act = () => RegistryItemId.From("Email_Smtp");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_RejectConcern_When_NotSingleLowercaseWord()
    {
        var act = () => ConcernName.From("e-mail");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_RejectNamespace_When_NotValidCSharpNamespace()
    {
        var act = () => TargetNamespace.From("My App.Infra");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_BeEqual_When_PackageDependenciesHaveSameComponents()
    {
        var a = PackageDependency.From("MailKit", "4.0.0");
        var b = PackageDependency.From("MailKit", "4.0.0");
        var c = PackageDependency.From("MailKit", "4.1.0");

        a.Should().Be(b);
        a.Should().NotBe(c);
    }
}
