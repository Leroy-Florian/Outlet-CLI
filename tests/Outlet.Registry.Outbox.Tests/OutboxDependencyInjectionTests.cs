using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Outbox;
using Outlet.Registry.Outbox.Tests.Support;

namespace Outlet.Registry.Outbox.Tests;

public sealed class OutboxDependencyInjectionTests
{
    [Fact]
    public void Should_ResolveEfCoreStoreBehindThePort_When_AddEfCoreOutboxIsCalled()
    {
        var services = new ServiceCollection();
        services.AddScoped<IOutboxDbContext>(_ => new TestOutboxDbContext(
            new DbContextOptionsBuilder<TestOutboxDbContext>().UseSqlite("Data Source=:memory:").Options));
        services.AddEfCoreOutbox();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IOutboxStore>().Should().BeOfType<OutboxEfCoreStore>();
    }

    [Fact]
    public void Should_ScopeTheStore_When_Registered()
    {
        var services = new ServiceCollection();
        services.AddScoped<IOutboxDbContext>(_ => new TestOutboxDbContext(
            new DbContextOptionsBuilder<TestOutboxDbContext>().UseSqlite("Data Source=:memory:").Options));
        services.AddEfCoreOutbox();

        var descriptor = services.Single(service => service.ServiceType == typeof(IOutboxStore));

        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}
