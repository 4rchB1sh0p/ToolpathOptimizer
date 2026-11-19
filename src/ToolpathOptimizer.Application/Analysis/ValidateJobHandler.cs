using Microsoft.Extensions.Logging;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Core.Validation;

namespace ToolpathOptimizer.Application.Analysis;

/// <summary>
/// Counts describing a valid job.
/// </summary>
public sealed class JobValidationSummary
{
    /// <summary>Creates a summary.</summary>
    /// <param name="inputSha256">Canonical input hash.</param>
    /// <param name="pointCount">Number of points.</param>
    /// <param name="precedenceEdgeCount">Number of precedence edges.</param>
    /// <param name="lockedGroupCount">Number of locked groups.</param>
    public JobValidationSummary(string inputSha256, int pointCount, int precedenceEdgeCount, int lockedGroupCount)
    {
        InputSha256 = inputSha256;
        PointCount = pointCount;
        PrecedenceEdgeCount = precedenceEdgeCount;
        LockedGroupCount = lockedGroupCount;
    }

    /// <summary>Gets the canonical input hash.</summary>
    public string InputSha256 { get; }

    /// <summary>Gets the number of points.</summary>
    public int PointCount { get; }

    /// <summary>Gets the number of precedence edges.</summary>
    public int PrecedenceEdgeCount { get; }

    /// <summary>Gets the number of locked groups.</summary>
    public int LockedGroupCount { get; }
}

/// <summary>
/// Query: validates inputs and the constraint graph without optimising.
/// </summary>
public sealed class ValidateJobHandler
{
    private readonly JobPreparer _preparer;
    private readonly ILogger<ValidateJobHandler> _logger;

    /// <summary>Creates the handler.</summary>
    /// <param name="preparer">Input preparer.</param>
    /// <param name="logger">Logger.</param>
    public ValidateJobHandler(JobPreparer preparer, ILogger<ValidateJobHandler> logger)
    {
        _preparer = preparer;
        _logger = logger;
    }

    /// <summary>Validates a job.</summary>
    /// <param name="input">Raw inputs.</param>
    /// <returns>A summary, or the validation failure.</returns>
    public Result<JobValidationSummary> Handle(JobInput input)
    {
        Result<PreparedJob> prepared = _preparer.Prepare(input);
        if (!prepared.IsSuccess)
        {
            ApplicationLog.ValidationFinished(_logger, prepared.Outcome.Errors[0].Code, 0);
            return Result<JobValidationSummary>.Failure(prepared.Outcome);
        }

        PreparedJob job = prepared.Value;
        ValidationResult validation = JobValidator.Validate(job.DrillJob);
        if (!validation.IsValid)
        {
            ApplicationLog.ValidationFinished(_logger, validation.Issues[0].Code, job.DrillJob.Points.Count);
            return Result<JobValidationSummary>.Failure(OperationOutcome.Failure(validation.Issues.Select(i => new OutcomeError(i.Code, i.Message)).ToArray()));
        }

        ApplicationLog.ValidationFinished(_logger, "OK", job.DrillJob.Points.Count);
        return Result<JobValidationSummary>.Success(new JobValidationSummary(
            job.InputSha256,
            job.DrillJob.Points.Count,
            job.DrillJob.Constraints.Precedence.Count,
            job.DrillJob.Constraints.LockedGroups.Distinct(StringComparer.Ordinal).Count()));
    }
}
