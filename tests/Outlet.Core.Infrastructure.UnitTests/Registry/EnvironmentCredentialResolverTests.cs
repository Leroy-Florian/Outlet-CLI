using Outlet.Core.Application.Configuration;
using Outlet.Core.Infrastructure.Registry;

namespace Outlet.Core.Infrastructure.UnitTests.Registry;

public sealed class EnvironmentCredentialResolverTests
{
    [Fact]
    public void Should_ResolveToken_When_EnvVarIsSet()
    {
        Environment.SetEnvironmentVariable("OUTLET_CRED_RESOLVER_TEST", "tok-1");
        try
        {
            new EnvironmentCredentialResolver()
                .Resolve(new RegistryAuth("Bearer", "OUTLET_CRED_RESOLVER_TEST"))
                .Should().Be("tok-1");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OUTLET_CRED_RESOLVER_TEST", null);
        }
    }

    [Fact]
    public void Should_ReturnNull_When_EnvVarIsMissing()
    {
        new EnvironmentCredentialResolver()
            .Resolve(new RegistryAuth("Bearer", "OUTLET_MISSING_VAR_XYZ"))
            .Should().BeNull();
    }
}
