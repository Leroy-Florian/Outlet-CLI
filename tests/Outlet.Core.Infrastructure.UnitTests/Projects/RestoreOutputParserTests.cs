using Outlet.Core.Infrastructure.Projects;

namespace Outlet.Core.Infrastructure.UnitTests.Projects;

public sealed class RestoreOutputParserTests
{
    [Fact]
    public void Should_ReportSuccess_When_ExitCodeIsZero()
    {
        var result = RestoreOutputParser.Parse(0, "Restored /repo/App.csproj (in 1.2 sec).", string.Empty);

        result.Succeeded.Should().BeTrue();
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Should_ReportFailure_AndExtractError_When_VersionConflict()
    {
        const string output = """
            Determining projects to restore...
            /repo/App.csproj : error NU1107: Version conflict detected for Newtonsoft.Json. Install/reference Newtonsoft.Json 13.0.3 directly.
            Build FAILED.
            """;

        var result = RestoreOutputParser.Parse(1, output, string.Empty);

        result.Succeeded.Should().BeFalse();
        result.Diagnostics.Should().ContainSingle()
            .Which.Should().StartWith("NU1107:").And.Contain("Version conflict detected for Newtonsoft.Json");
    }

    [Fact]
    public void Should_SurfaceWarnings_When_RestoreSucceedsWithDowngrade()
    {
        const string output =
            "/repo/App.csproj : warning NU1605: Detected package downgrade: System.Text.Json from 9.0.0 to 8.0.0.";

        var result = RestoreOutputParser.Parse(0, output, string.Empty);

        result.Succeeded.Should().BeTrue();
        result.Diagnostics.Should().ContainSingle().Which.Should().StartWith("NU1605:");
    }

    [Fact]
    public void Should_DeduplicateRepeatedDiagnostics_AcrossFrameworks()
    {
        const string output = """
            /repo/App.csproj : error NU1107: Version conflict detected for SendGrid.
            /repo/App.csproj : error NU1107: Version conflict detected for SendGrid.
            """;

        var result = RestoreOutputParser.Parse(1, output, string.Empty);

        result.Diagnostics.Should().ContainSingle();
    }

    [Fact]
    public void Should_ScanStandardError_AsWellAsStandardOutput()
    {
        var result = RestoreOutputParser.Parse(1, string.Empty, "error NU1101: Unable to find package Ghost.Package.");

        result.Diagnostics.Should().ContainSingle().Which.Should().StartWith("NU1101:");
    }
}
