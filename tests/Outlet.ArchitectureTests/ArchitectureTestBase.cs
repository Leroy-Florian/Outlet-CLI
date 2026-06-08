using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Outlet.ArchitectureTests;

public abstract class ArchitectureTestBase
{
    // ==========================================================================
    // Outlet now spans more than one bounded context:
    //   - the registry/install engine (Outlet.Core.*), and
    //   - identity / access (Outlet.Identity.*) — users + personal access tokens.
    // The convention gates below run across every context's Domain assembly via
    // AllDomainAssemblies. A dedicated BoundedContextIsolationTests lands with the
    // Cloud context (organizations), when cross-context isolation first has teeth.
    // ==========================================================================

    protected static readonly Assembly KernelAssembly =
        typeof(Outlet.Kernel.Shared.AssemblyReference).Assembly;

    protected static readonly Assembly DomainAssembly =
        typeof(Outlet.Core.Domain.AssemblyReference).Assembly;

    protected static readonly Assembly ApplicationAssembly =
        typeof(Outlet.Core.Application.AssemblyReference).Assembly;

    protected static readonly Assembly InfrastructureAssembly =
        typeof(Outlet.Core.Infrastructure.AssemblyReference).Assembly;

    protected static readonly Assembly IdentityDomainAssembly =
        typeof(Outlet.Identity.Domain.AssemblyReference).Assembly;

    protected static readonly Assembly[] AllDomainAssemblies = [DomainAssembly, IdentityDomainAssembly];
    protected static readonly Assembly[] AllApplicationAssemblies = [ApplicationAssembly];
    protected static readonly Assembly[] AllInfrastructureAssemblies = [InfrastructureAssembly];

    #region Given - Assembly Selection

    protected static Types GivenTypesInDomain() => Types.InAssemblies(AllDomainAssemblies);

    protected static Types GivenTypesInApplication() => Types.InAssemblies(AllApplicationAssemblies);

    protected static Types GivenTypesInInfrastructure() => Types.InAssemblies(AllInfrastructureAssemblies);

    protected static IEnumerable<Type> GivenAllTypesInDomain() =>
        AllDomainAssemblies.SelectMany(a => a.GetTypes());

    protected static IEnumerable<Type> GivenAllTypesInApplication() =>
        AllApplicationAssemblies.SelectMany(a => a.GetTypes());

    protected static IEnumerable<Type> GivenAllTypesInInfrastructure() =>
        AllInfrastructureAssemblies.SelectMany(a => a.GetTypes());

    #endregion

    #region When - Dependency Checks

    protected static TestResult WhenCheckingNoDependencyOn(Types types, params string[] forbiddenDependencies) =>
        types.ShouldNot().HaveDependencyOnAny(forbiddenDependencies).GetResult();

    #endregion

    #region Then - Assertions

    protected static void ThenShouldHaveNoViolations(TestResult result, string layerName, string dependencyType)
    {
        result.IsSuccessful.Should().BeTrue(
            $"{layerName} should not have {dependencyType} dependencies but found violations in:{Environment.NewLine}" +
            FormatViolations(result.FailingTypeNames));
    }

    #endregion

    #region Property Analysis Helpers

    protected static bool HasPublicSetters(Type type) =>
        type.GetProperties().Any(p =>
            p.SetMethod?.IsPublic == true &&
            !IsInitOnlySetter(p));

    private static bool IsInitOnlySetter(PropertyInfo property) =>
        property.SetMethod?.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(x => x.Name == "IsExternalInit") == true;

    #endregion

    #region Formatting Helpers

    protected static string FormatViolations(IEnumerable<string>? violations) =>
        violations is null ? string.Empty : string.Join(Environment.NewLine, violations.Select(v => $"  - {v}"));

    protected static string FormatTypeList(IEnumerable<string> types) =>
        string.Join(Environment.NewLine, types.Select(t => $"  - {t}"));

    #endregion
}
