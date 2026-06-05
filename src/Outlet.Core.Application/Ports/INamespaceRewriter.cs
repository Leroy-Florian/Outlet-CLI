using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — rewrites the namespace of a copied C# source file to the
/// user's target namespace. Implementations MUST use Roslyn
/// (CSharpSyntaxRewriter), never textual find/replace.
/// </summary>
public interface INamespaceRewriter
{
    /// <summary>Returns the source with its namespace declarations rewritten to <paramref name="target"/>.</summary>
    string Rewrite(string sourceCode, TargetNamespace target);
}
