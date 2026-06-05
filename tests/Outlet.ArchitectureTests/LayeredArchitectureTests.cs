using NetArchTest.Rules;

namespace Outlet.ArchitectureTests;

public sealed class LayeredArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        var result = Types.InAssemblies(AllDomainAssemblies)
            .ShouldNot()
            .HaveDependencyOnAny([.. AllApplicationAssemblies.Select(a => a.GetName().Name!)])
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain layer depends on Application layer:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssemblies(AllDomainAssemblies)
            .ShouldNot()
            .HaveDependencyOnAny([.. AllInfrastructureAssemblies.Select(a => a.GetName().Name!)])
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain layer depends on Infrastructure layer:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssemblies(AllApplicationAssemblies)
            .ShouldNot()
            .HaveDependencyOnAny([.. AllInfrastructureAssemblies.Select(a => a.GetName().Name!)])
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application layer depends on Infrastructure layer:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Kernel_ShouldNot_DependOn_AnyCoreLayer()
    {
        var result = Types.InAssembly(KernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                DomainAssembly.GetName().Name!,
                ApplicationAssembly.GetName().Name!,
                InfrastructureAssembly.GetName().Name!)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"SharedKernel must stay independent of the Core engine:{Environment.NewLine}" +
            string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Infrastructure_Should_DependOn_ApplicationOrDomain()
    {
        var hasRealTypes = Types.InAssembly(InfrastructureAssembly)
            .That()
            .DoNotHaveName("AssemblyReference")
            .GetTypes()
            .Any();

        if (!hasRealTypes)
            return;

        var hasApplicationDependency = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveDependencyOn(ApplicationAssembly.GetName().Name!)
            .GetTypes()
            .Any();

        var hasDomainDependency = Types.InAssembly(InfrastructureAssembly)
            .That()
            .HaveDependencyOn(DomainAssembly.GetName().Name!)
            .GetTypes()
            .Any();

        Assert.True(hasApplicationDependency || hasDomainDependency,
            "Infrastructure should implement Application ports / use Domain types — " +
            "an Infrastructure layer with no inward dependency is dead weight.");
    }
}
