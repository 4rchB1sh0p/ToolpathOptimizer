using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace ToolpathOptimizer.Cli.IntegrationTests;

/// <summary>In-process tests for paths that are hard to trigger in a child process (cancellation, deadline).</summary>
public sealed class CliApplicationTests : IDisposable
{
    private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), "tpo-cli-app-tests", Guid.NewGuid().ToString("N"));

    public CliApplicationTests()
    {
        Directory.CreateDirectory(_outputDirectory);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_outputDirectory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
    }

    private string[] AnalyzeArgs() =>
        ["analyze", "--points", CliProcess.Samples("plate.csv"), "--job", CliProcess.Samples("plate-job.json"), "--output", Path.Combine(_outputDirectory, "r.json")];

    private static string FirstCode(StringWriter stdout) =>
        JsonDocument.Parse(stdout.ToString()).RootElement.GetProperty("errors")[0].GetProperty("code").GetString()!;

    [Fact]
    public async Task Cancellation_ExitsSixAndLeavesNoFile()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        using var stdout = new StringWriter();

        int exitCode = await CliApplication.RunAsync(AnalyzeArgs(), stdout, configureServices: null, cancelled.Token);

        Assert.Equal(6, exitCode);
        Assert.Equal("CANCELLED", FirstCode(stdout));
        Assert.Empty(Directory.GetFiles(_outputDirectory));
    }

    [Fact]
    public async Task DeadlineReached_ExitsEightAndLeavesNoFile()
    {
        using var stdout = new StringWriter();

        int exitCode = await CliApplication.RunAsync(AnalyzeArgs(), stdout, services => services.AddSingleton<TimeProvider>(new ExpiredDeadlineTimeProvider()), CancellationToken.None);

        Assert.Equal(8, exitCode);
        Assert.Equal("OPT_DEADLINE_EXCEEDED", FirstCode(stdout));
        Assert.Empty(Directory.GetFiles(_outputDirectory));
    }

    [Fact]
    public async Task Help_PrintsUsageAndExitsZero()
    {
        using var stdout = new StringWriter();

        int exitCode = await CliApplication.RunAsync(["help"], stdout, null, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Contains("toolpath-optimize analyze", stdout.ToString(), StringComparison.Ordinal);
    }

    /// <summary>A clock whose timers fire immediately, simulating a reached wall-clock deadline.</summary>
    private sealed class ExpiredDeadlineTimeProvider : TimeProvider
    {
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            callback(state);
            return System.CreateTimer(_ => { }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
    }
}
