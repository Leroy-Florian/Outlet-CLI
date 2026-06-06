using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Infrastructure.Rewriting;

/// <summary>
/// SECONDARY ADAPTER — rewrites namespace declarations and <c>using</c> directives
/// with Roslyn (CSharpSyntaxRewriter), never textual find/replace. It remaps the
/// registry root namespace (and any sub-namespace) to the target; strings,
/// comments and unrelated identifiers are untouched.
/// </summary>
public sealed class RoslynNamespaceRewriter : INamespaceRewriter
{
    public string Rewrite(string sourceCode, TargetNamespace sourceRoot, TargetNamespace target)
    {
        var root = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();
        var rewritten = new PrefixRewriter(sourceRoot.Value, target.Value).Visit(root);
        return rewritten.ToFullString();
    }

    private sealed class PrefixRewriter(string sourceRoot, string target) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            var visited = (FileScopedNamespaceDeclarationSyntax)base.VisitFileScopedNamespaceDeclaration(node)!;
            return visited.WithName(Remap(visited.Name));
        }

        public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var visited = (NamespaceDeclarationSyntax)base.VisitNamespaceDeclaration(node)!;
            return visited.WithName(Remap(visited.Name));
        }

        public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
        {
            var visited = (UsingDirectiveSyntax)base.VisitUsingDirective(node)!;
            return visited.Name is null ? visited : visited.WithName(Remap(visited.Name));
        }

        private NameSyntax Remap(NameSyntax original)
        {
            var name = original.ToString();

            var remapped =
                name == sourceRoot ? target
                : name.StartsWith(sourceRoot + ".", StringComparison.Ordinal) ? target + name[sourceRoot.Length..]
                : null;

            if (remapped is null)
                return original;

            return SyntaxFactory.ParseName(remapped)
                .WithLeadingTrivia(original.GetLeadingTrivia())
                .WithTrailingTrivia(original.GetTrailingTrivia());
        }
    }
}
