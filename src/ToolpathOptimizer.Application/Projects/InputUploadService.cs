using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Command: validates an uploaded input and stores it as an immutable artifact.
/// </summary>
public sealed class InputUploadService
{
    private readonly ProjectAccess _access;
    private readonly IProjectRepository _repository;
    private readonly ProjectArtifacts _artifacts;
    private readonly IEntitlementService _entitlements;
    private readonly ProjectAuditor _auditor;
    private readonly ValidateJobHandler _validateHandler;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the service.</summary>
    /// <param name="access">Authorises the caller and resolves the project.</param>
    /// <param name="repository">Stores the input record.</param>
    /// <param name="artifacts">Stores the raw input bytes.</param>
    /// <param name="entitlements">Enforces the plan's job-size quota.</param>
    /// <param name="auditor">Records the upload event.</param>
    /// <param name="validateHandler">Rejects invalid or infeasible inputs before storage.</param>
    /// <param name="timeProvider">Clock for upload timestamps.</param>
    public InputUploadService(
        ProjectAccess access,
        IProjectRepository repository,
        ProjectArtifacts artifacts,
        IEntitlementService entitlements,
        ProjectAuditor auditor,
        ValidateJobHandler validateHandler,
        TimeProvider timeProvider)
    {
        _access = access;
        _repository = repository;
        _artifacts = artifacts;
        _entitlements = entitlements;
        _auditor = auditor;
        _validateHandler = validateHandler;
        _timeProvider = timeProvider;
    }

    /// <summary>Validates and stores an immutable input.</summary>
    /// <param name="projectId">Project to add the input to.</param>
    /// <param name="input">Raw input bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The input artifact, or the failure.</returns>
    public async Task<Result<InputArtifact>> UploadAsync(Guid projectId, JobInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        Result<Project> project = _access.AuthorizeProject(ProjectAction.UploadInput, projectId);
        if (!project.IsSuccess)
        {
            return project.FailAs<InputArtifact>();
        }

        Result<JobValidationSummary> validation = _validateHandler.Handle(input);
        if (!validation.IsSuccess)
        {
            return validation.FailAs<InputArtifact>();
        }

        OperationOutcome entitled = _entitlements.CheckAnalysisAllowed(_access.TenantId, validation.Value.PointCount);
        if (!entitled.IsSuccess)
        {
            return Result<InputArtifact>.Failure(entitled);
        }

        var artifact = new InputArtifact(Guid.NewGuid(), projectId, _access.TenantId, validation.Value.InputSha256, validation.Value.PointCount, _access.UserId, _timeProvider.GetUtcNow());
        await _artifacts.SaveInputAsync(artifact, input, cancellationToken).ConfigureAwait(false);
        _repository.AddInput(artifact);
        _auditor.Record("input.uploaded", artifact.Id);
        return Result<InputArtifact>.Success(artifact);
    }
}
