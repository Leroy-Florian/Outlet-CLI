using FluentAssertions;
using Outlet.Core.Domain.Cli;

namespace Outlet.Core.UnitTests.Domain;

public sealed class CliVersionTests
{
    [Theory]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("0.1.0", 0, 1, 0)]
    [InlineData("2", 2, 0, 0)]
    [InlineData("2.5", 2, 5, 0)]
    [InlineData("1.2.3.4", 1, 2, 3)]
    public void Should_ParseComponents_When_StringIsValid(string value, int major, int minor, int patch)
    {
        var version = CliVersion.From(value);

        version.Major.Should().Be(major);
        version.Minor.Should().Be(minor);
        version.Patch.Should().Be(patch);
        version.IsPreRelease.Should().BeFalse();
    }

    [Fact]
    public void Should_CaptureLabel_When_VersionIsPreRelease()
    {
        var version = CliVersion.From("1.0.0-beta.1");

        version.IsPreRelease.Should().BeTrue();
        version.PreRelease.Should().Be("beta.1");
        version.ToString().Should().Be("1.0.0-beta.1");
    }

    [Fact]
    public void Should_IgnoreBuildMetadata_When_Present()
    {
        var version = CliVersion.From("1.0.0+build.42");

        version.IsPreRelease.Should().BeFalse();
        version.ToString().Should().Be("1.0.0");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1.x.0")]
    [InlineData("-1.0.0")]
    [InlineData("1.0.0-")]
    public void Should_ReturnNull_When_StringIsNotAVersion(string value)
        => CliVersion.TryParse(value).Should().BeNull();

    [Theory]
    [InlineData("1.0.1", "1.0.0")]
    [InlineData("1.1.0", "1.0.9")]
    [InlineData("2.0.0", "1.9.9")]
    public void Should_BeNewer_When_CoreVersionIsHigher(string candidate, string baseline)
        => CliVersion.From(candidate).IsNewerThan(CliVersion.From(baseline)).Should().BeTrue();

    [Fact]
    public void Should_NotBeNewer_When_VersionsAreEqual()
        => CliVersion.From("1.2.3").IsNewerThan(CliVersion.From("1.2.3")).Should().BeFalse();

    [Fact]
    public void Should_RankStableAboveItsPreRelease()
    {
        CliVersion.From("1.0.0").IsNewerThan(CliVersion.From("1.0.0-rc.1")).Should().BeTrue();
        CliVersion.From("1.0.0-rc.1").IsNewerThan(CliVersion.From("1.0.0")).Should().BeFalse();
    }

    [Fact]
    public void Should_BeValueEqual_When_ComponentsMatch()
        => CliVersion.From("1.2.3").Should().Be(CliVersion.From("1.2.3"));

    [Fact]
    public void Should_Throw_When_FromGetsInvalidString()
    {
        var act = () => CliVersion.From("not-a-version");

        act.Should().Throw<ArgumentException>();
    }
}
