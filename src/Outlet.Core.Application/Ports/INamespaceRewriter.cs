using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Application.Ports;

/// <summary>
/// SECONDARY PORT — rewrites the registry root namespace of a copied C# source
/// file to the user's target namespace. Implementations MUST use Roslyn
/// (CSharpSyntaxRewriter), never textual find/replace, so strings, comments and
/// unrelated identifiers are left untouched.
/// </summary>
public interface INamespaceRewriter
{
    /// <summary>
    /// Returns <paramref name="sourceCode"/> with every namespace declaration and
    /// <c>using</c> directive under <paramref name="sourceRoot"/> remapped to
    /// <paramref name="target"/> (the root itself and any sub-namespace prefix).
    /// Unrelated namespaces (System, provider libs, …) are preserved.
    /// </summary>
    string Rewrite(string sourceCode, TargetNamespace sourceRoot, TargetNamespace target);
}
