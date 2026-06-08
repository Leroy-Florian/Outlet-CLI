using NetArchTest.Rules;

namespace Outlet.ArchitectureTests;

/// <summary>
/// Each bounded context owns its own model. Contexts communicate by id (and, later,
/// domain events / application orchestration), never by importing another context's
/// types. These tests keep the boundaries honest: the registry engine (Core),
/// identity/access (Identity) and cloud/organizations (Cloud) must not leak into
/// one another at the Domain layer.
/// </summary>
public sealed class BoundedContextIsolationTests : ArchitectureTestBase
{
    [Fact]
    public void Identity_ShouldNot_DependOn_OtherContexts()
    {
        var result = Types.InAssembly(IdentityDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                CloudDomainAssembly.GetName().Name!,
                DomainAssembly.GetName().Name!,
                ApplicationAssembly.GetName().Name!,
                InfrastructureAssembly.GetName().Name!)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Identity context must not depend on Cloud or Core:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Cloud_ShouldNot_DependOn_OtherContexts()
    {
        var result = Types.InAssembly(CloudDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                IdentityDomainAssembly.GetName().Name!,
                DomainAssembly.GetName().Name!,
                ApplicationAssembly.GetName().Name!,
                InfrastructureAssembly.GetName().Name!)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Cloud context must not depend on Identity or Core:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void CoreEngine_ShouldNot_DependOn_CloudOrIdentity()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                IdentityDomainAssembly.GetName().Name!,
                CloudDomainAssembly.GetName().Name!)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"The registry engine (Core) must not depend on Identity or Cloud:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
