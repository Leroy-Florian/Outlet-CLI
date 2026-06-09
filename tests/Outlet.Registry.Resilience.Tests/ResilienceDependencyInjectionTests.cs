using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Resilience;

namespace Outlet.Registry.Resilience.Tests;

public sealed class ResilienceDependencyInjectionTests
{
    [Fact]
    public void Should_ResolvePollyExecutor_When_AddPollyResilienceIsCalled()
    {
        var services = new ServiceCollection();
        services.AddPollyResilience(o => o.MaxRetries = 1);

        using var provider = services.BuildServiceProvider();
        var executor = provider.GetService<IResilienceExecutor>();

        executor.Should().BeOfType<PollyResilienceExecutor>();
    }

    [Fact]
    public void Should_ForwardGenericAndSpecificToSameInstance_When_AddPollyResilienceIsCalled()
    {
        var services = new ServiceCollection();
        services.AddPollyResilience(o => o.MaxRetries = 1);

        using var provider = services.BuildServiceProvider();
        var generic = provider.GetRequiredService<IResilienceExecutor>();
        var specific = provider.GetRequiredService<IPollyResilienceExecutor>();

        generic.Should().BeOfType<PollyResilienceExecutor>();
        specific.Should().BeSameAs(generic);
    }

    [Fact]
    public void Should_ResolveBuiltInExecutor_When_AddBuiltInResilienceIsCalled()
    {
        var services = new ServiceCollection();
        services.AddBuiltInResilience(o => o.MaxRetries = 1);

        using var provider = services.BuildServiceProvider();
        var executor = provider.GetService<IResilienceExecutor>();

        executor.Should().BeOfType<BuiltInResilienceExecutor>();
    }

    [Fact]
    public void Should_ResolveMicrosoftExecutor_When_AddMicrosoftResilienceIsCalled()
    {
        var services = new ServiceCollection();
        services.AddMicrosoftResilience(o => o.MaxRetries = 1);

        using var provider = services.BuildServiceProvider();
        var executor = provider.GetService<IResilienceExecutor>();

        executor.Should().BeOfType<MicrosoftResilienceExecutor>();
    }

    [Fact]
    public void Should_LetAdaptersSwapBehindOnePort_When_OnlyTheDiCallChanges()
    {
        IResilienceExecutor Resolve(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            register(services);
            return services.BuildServiceProvider().GetRequiredService<IResilienceExecutor>();
        }

        var polly = Resolve(s => s.AddPollyResilience(o => o.MaxRetries = 1));
        var microsoft = Resolve(s => s.AddMicrosoftResilience(o => o.MaxRetries = 1));
        var builtIn = Resolve(s => s.AddBuiltInResilience(o => o.MaxRetries = 1));

        polly.Should().BeOfType<PollyResilienceExecutor>();
        microsoft.Should().BeOfType<MicrosoftResilienceExecutor>();
        builtIn.Should().BeOfType<BuiltInResilienceExecutor>();
    }
}
