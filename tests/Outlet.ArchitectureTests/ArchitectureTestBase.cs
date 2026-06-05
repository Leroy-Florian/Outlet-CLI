using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Outlet.ArchitectureTests;

public abstract class ArchitectureTestBase
{
    // ==========================================================================
    // Outlet is (for now) a SINGLE bounded context: the registry/install engine.
    // If a second context ever appears, switch to per-context assembly arrays
    // like WOW's ArchitectureTestBase and add BoundedContextIsolationTests back.
    // ==========================================================================

    protected static readonly Assembly KernelAssembly =
        typeof(Outlet.Kernel.Shared.AssemblyReference).Assembly;

    protected static readonly Assembly DomainAssembly =
        typeof(Outlet.Core.Domain.AssemblyReference).Assembly;

    protected static readonly Assembly ApplicationAssembly =
        typeof(Outlet.Core.Application.AssemblyReference).Assembly;

    protected static readonly Assembly InfrastructureAssembly =
        typeof(Outlet.Core.Infrastructure.AssemblyReference).Assembly;

    protected static readonly Assembly[] AllDomainAssemblies = [DomainAssembly];
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
