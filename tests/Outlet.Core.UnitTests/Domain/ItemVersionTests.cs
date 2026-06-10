using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.UnitTests.Domain;

public sealed class ItemVersionTests
{
    [Fact]
    public void Should_ParseMajorMinorPatch()
    {
        var version = ItemVersion.From("2.5.9");

        version.Major.Should().Be(2);
        version.Minor.Should().Be(5);
        version.Patch.Should().Be(9);
        version.ToString().Should().Be("2.5.9");
    }

    [Fact]
    public void Should_DefaultToOneZeroZero()
    {
        ItemVersion.Default.ToString().Should().Be("1.0.0");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1.0")]
    [InlineData("1.0.0.0")]
    [InlineData("1.x.0")]
    [InlineData("-1.0.0")]
    public void Should_Throw_When_NotMajorMinorPatch(string value)
    {
        var act = () => ItemVersion.From(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_OrderByPrecedence()
    {
        ItemVersion.From("1.1.0").IsNewerThan(ItemVersion.From("1.0.9")).Should().BeTrue();
        ItemVersion.From("2.0.0").IsNewerThan(ItemVersion.From("1.9.9")).Should().BeTrue();
        ItemVersion.From("1.0.0").IsNewerThan(ItemVersion.From("1.0.1")).Should().BeFalse();
    }

    [Fact]
    public void Should_BeValueEqual_When_SameComponents()
    {
        ItemVersion.From("1.2.3").Should().Be(ItemVersion.From("1.2.3"));
        ItemVersion.From("1.2.3").Should().NotBe(ItemVersion.From("1.2.4"));
    }
}
