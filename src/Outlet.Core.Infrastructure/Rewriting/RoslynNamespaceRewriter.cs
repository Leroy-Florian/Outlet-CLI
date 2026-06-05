using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Outlet.Core.Application.Ports;
using Outlet.Core.Domain.RegistryItems;

namespace Outlet.Core.Infrastructure.Rewriting;

/// <summary>
/// SECONDARY ADAPTER — rewrites namespace declarations with Roslyn
/// (CSharpSyntaxRewriter), never textual find/replace, so strings,
/// comments and unrelated identifiers are untouched.
/// </summary>
public sealed class RoslynNamespaceRewriter : INamespaceRewriter
{
    public string Rewrite(string sourceCode, TargetNamespace target)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();

        var rewritten = new NamespaceNameRewriter(target.Value).Visit(root);

        return rewritten.ToFullString();
    }

    private sealed class NamespaceNameRewriter(string targetNamespace) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            var visited = (FileScopedNamespaceDeclarationSyntax)base.VisitFileScopedNamespaceDeclaration(node)!;
            return visited.WithName(BuildName(visited.Name));
        }

        public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var visited = (NamespaceDeclarationSyntax)base.VisitNamespaceDeclaration(node)!;
            return visited.WithName(BuildName(visited.Name));
        }

        private NameSyntax BuildName(NameSyntax original)
        {
            return SyntaxFactory.ParseName(targetNamespace)
                .WithLeadingTrivia(original.GetLeadingTrivia())
                .WithTrailingTrivia(original.GetTrailingTrivia());
        }
    }
}
