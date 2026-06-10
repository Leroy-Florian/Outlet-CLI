using Outlet.Core.Application.Ports;
using Outlet.Core.Application.RegistryItems;
using Outlet.Kernel.Shared;

namespace Outlet.Core.UnitTests.Fakes;

/// <summary>Hand-written in-memory <see cref="IRegistryPublisher"/> recording the last call.</summary>
public sealed class FakeRegistryPublisher : IRegistryPublisher
{
    private string? _failure;

    public string? RegistryRoot { get; private set; }

    public string? OutputDirectory { get; private set; }

    public bool DryRun { get; private set; }

    public void Failing(string error) => _failure = error;

    public Task<Result<RegistryPublicationReport>> PublishAsync(
        string registryRoot,
        string outputDirectory,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        RegistryRoot = registryRoot;
        OutputDirectory = outputDirectory;
        DryRun = dryRun;

        if (_failure is not null)
            return Task.FromResult(Result<RegistryPublicationReport>.Failure(_failure));

        return Task.FromResult(Result<RegistryPublicationReport>.Success(
            new RegistryPublicationReport(outputDirectory, dryRun, [new PublishedRegistryItem("email-smtp", 2)])));
    }
}
