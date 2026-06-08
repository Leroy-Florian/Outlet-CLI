using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Registry.Outbox;
using Outlet.Registry.Outbox.Tests.Support;

namespace Outlet.Registry.Outbox.Tests;

public sealed class OutboxDependencyInjectionTests
{
    [Fact]
    public void Should_ResolveEfCoreStore_When_AddEfCoreOutboxIsCalled()
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
    public void Should_ResolveDapperStore_When_AddDapperOutboxIsCalled()
    {
        var services = new ServiceCollection();
        services.AddScoped<IOutboxDbSession>(_ => new AmbientOutboxDbSession(new SqliteConnection("Data Source=:memory:"), null));
        services.AddDapperOutbox(options => options.TableName = "Outbox");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IOutboxStore>().Should().BeOfType<OutboxDapperStore>();
    }

    [Fact]
    public void Should_LetAdaptersSwapBehindOnePort_When_OnlyTheDiCallChanges()
    {
        IOutboxStore Resolve(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            register(services);
            var provider = services.BuildServiceProvider();
            return provider.CreateScope().ServiceProvider.GetRequiredService<IOutboxStore>();
        }

        var efCore = Resolve(services =>
        {
            services.AddScoped<IOutboxDbContext>(_ => new TestOutboxDbContext(
                new DbContextOptionsBuilder<TestOutboxDbContext>().UseSqlite("Data Source=:memory:").Options));
            services.AddEfCoreOutbox();
        });

        var dapper = Resolve(services =>
        {
            services.AddScoped<IOutboxDbSession>(_ => new AmbientOutboxDbSession(new SqliteConnection("Data Source=:memory:"), null));
            services.AddDapperOutbox();
        });

        efCore.Should().BeOfType<OutboxEfCoreStore>();
        dapper.Should().BeOfType<OutboxDapperStore>();
    }
}
