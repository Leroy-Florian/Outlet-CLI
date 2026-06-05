using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.UnitTests.Fakes;

/// <summary>
/// Hand-written <see cref="INamespaceRewriter"/> — a plain prefix replace, enough to
/// assert the orchestration rewrites content. The real Roslyn rewriter is tested on its own.
/// </summary>
public sealed class FakeNamespaceRewriter : INamespaceRewriter
{
    public string Rewrite(string sourceCode, TargetNamespace sourceRoot, TargetNamespace target)
        => sourceCode.Replace(sourceRoot.Value, target.Value, StringComparison.Ordinal);
}
