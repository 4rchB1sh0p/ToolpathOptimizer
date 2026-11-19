using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Commands on the project itself: create and delete. Inputs and runs have their own services.
/// </summary>
public sealed class ProjectCommandService
{
    private const int MaxNameLength = 100;

    private readonly ProjectAccess _access;
    private readonly IProjectRepository _repository;
    private readonly ProjectArtifacts _artifacts;
    private readonly ProjectAuditor _auditor;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the service.</summary>
    /// <param name="access">Authorises the caller and resolves projects in their tenant.</param>
    /// <param name="repository">Stores and deletes project records.</param>
    /// <param name="artifacts">Deletes stored inputs and reports.</param>
    /// <param name="auditor">Records create and delete events.</param>
    /// <param name="timeProvider">Clock for creation timestamps.</param>
    public ProjectCommandService(ProjectAccess access, IProjectRepository repository, ProjectArtifacts artifacts, ProjectAuditor auditor, TimeProvider timeProvider)
    {
        _access = access;
        _repository = repository;
        _artifacts = artifacts;
        _auditor = auditor;
        _timeProvider = timeProvider;
    }

    /// <summary>Creates a project in the caller's tenant.</summary>
    /// <param name="name">Display name (1–100 printable characters after trimming).</param>
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
        _auditor.Record("project.created", project.Id);
        return Result<Project>.Success(project);
    }

    /// <summary>Deletes a project, its inputs, runs and artifacts (Owner only).</summary>
    /// <param name="projectId">Project to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or the failure.</returns>
    public async Task<OperationOutcome> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        Result<Project> project = _access.AuthorizeProject(ProjectAction.DeleteProject, projectId);
        if (!project.IsSuccess)
        {
            return project.Outcome;
        }

        await _artifacts.DeleteProjectAsync(project.Value, cancellationToken).ConfigureAwait(false);
        _repository.DeleteProject(_access.TenantId, projectId);
        _auditor.Record("project.deleted", projectId);
        return OperationOutcome.Success;
    }
}
