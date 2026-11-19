using System.Text.RegularExpressions;
using ToolpathOptimizer.Cli.IntegrationTests.Support;

namespace ToolpathOptimizer.Cli.IntegrationTests;

/// <summary>
/// Golden-fixture tests: reports must match the committed bytes except <c>generated_at_utc</c> and
/// <c>tool_version</c>. Each golden runs twice to catch hidden state (testing-strategy.md), which is why this
/// suite starts two processes per fixture.
/// </summary>
[Trait("Category", "Process")]
public sealed partial class GoldenReportProcessTests : IDisposable
{
    private readonly OutputDirectory _output = new();

    /// <inheritdoc />
    public void Dispose() => _output.Dispose();

    [Theory]
    [InlineData("holes.csv", "job.json", "holes.report.json")]
    [InlineData("plate.csv", "plate-job.json", "plate.report.json")]
    public async Task Analyze_GoldenSampleRunTwice_MatchesGoldenReportEachTime(string points, string job, string golden)
    {
        string expected = Normalise(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Golden", golden)));
        string first = await AnalyzeToTextAsync(points, job, "first-" + golden);
        string second = await AnalyzeToTextAsync(points, job, "second-" + golden);

        Assert.Equal((expected, expected), (Normalise(first), Normalise(second)));
    }

    private async Task<string> AnalyzeToTextAsync(string points, string job, string outputName)
    {
        string output = _output.PathOf(outputName);
        CliRun run = await CliProcess.RunAsync("analyze", "--points", CliProcess.SamplePath(points), "--job", CliProcess.SamplePath(job), "--output", output);
        Assert.True(run.ExitCode == 0, $"analyze failed with exit code {run.ExitCode}: {run.Stdout}");
        return await File.ReadAllTextAsync(output);
    }

    /// <summary>Replaces the fields excluded from determinism comparisons.</summary>
    private static string Normalise(string report) =>
        VolatileFields().Replace(report.ReplaceLineEndings("\n"), "\"$1\": \"<excluded>\"");

    [GeneratedRegex("\"(generated_at_utc|tool_version)\": \"[^\"]*\"")]
    private static partial Regex VolatileFields();
}
