using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Commands on projects: create, upload input, run analysis, delete. Tenant and role checks first.
/// </summary>
public sealed class ProjectCommandService
{
    private const int MaxNameLength = 100;
    private const int MaxIdempotencyKeyLength = 128;

    private readonly ProjectAccess _access;
    private readonly IProjectRepository _repository;
    private readonly IArtifactStore _artifacts;
    private readonly IEntitlementService _entitlements;
    private readonly IAuditLog _audit;
    private readonly ValidateJobHandler _validate;
    private readonly AnalyzeJobHandler _analyze;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the service.</summary>
    /// <param name="access">Access helper.</param>
    /// <param name="repository">Repository.</param>
    /// <param name="artifacts">Artifact store.</param>
    /// <param name="entitlements">Entitlements.</param>
    /// <param name="audit">Audit log.</param>
    /// <param name="validate">Validation handler.</param>
    /// <param name="analyze">Analysis handler.</param>
    /// <param name="timeProvider">Clock.</param>
    public ProjectCommandService(
        ProjectAccess access,
        IProjectRepository repository,
        IArtifactStore artifacts,
        IEntitlementService entitlements,
        IAuditLog audit,
        ValidateJobHandler validate,
        AnalyzeJobHandler analyze,
        TimeProvider timeProvider)
    {
        _access = access;
        _repository = repository;
        _artifacts = artifacts;
        _entitlements = entitlements;
        _audit = audit;
        _validate = validate;
        _analyze = analyze;
        _timeProvider = timeProvider;
    }

    /// <summary>Creates a project.</summary>
    /// <param name="name">Display name (1–100 printable characters).</param>
    /// <returns>The project, or the failure.</returns>
    public Result<Project> CreateProject(string name)
    {
        OperationOutcome authorized = _access.Authorize(ProjectAction.CreateProject);
        if (!authorized.IsSuccess)
        {
            return Result<Project>.Failure(authorized);
        }

        string trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > MaxNameLength || trimmed.Any(char.IsControl))
        {
            return Result<Project>.Failure(ErrorCodes.InputInvalidParameter, $"Project name must be 1–{MaxNameLength} printable characters.");
        }

