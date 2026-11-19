using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ToolpathOptimizer.Cli.IntegrationTests.Support;

namespace ToolpathOptimizer.Cli.IntegrationTests;

/// <summary>In-process tests of <see cref="CliApplication"/> for paths that are hard to trigger in a child process.</summary>
public sealed class CliApplicationTests : IDisposable
{
    private readonly OutputDirectory _output = new();

    /// <inheritdoc />
    public void Dispose() => _output.Dispose();

    private string[] CreateAnalyzeArguments() =>
        ["analyze", "--points", CliProcess.SamplePath("plate.csv"), "--job", CliProcess.SamplePath("plate-job.json"), "--output", _output.PathOf("r.json")];

    private static string ReadFirstErrorCode(StringWriter stdout) =>
        JsonDocument.Parse(stdout.ToString()).RootElement.GetProperty("errors")[0].GetProperty("code").GetString()!;

    private async Task<(int ExitCode, string Code)> RunCancelledAsync()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        using var stdout = new StringWriter();
        int exitCode = await CliApplication.RunAsync(CreateAnalyzeArguments(), stdout, cancelled.Token);
        return (exitCode, ReadFirstErrorCode(stdout));
    }

    private async Task<(int ExitCode, string Code)> RunPastDeadlineAsync()
    {
        using var stdout = new StringWriter();
        int exitCode = await CliApplication.RunAsync(
            CreateAnalyzeArguments(),
            stdout,
            services => services.AddSingleton<TimeProvider>(new ExpiredDeadlineTimeProvider()),
            CancellationToken.None);
        return (exitCode, ReadFirstErrorCode(stdout));
    }

    [Fact]
    public async Task RunAsync_Cancelled_ExitsSix()
    {
        Assert.Equal((6, "CANCELLED"), await RunCancelledAsync());
    }

    [Fact]
    public async Task RunAsync_Cancelled_LeavesNoFile()
    {
        await RunCancelledAsync();

        Assert.Empty(_output.Files);
    }

    [Fact]
    public async Task RunAsync_DeadlineReached_ExitsEight()
    {
        Assert.Equal((8, "OPT_DEADLINE_EXCEEDED"), await RunPastDeadlineAsync());
    }

    [Fact]
    public async Task RunAsync_DeadlineReached_LeavesNoFile()
    {
        await RunPastDeadlineAsync();

        Assert.Empty(_output.Files);
    }

    [Fact]
    public async Task RunAsync_Help_PrintsUsage()
    {
        using var stdout = new StringWriter();

        int exitCode = await CliApplication.RunAsync(["help"], stdout, CancellationToken.None);

        Assert.Equal((0, true), (exitCode, stdout.ToString().Contains("toolpath-optimize analyze", StringComparison.Ordinal)));
    }
}
