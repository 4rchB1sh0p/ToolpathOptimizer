using System.Text.Json;
using ToolpathOptimizer.Cli.IntegrationTests.Support;

namespace ToolpathOptimizer.Cli.IntegrationTests;

/// <summary>Child-process tests of a successful <c>analyze</c> run, sharing one process via <see cref="AnalyzeSampleFixture"/>.</summary>
[Trait("Category", "Process")]
public sealed class AnalyzeCommandProcessTests : IClassFixture<AnalyzeSampleFixture>
{
    private readonly AnalyzeSampleFixture _fixture;

    /// <summary>Creates the tests with the shared run.</summary>
    /// <param name="fixture">Shared analyze run.</param>
    public AnalyzeCommandProcessTests(AnalyzeSampleFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Analyze_PlateSample_ExitsZero()
    {
        Assert.Equal(0, _fixture.Run.ExitCode);
    }

    [Fact]
    public void Analyze_PlateSample_PrintsOkEnvelope()
    {
        Assert.Equal("ok", _fixture.Run.ReadEnvelope().GetProperty("status").GetString());
    }

    [Fact]
    public void Analyze_PlateSample_EnvelopeNamesReportPath()
    {
        Assert.Equal(_fixture.ReportPath, _fixture.Run.ReadEnvelope().GetProperty("report_path").GetString());
    }

    [Fact]
    public void Analyze_PlateSample_KeepsEnvelopeOffStderr()
    {
        Assert.DoesNotContain("schema_version", _fixture.Run.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyze_PlateSample_WritesReportSelectingCandidate()
    {
        using JsonDocument report = JsonDocument.Parse(File.ReadAllBytes(_fixture.ReportPath));

        Assert.Equal("candidate", report.RootElement.GetProperty("selected_route").GetString());
    }

    [Fact]
    public void Analyze_PlateSample_LeavesOnlyTheReport()
    {
        Assert.Equal([_fixture.ReportPath], _fixture.OutputFiles);
    }
}
