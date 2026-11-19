using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Command: runs a synchronous analysis on a stored input and records the run. Repeating an idempotency key
/// returns the original run.
/// </summary>
public sealed class AnalysisRunService
{
    private const int MaxIdempotencyKeyLength = 128;

    private readonly ProjectAccess _access;
    private readonly IProjectRepository _repository;
    private readonly ProjectArtifacts _artifacts;
    private readonly ProjectAuditor _auditor;
    private readonly AnalyzeJobHandler _analyzeHandler;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the service.</summary>
    /// <param name="access">Authorises the caller and resolves the project.</param>
    /// <param name="repository">Resolves inputs and stores runs.</param>
    /// <param name="artifacts">Loads input bytes and stores reports.</param>
    /// <param name="auditor">Records the run event.</param>
    /// <param name="analyzeHandler">Shared analysis handler (same as the CLI).</param>
    /// <param name="timeProvider">Clock for run timestamps.</param>
    public AnalysisRunService(ProjectAccess access, IProjectRepository repository, ProjectArtifacts artifacts, ProjectAuditor auditor, AnalyzeJobHandler analyzeHandler, TimeProvider timeProvider)
    {
        _access = access;
        _repository = repository;
        _artifacts = artifacts;
        _auditor = auditor;
        _analyzeHandler = analyzeHandler;
        _timeProvider = timeProvider;
    }

    /// <summary>Runs an analysis.</summary>
    /// <param name="projectId">Project of the input.</param>
    /// <param name="inputId">Input to analyse.</param>
    /// <param name="idempotencyKey">Client idempotency key; empty for none.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The run, or the failure.</returns>
    public async Task<Result<AnalysisRun>> RunAsync(Guid projectId, Guid inputId, string idempotencyKey, CancellationToken cancellationToken)
    {
        string key = idempotencyKey ?? string.Empty;
        Result<InputArtifact> input = ResolveInput(projectId, inputId, key);
        if (!input.IsSuccess)
        {
            return input.FailAs<AnalysisRun>();
        }

        if (key.Length > 0 && _repository.FindRunByIdempotencyKey(_access.TenantId, projectId, key) is { IsSuccess: true } existing)
        {
            return MatchExistingRun(existing.Value, inputId);
        }

        Result<JobInput> loaded = await _artifacts.LoadInputAsync(input.Value, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return loaded.FailAs<AnalysisRun>();
        }

        Result<AnalysisReport> report = _analyzeHandler.Handle(loaded.Value, cancellationToken);
        var origin = new RunOrigin(input.Value, key, _access.UserId, _timeProvider.GetUtcNow());
        return await RecordRunAsync(origin, report, cancellationToken).ConfigureAwait(false);
    }

    private Result<InputArtifact> ResolveInput(Guid projectId, Guid inputId, string idempotencyKey)
    {
        Result<Project> project = _access.AuthorizeProject(ProjectAction.RunAnalysis, projectId);
        if (!project.IsSuccess)
        {
            return project.FailAs<InputArtifact>();
        }

        if (idempotencyKey.Length > MaxIdempotencyKeyLength || idempotencyKey.Any(char.IsControl))
        {
            return Result<InputArtifact>.Failure(ErrorCodes.InputInvalidParameter, $"Idempotency-Key must be at most {MaxIdempotencyKeyLength} printable characters.");
        }

        return _repository.FindInput(_access.TenantId, projectId, inputId);
    }

    private async Task<Result<AnalysisRun>> RecordRunAsync(RunOrigin origin, Result<AnalysisReport> report, CancellationToken cancellationToken)
    {
        AnalysisRun run = report.IsSuccess
            ? AnalysisRun.Succeeded(Guid.NewGuid(), origin, report.Value.Summary)
            : AnalysisRun.Failed(Guid.NewGuid(), origin, report.Outcome.Errors[0].Code);
        if (report.IsSuccess)
        {
            await _artifacts.SaveReportAsync(run, report.Value.Content, cancellationToken).ConfigureAwait(false);
        }

        AnalysisRun stored = _repository.AddRunIfAbsent(run);
        if (stored.Id != run.Id)
        {
            return MatchExistingRun(stored, origin.InputId);
        }

        _auditor.Record("analysis.run", run.Id);
        return report.IsSuccess ? Result<AnalysisRun>.Success(run) : report.FailAs<AnalysisRun>();
    }

    private static Result<AnalysisRun> MatchExistingRun(AnalysisRun existing, Guid inputId) =>
        existing.InputId == inputId
            ? Result<AnalysisRun>.Success(existing)
            : Result<AnalysisRun>.Failure(ErrorCodes.IdempotencyConflict, "Idempotency-Key was already used for a different input.");
}
