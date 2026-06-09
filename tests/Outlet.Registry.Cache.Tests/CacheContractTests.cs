using Outlet.Registry.Cache;

namespace Outlet.Registry.Cache.Tests;

public sealed class CacheContractTests
{
    [Fact]
    public void Should_HaveNoExpiration_When_OptionsAreNone()
    {
        CacheEntryOptions.None.TimeToLive.Should().BeNull();
    }

    [Fact]
    public void Should_CarryTimeToLive_When_BuiltWithExpiresIn()
    {
        var ttl = TimeSpan.FromMinutes(5);

        var options = CacheEntryOptions.ExpiresIn(ttl);

        options.TimeToLive.Should().Be(ttl);
    }

    [Fact]
    public void Should_ShareOneInstance_When_NoneIsRequestedRepeatedly()
    {
        CacheEntryOptions.None.Should().BeSameAs(CacheEntryOptions.None);
    }
}
