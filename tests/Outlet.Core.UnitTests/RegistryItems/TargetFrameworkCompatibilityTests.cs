using Outlet.Core.Application.RegistryItems;

namespace Outlet.Core.UnitTests.RegistryItems;

public sealed class TargetFrameworkCompatibilityTests
{
    [Theory]
    [InlineData("net10.0")]   // exact
    [InlineData("net8.0")]    // exact (lowest)
    [InlineData("net11.0")]   // newer than the item's lowest → forward compatible
    public void Should_BeCompatible_When_ProjectFrameworkIsSupported(string projectFramework)
    {
        var reason = TargetFrameworkCompatibility.Check(
            "email-smtp", ["net8.0", "net9.0", "net10.0"], [projectFramework]);

        reason.Should().BeNull();
    }

    [Theory]
    [InlineData("net6.0")]            // older than the item's lowest .NET
    [InlineData("netstandard2.0")]    // not a net5+ TFM and not listed
    [InlineData("net48")]             // .NET Framework
    public void Should_BeIncompatible_When_ProjectFrameworkIsBelowOrUnlisted(string projectFramework)
    {
        var reason = TargetFrameworkCompatibility.Check(
            "email-smtp", ["net8.0", "net9.0", "net10.0"], [projectFramework]);

        reason.Should().NotBeNull();
        reason.Should().Contain("email-smtp").And.Contain(projectFramework);
    }

    [Fact]
    public void Should_RequireEveryFramework_When_ProjectMultiTargets()
    {
        TargetFrameworkCompatibility.Check("x", ["net8.0"], ["net8.0", "net9.0"]).Should().BeNull();
        TargetFrameworkCompatibility.Check("x", ["net8.0"], ["net6.0", "net8.0"]).Should().NotBeNull();
    }

    [Fact]
    public void Should_NotBlock_When_EitherSideDeclaresNoFrameworks()
    {
        TargetFrameworkCompatibility.Check("x", [], ["net10.0"]).Should().BeNull();
        TargetFrameworkCompatibility.Check("x", ["net8.0"], []).Should().BeNull();
    }
}
