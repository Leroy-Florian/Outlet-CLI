using Outlet.Core.Domain.RegistryItems;
using Outlet.Core.Infrastructure.Rewriting;

namespace Outlet.Core.Infrastructure.UnitTests.Rewriting;

public sealed class RoslynNamespaceRewriterTests
{
    private readonly RoslynNamespaceRewriter _rewriter = new();

    private string Rewrite(string source, string from = "Outlet.Registry.Email", string to = "Acme.App.Infrastructure.Email")
        => _rewriter.Rewrite(source, TargetNamespace.From(from), TargetNamespace.From(to));

    [Fact]
    public void Should_RewriteFileScopedNamespaceDeclaration()
    {
        var result = Rewrite("namespace Outlet.Registry.Email;\n\npublic interface IEmailSender;\n");

        result.Should().Contain("namespace Acme.App.Infrastructure.Email;");
        result.Should().NotContain("Outlet.Registry.Email");
    }

    [Fact]
    public void Should_RewriteBlockNamespaceDeclaration()
    {
        var result = Rewrite("namespace Outlet.Registry.Email\n{\n    public class X { }\n}\n");

        result.Should().Contain("namespace Acme.App.Infrastructure.Email");
    }

    [Fact]
    public void Should_RewriteUsingDirective_When_ItMatchesTheRegistryRoot()
    {
        var source = "using Outlet.Registry.Email;\n\nnamespace Acme.Consumer;\n";

        var result = Rewrite(source);

        result.Should().Contain("using Acme.App.Infrastructure.Email;");
    }

    [Fact]
    public void Should_RewriteSubNamespacePrefix_When_UsingIsUnderTheRoot()
    {
        var source = "using Outlet.Registry.Email.Internal;\n\nnamespace Acme.Consumer;\n";

        var result = Rewrite(source);

        result.Should().Contain("using Acme.App.Infrastructure.Email.Internal;");
    }

    [Fact]
    public void Should_LeaveUnrelatedNamespacesAndUsingsUntouched()
    {
        var source = """
            using System;
            using MailKit.Net.Smtp;

            namespace Outlet.Registry.Email;

            public sealed class SmtpEmailSender;
            """;

        var result = Rewrite(source);

        result.Should().Contain("using System;");
        result.Should().Contain("using MailKit.Net.Smtp;");
        result.Should().Contain("namespace Acme.App.Infrastructure.Email;");
    }

    [Fact]
    public void Should_NotTouchStringsOrComments_ThatMentionTheNamespace()
    {
        var source = """
            namespace Outlet.Registry.Email;

            public sealed class X
            {
                // Outlet.Registry.Email stays here as documentation
                public const string Marker = "Outlet.Registry.Email";
            }
            """;

        var result = Rewrite(source);

        result.Should().Contain("// Outlet.Registry.Email stays here as documentation");
        result.Should().Contain("\"Outlet.Registry.Email\"");
        result.Should().Contain("namespace Acme.App.Infrastructure.Email;");
    }

    [Fact]
    public void Should_RoundTripToValidParseableCode()
    {
        var source = """
            using Outlet.Registry.Email;

            namespace Outlet.Registry.Email;

            public sealed class SmtpEmailSender : IEmailSender;
            """;

        var result = Rewrite(source);

        Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(result)
            .GetDiagnostics().Should().BeEmpty();
    }
}
