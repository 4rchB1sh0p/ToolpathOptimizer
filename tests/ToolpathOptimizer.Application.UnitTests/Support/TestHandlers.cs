using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Provenance;

namespace ToolpathOptimizer.Application.UnitTests.Support;

/// <summary>Analysis handlers wired with default limits, null loggers and a fixed clock.</summary>
internal sealed class TestHandlers
{
    /// <summary>Gets the fixed time used by the default clock.</summary>
    public static DateTimeOffset FixedNow { get; } = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Creates handlers with default limits and the fixed clock.</summary>
    public TestHandlers()
        : this(new AnalysisLimitsOptions(), new FakeTimeProvider(FixedNow))
    {
    }

    /// <summary>Creates handlers with a specific clock.</summary>
    /// <param name="clock">Clock for deadlines and timestamps.</param>
    public TestHandlers(TimeProvider clock)
        : this(new AnalysisLimitsOptions(), clock)
    {
    }

    /// <summary>Creates handlers with specific limits and clock.</summary>
    /// <param name="limits">Analysis limits.</param>
    /// <param name="clock">Clock for deadlines and timestamps.</param>
    public TestHandlers(AnalysisLimitsOptions limits, TimeProvider clock)
    {
        Clock = clock;
        IOptions<AnalysisLimitsOptions> options = Options.Create(limits);
        var preparer = new JobPreparer(options);
        Validate = new ValidateJobHandler(preparer, NullLogger<ValidateJobHandler>.Instance);
        Analyze = new AnalyzeJobHandler(preparer, new ProvenanceFactory(clock, new ToolInfo("toolpath-optimize", "test")), options, clock, NullLogger<AnalyzeJobHandler>.Instance);
    }

    /// <summary>Gets the clock.</summary>
    public TimeProvider Clock { get; }

    /// <summary>Gets the validation handler.</summary>
    public ValidateJobHandler Validate { get; }

    /// <summary>Gets the analysis handler.</summary>
    public AnalyzeJobHandler Analyze { get; }
}