        var project = new Project(Guid.NewGuid(), _access.TenantId, trimmed, _access.UserId, _timeProvider.GetUtcNow());
        _repository.AddProject(project);
        Audit("project.created", project.Id);
        return Result<Project>.Success(project);
    }

    /// <summary>Validates and stores an immutable input.</summary>
    /// <param name="projectId">Project.</param>
    /// <param name="input">Raw inputs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The input artifact, or the failure.</returns>
    public async Task<Result<InputArtifact>> UploadInputAsync(Guid projectId, JobInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        Result<Project> project = _access.AuthorizeProject(ProjectAction.UploadInput, projectId);
        if (!project.IsSuccess)
        {
            return Result<InputArtifact>.Failure(project.Outcome);
        }

        Result<JobValidationSummary> validation = _validate.Handle(input);
        if (!validation.IsSuccess)
        {
            return Result<InputArtifact>.Failure(validation.Outcome);
        }

        OperationOutcome entitled = _entitlements.CheckAnalysisAllowed(_access.TenantId, validation.Value.PointCount);
        if (!entitled.IsSuccess)
        {
            return Result<InputArtifact>.Failure(entitled);
        }

        var artifact = new InputArtifact(Guid.NewGuid(), projectId, _access.TenantId, validation.Value.InputSha256, validation.Value.PointCount, _access.UserId, _timeProvider.GetUtcNow());
        await _artifacts.PutAsync(_access.TenantId, artifact.PointsKey, input.PointsCsv, cancellationToken).ConfigureAwait(false);
        await _artifacts.PutAsync(_access.TenantId, artifact.ManifestKey, input.ManifestJson, cancellationToken).ConfigureAwait(false);
        _repository.AddInput(artifact);
        Audit("input.uploaded", artifact.Id);
        return Result<InputArtifact>.Success(artifact);
    }

    /// <summary>Runs an analysis synchronously; repeated keys return the original run.</summary>
    /// <param name="projectId">Project.</param>
    /// <param name="inputId">Input to analyse.</param>
    /// <param name="idempotencyKey">Optional idempotency key; empty for none.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    /// <returns>The run, or the failure.</returns>
    public async Task<Result<AnalysisRun>> RunAnalysisAsync(Guid projectId, Guid inputId, string idempotencyKey, CancellationToken cancellationToken)
    {
        Result<Project> project = _access.AuthorizeProject(ProjectAction.RunAnalysis, projectId);
        if (!project.IsSuccess)
        {
            return Result<AnalysisRun>.Failure(project.Outcome);
        }

        idempotencyKey ??= string.Empty;
        if (idempotencyKey.Length > MaxIdempotencyKeyLength || idempotencyKey.Any(char.IsControl))
        {
            return Result<AnalysisRun>.Failure(ErrorCodes.InputInvalidParameter, $"Idempotency-Key must be at most {MaxIdempotencyKeyLength} printable characters.");
        }

        InputArtifact? artifact = _repository.FindInput(_access.TenantId, projectId, inputId);
        if (artifact is null)
        {
            return ProjectAccess.NotFound<AnalysisRun>("Input");
        }

        if (idempotencyKey.Length > 0 && _repository.FindRunByIdempotencyKey(_access.TenantId, projectId, idempotencyKey) is AnalysisRun existing)
        {
            return ReturnExisting(existing, inputId);
        }

        byte[]? points = await _artifacts.GetAsync(_access.TenantId, artifact.PointsKey, cancellationToken).ConfigureAwait(false);
        byte[]? manifest = await _artifacts.GetAsync(_access.TenantId, artifact.ManifestKey, cancellationToken).ConfigureAwait(false);
        if (points is null || manifest is null)
        {
            return Result<AnalysisRun>.Failure(ErrorCodes.IoReadFailed, "Stored input artifact is missing.");
        }

        Result<AnalysisReport> report = _analyze.Handle(new JobInput(points, manifest), cancellationToken);
        var run = new AnalysisRun(
            Guid.NewGuid(), projectId, _access.TenantId, inputId, idempotencyKey,
            report.IsSuccess ? RunStatus.Succeeded : RunStatus.Failed,
            report.IsSuccess ? string.Empty : report.Outcome.Errors[0].Code,
            report.IsSuccess ? report.Value.Summary : null,
            _access.UserId, _timeProvider.GetUtcNow());

        if (report.IsSuccess)
        {
            await _artifacts.PutAsync(_access.TenantId, run.ReportKey, report.Value.Content, cancellationToken).ConfigureAwait(false);
        }

        AnalysisRun stored = _repository.AddRunIfAbsent(run);
        if (stored.Id != run.Id)
        {
            return ReturnExisting(stored, inputId);
        }

        Audit("analysis.run", run.Id);
        return report.IsSuccess ? Result<AnalysisRun>.Success(run) : Result<AnalysisRun>.Failure(report.Outcome);
    }

    /// <summary>Deletes a project, its inputs, runs and artifacts.</summary>
    /// <param name="projectId">Project.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or the failure.</returns>
    public async Task<OperationOutcome> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        Result<Project> project = _access.AuthorizeProject(ProjectAction.DeleteProject, projectId);
        if (!project.IsSuccess)
        {
            return project.Outcome;
        }

        await _artifacts.DeletePrefixAsync(_access.TenantId, $"projects/{projectId:N}/", cancellationToken).ConfigureAwait(false);
        _repository.DeleteProject(_access.TenantId, projectId);
        Audit("project.deleted", projectId);
        return OperationOutcome.Success;
    }

    private static Result<AnalysisRun> ReturnExisting(AnalysisRun existing, Guid inputId) =>
        existing.InputId == inputId
            ? Result<AnalysisRun>.Success(existing)
            : Result<AnalysisRun>.Failure(ErrorCodes.IdempotencyConflict, "Idempotency-Key was already used for a different input.");

    private void Audit(string action, Guid resourceId) =>
        _audit.Record(new AuditEvent(_access.TenantId, _access.UserId, action, resourceId.ToString("N"), _timeProvider.GetUtcNow()));
}
