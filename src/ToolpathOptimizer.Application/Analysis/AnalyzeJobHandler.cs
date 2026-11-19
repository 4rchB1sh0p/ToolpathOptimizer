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
/// Command: parses, validates, optimises, verifies and serialises an advisory report.
/// Shared by CLI and Web so both produce identical bytes for the same input and clock.
/// </summary>
public sealed class AnalyzeJobHandler
{
    private readonly JobPreparer _preparer;
    private readonly ProvenanceFactory _provenanceFactory;
    private readonly IOptions<AnalysisLimitsOptions> _limits;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AnalyzeJobHandler> _logger;

    /// <summary>Creates the handler.</summary>
    /// <param name="preparer">Parses, maps and hashes raw inputs.</param>
    /// <param name="provenanceFactory">Stamps reports with hashes, tool identity and time.</param>
    /// <param name="limits">Configured limits; supplies the wall-clock safety deadline.</param>
    /// <param name="timeProvider">Clock for the deadline and logged durations.</param>
    /// <param name="logger">Receives counts, hashes, durations and codes only.</param>
    public AnalyzeJobHandler(JobPreparer preparer, ProvenanceFactory provenanceFactory, IOptions<AnalysisLimitsOptions> limits, TimeProvider timeProvider, ILogger<AnalyzeJobHandler> logger)
    {
        _preparer = preparer;
        _provenanceFactory = provenanceFactory;
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
        Result<AnalysisReport> result = AnalyseInput(input, cancellationToken);
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

    private Result<AnalysisReport> AnalyseInput(JobInput input, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Result<AnalysisReport>.Failure(ErrorCodes.Cancelled, ErrorMessages.Cancelled);
        }

        Result<PreparedJob> prepared = _preparer.Prepare(input);
        if (!prepared.IsSuccess)
        {
            return prepared.FailAs<AnalysisReport>();
        }

        Result<AnalysisResult> analysis = RunEngineWithinDeadline(prepared.Value, cancellationToken);
        return analysis.IsSuccess
            ? Result<AnalysisReport>.Success(CreateReport(prepared.Value, analysis.Value))
            : analysis.FailAs<AnalysisReport>();
    }

    private Result<AnalysisResult> RunEngineWithinDeadline(PreparedJob job, CancellationToken cancellationToken)
    {
        TimeSpan deadline = _limits.Value.Deadline;
        using var deadlineSource = new CancellationTokenSource(deadline, _timeProvider);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadlineSource.Token);
        try
        {
            AnalysisOutcome outcome = AnalysisEngine.Analyse(job.DrillJob, CreateOptions(job.CanonicalJob.Manifest.Parameters), linkedSource.Token);
            return outcome.IsSuccess
                ? Result<AnalysisResult>.Success(outcome.Result)
                : Result<AnalysisResult>.Failure(OperationOutcome.Failure(outcome.Issues.Select(issue => new OutcomeError(issue.Code, issue.Message)).ToArray()));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<AnalysisResult>.Failure(ErrorCodes.Cancelled, ErrorMessages.Cancelled);
        }
        catch (OperationCanceledException) when (deadlineSource.IsCancellationRequested)
        {
            return Result<AnalysisResult>.Failure(ErrorCodes.OptDeadlineExceeded, $"The safety deadline of {deadline.TotalSeconds} s was reached; lower the search budgets and rerun.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ApplicationLog.AnalysisDefect(_logger, ErrorCodes.InternalError, ex);
            return Result<AnalysisResult>.Failure(ErrorCodes.InternalError, "An internal error occurred during analysis.");
        }
    }

    private static AnalysisOptions CreateOptions(AlgorithmParameters parameters) => new(
        new SearchBudget(parameters.MaxPasses, parameters.MaxMoveEvaluations),
        Length.FromMillimetres(parameters.MinImprovementMm));

    private AnalysisReport CreateReport(PreparedJob job, AnalysisResult analysis)
    {
        ReportProvenance provenance = _provenanceFactory.Create(job.InputSha256, job.SourceFiles);
        byte[] content = AnalysisReportWriter.Serialize(job.CanonicalJob, provenance, analysis);
        return new AnalysisReport(content, Summarise(job, analysis));
    }

    private static AnalysisSummary Summarise(PreparedJob job, AnalysisResult analysis) => new(
        job.InputSha256,
        job.DrillJob.Points.Count,
        ContractNames.ForSelection(analysis.Selection),
        analysis.BaselineCheck.Distance.Millimetres,
        analysis.CandidateCheck.Distance.Millimetres,
        analysis.DeltaPercent,
        analysis.Warnings.Select(warning => warning.Code).ToArray());
}
