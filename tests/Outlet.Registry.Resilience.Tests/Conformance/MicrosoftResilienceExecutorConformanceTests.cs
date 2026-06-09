using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Resilience;

namespace Outlet.Registry.Resilience.Tests.Conformance;

public sealed class MicrosoftResilienceExecutorConformanceTests : ResilienceExecutorConformanceTests
{
    protected override IResilienceExecutor CreateExecutor(Action<ResilienceOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddMicrosoftResilience(configure);
        return services.BuildServiceProvider().GetRequiredService<IResilienceExecutor>();
    }
}
