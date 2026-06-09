using Outlet.Core.Application.Ports;

namespace Outlet.Core.UnitTests.Fakes;

/// <summary>Hand-written in-memory fake of <see cref="ICliUpdateStateStore"/>.</summary>
public sealed class FakeCliUpdateStateStore : ICliUpdateStateStore
{
    public DateTime? LastCheckUtc { get; private set; }

    public void Seed(DateTime lastCheckUtc) => LastCheckUtc = lastCheckUtc;

    public DateTime? ReadLastCheckUtc() => LastCheckUtc;

    public void SaveLastCheckUtc(DateTime timestampUtc) => LastCheckUtc = timestampUtc;
}
