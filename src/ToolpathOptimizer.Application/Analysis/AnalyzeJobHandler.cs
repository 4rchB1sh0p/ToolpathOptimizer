using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Provenance;
using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Units;

namespace ToolpathOptimizer.Application.Analysis;

/// <summary>
/// Key figures of a successful analysis, used by hosts without re-parsing the report.
/// </summary>
public sealed class AnalysisSummary
{
    /// <summary>Creates a summary.</summary>
    /// <param name="inputSha256">Canonical input hash.</param>
    /// <param name="pointCount">Number of points.</param>
    /// <param name="selectedRoute"><c>candidate</c> or <c>baseline</c>.</param>
    /// <param name="baselineDistanceMm">Baseline distance in millimetres.</param>
    /// <param name="candidateDistanceMm">Candidate distance in millimetres.</param>
    /// <param name="deltaPercent">Delta percentage, or <c>null</c>.</param>
    /// <param name="warningCodes">Warning codes.</param>
    public AnalysisSummary(string inputSha256, int pointCount, string selectedRoute, double baselineDistanceMm, double candidateDistanceMm, double? deltaPercent, IReadOnlyList<string> warningCodes)
    {
        InputSha256 = inputSha256;
        PointCount = pointCount;
        SelectedRoute = selectedRoute;
        BaselineDistanceMm = baselineDistanceMm;
        CandidateDistanceMm = candidateDistanceMm;
        DeltaPercent = deltaPercent;
        WarningCodes = warningCodes.ToArray();
    }

    /// <summary>Gets the canonical input hash.</summary>
    public string InputSha256 { get; }

    /// <summary>Gets the number of points.</summary>
    public int PointCount { get; }

    /// <summary>Gets the selected route kind.</summary>
    public string SelectedRoute { get; }

    /// <summary>Gets the baseline distance in millimetres.</summary>
    public double BaselineDistanceMm { get; }

    /// <summary>Gets the candidate distance in millimetres.</summary>
    public double CandidateDistanceMm { get; }

    /// <summary>Gets candidate minus baseline in millimetres.</summary>
    public double DeltaDistanceMm => CandidateDistanceMm - BaselineDistanceMm;

    /// <summary>Gets the delta percentage, or <c>null</c> when the baseline distance is zero.</summary>
    public double? DeltaPercent { get; }

    /// <summary>Gets the warning codes.</summary>
    public IReadOnlyList<string> WarningCodes { get; }
}

/// <summary>
/// A serialised report with its summary.
/// </summary>
public sealed class AnalysisReport
{
    /// <summary>Creates a report.</summary>
    /// <param name="content">Report v1 bytes.</param>
    /// <param name="summary">Key figures.</param>
    public AnalysisReport(byte[] content, AnalysisSummary summary)
    {
        Content = content;
        Summary = summary;
    }

    /// <summary>Gets the report bytes.</summary>
    public byte[] Content { get; }

    /// <summary>Gets the key figures.</summary>
    public AnalysisSummary Summary { get; }
}

/// <summary>
/// Command: parses, validates, optimises, verifies and serialises an advisory report.
/// Shared by CLI and Web so both produce identical bytes for the same input and clock.
/// </summary>
public sealed class AnalyzeJobHandler
{
    private readonly JobPreparer _preparer;
    private readonly ProvenanceFactory _provenance;
    private readonly IOptions<AnalysisLimitsOptions> _limits;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AnalyzeJobHandler> _logger;

    /// <summary>Creates the handler.</summary>
    /// <param name="preparer">Input preparer.</param>
    /// <param name="provenance">Provenance factory.</param>
    /// <param name="limits">Configured limits (deadline).</param>
    /// <param name="timeProvider">Clock for the deadline and durations.</param>
    /// <param name="logger">Logger.</param>
    public AnalyzeJobHandler(JobPreparer preparer, ProvenanceFactory provenance, IOptions<AnalysisLimitsOptions> limits, TimeProvider timeProvider, ILogger<AnalyzeJobHandler> logger)
    {
        _preparer = preparer;
        _provenance = provenance;
        _limits = limits;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Analyses a job.</summary>
    /// <param name="input">Raw inputs.</param>
    /// <param name="cancellationToken">Caller cancellation (CANCELLED); the configured deadline yields OPT_DEADLINE_EXCEEDED.</param>
    /// <returns>The report, or the failure outcome.</returns>
    public Result<AnalysisReport> Handle(JobInput input, CancellationToken cancellationToken)
    {
        long started = _timeProvider.GetTimestamp();
        Result<AnalysisReport> result = Run(input, cancellationToken);
        long durationMs = (long)_timeProvider.GetElapsedTime(started).TotalMilliseconds;
        if (result.IsSuccess)
        {
            AnalysisSummary summary = result.Value.Summary;
            ApplicationLog.AnalysisSucceeded(_logger, summary.PointCount, summary.InputSha256, summary.SelectedRoute, durationMs);
        }
        else
        {
            ApplicationLog.AnalysisFailed(_logger, result.Outcome.Errors[0].Code, durationMs);
        }

        return result;
    }

    private Result<AnalysisReport> Run(JobInput input, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Result<AnalysisReport>.Failure(ErrorCodes.Cancelled, "The analysis was cancelled.");
        }

        Result<PreparedJob> prepared = _preparer.Prepare(input);
        if (!prepared.IsSuccess)
        {
            return Result<AnalysisReport>.Failure(prepared.Outcome);
        }

        PreparedJob job = prepared.Value;
        AlgorithmParameters parameters = job.CanonicalJob.Manifest.Parameters;
        var options = new AnalysisOptions(
            new SearchBudget(parameters.MaxPasses, parameters.MaxMoveEvaluations),
            Length.FromMillimetres(parameters.MinImprovementMm));

        using var deadline = new CancellationTokenSource(_limits.Value.Deadline, _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        AnalysisOutcome outcome;
        try
        {
            outcome = AnalysisEngine.Analyse(job.DrillJob, options, linked.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<AnalysisReport>.Failure(ErrorCodes.Cancelled, "The analysis was cancelled.");
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return Result<AnalysisReport>.Failure(ErrorCodes.OptDeadlineExceeded, $"The safety deadline of {_limits.Value.Deadline.TotalSeconds} s was reached; lower the search budgets and rerun.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ApplicationLog.AnalysisDefect(_logger, ErrorCodes.InternalError, ex);
            return Result<AnalysisReport>.Failure(ErrorCodes.InternalError, "An internal error occurred during analysis.");
        }

        if (!outcome.IsSuccess)
        {
            return Result<AnalysisReport>.Failure(OperationOutcome.Failure(outcome.Issues.Select(i => new OutcomeError(i.Code, i.Message)).ToArray()));
        }

        AnalysisResult analysis = outcome.Result;
        ReportProvenance provenance = _provenance.Create(job.InputSha256, job.SourceFiles);
        byte[] content = AnalysisReportWriter.Serialize(job.CanonicalJob, provenance, analysis);
        return Result<AnalysisReport>.Success(new AnalysisReport(content, Summarise(job, analysis)));
    }

    private static AnalysisSummary Summarise(PreparedJob job, AnalysisResult analysis) => new(
        job.InputSha256,
        job.DrillJob.Points.Count,
        analysis.Selection == RouteSelection.Candidate ? "candidate" : "baseline",
        analysis.BaselineCheck.Distance.Millimetres,
        analysis.CandidateCheck.Distance.Millimetres,
        analysis.DeltaPercent,
        analysis.Warnings.Select(w => w.Code).ToArray());
}
