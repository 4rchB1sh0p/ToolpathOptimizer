namespace ToolpathOptimizer.Cli.IntegrationTests.Support;

/// <summary>
/// Runs <c>analyze</c> on the plate sample once and shares the result, so several focused tests can assert on
/// one child process instead of starting one each.
/// </summary>
public sealed class AnalyzeSampleFixture : IAsyncLifetime, IDisposable
{
    private readonly OutputDirectory _output = new();

    /// <summary>Gets the report path passed to <c>--output</c>.</summary>
    public string ReportPath => _output.PathOf("report.json");

    /// <summary>Gets the files in the output directory after the run.</summary>
    public string[] OutputFiles => _output.Files;

    /// <summary>Gets the captured run.</summary>
    internal CliRun Run { get; private set; } = new(-1, string.Empty, string.Empty);

    /// <inheritdoc />
    public async Task InitializeAsync() =>
        Run = await CliProcess.RunAsync("analyze", "--points", CliProcess.SamplePath("plate.csv"), "--job", CliProcess.SamplePath("plate-job.json"), "--output", ReportPath);

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public void Dispose() => _output.Dispose();
}
