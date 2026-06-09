using System.Reflection;
using NetArchTest.Rules;

namespace Outlet.ArchitectureTests;

/// <summary>
/// Each bounded context owns its own model. Contexts communicate by id (and, later,
/// domain events / composition-root orchestration), never by importing another
/// context's types. These tests keep the boundaries honest across BOTH the Domain
/// and Application layers — in particular, issuing a scoped token must not turn into
/// an Identity→Cloud dependency: scopes cross the boundary as plain strings.
/// </summary>
public sealed class BoundedContextIsolationTests : ArchitectureTestBase
{
    private static readonly Assembly[] IdentityContext = [IdentityDomainAssembly, IdentityApplicationAssembly, IdentityInfrastructureAssembly];
    private static readonly Assembly[] CloudContext = [CloudDomainAssembly, CloudApplicationAssembly, CloudInfrastructureAssembly];
    private static readonly Assembly[] CoreContext = [DomainAssembly, ApplicationAssembly, InfrastructureAssembly];

    [Fact]
    public void Identity_ShouldNot_DependOn_OtherContexts()
    {
        AssertNoDependency(IdentityContext, [.. CloudContext, .. CoreContext], "Identity", "Cloud or Core");
    }

    [Fact]
    public void Cloud_ShouldNot_DependOn_OtherContexts()
    {
        AssertNoDependency(CloudContext, [.. IdentityContext, .. CoreContext], "Cloud", "Identity or Core");
    }

    [Fact]
    public void CoreEngine_ShouldNot_DependOn_CloudOrIdentity()
    {
        AssertNoDependency(CoreContext, [.. IdentityContext, .. CloudContext], "Core", "Identity or Cloud");
    }

    private static void AssertNoDependency(Assembly[] context, Assembly[] forbidden, string contextName, string forbiddenName)
    {
        string[] forbiddenNames = [.. forbidden.Select(a => a.GetName().Name!)];

        var result = Types.InAssemblies(context)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenNames)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"The {contextName} context must not depend on {forbiddenName}:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }
}
