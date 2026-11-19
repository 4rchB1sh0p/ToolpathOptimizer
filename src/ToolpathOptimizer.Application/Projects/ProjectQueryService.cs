using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Queries on projects: list, get run, get report, export. Never mutates state.
/// </summary>
public sealed class ProjectQueryService
{
    private readonly ProjectAccess _access;
    private readonly IProjectRepository _repository;
    private readonly ProjectArtifacts _artifacts;

    /// <summary>Creates the service.</summary>
    /// <param name="access">Authorises the caller and resolves projects in their tenant.</param>
    /// <param name="repository">Reads project, input and run records.</param>
    /// <param name="artifacts">Reads stored reports.</param>
    public ProjectQueryService(ProjectAccess access, IProjectRepository repository, ProjectArtifacts artifacts)
    {
        _access = access;
        _repository = repository;
        _artifacts = artifacts;
    }

    /// <summary>Lists the caller's projects.</summary>
    /// <returns>The projects, or the authorisation failure.</returns>
    public Result<IReadOnlyList<Project>> ListProjects()
    {
        OperationOutcome authorized = _access.Authorize(ProjectAction.Read);
        return authorized.IsSuccess
            ? Result<IReadOnlyList<Project>>.Success(_repository.ListProjects(_access.TenantId))
            : Result<IReadOnlyList<Project>>.Failure(authorized);
    }

    /// <summary>Gets a run.</summary>
    /// <param name="projectId">Project of the run.</param>
    /// <param name="runId">Run to get.</param>
    /// <returns>The run, or the failure.</returns>
    public Result<AnalysisRun> GetRun(Guid projectId, Guid runId)
    {
        Result<Project> project = _access.AuthorizeProject(ProjectAction.Read, projectId);
        return project.IsSuccess ? _repository.FindRun(_access.TenantId, projectId, runId) : project.FailAs<AnalysisRun>();
    }

    /// <summary>Gets a succeeded run's report bytes.</summary>
    /// <param name="projectId">Project of the run.</param>
    /// <param name="runId">Run whose report is requested.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The report bytes, or the failure (failed runs have no report).</returns>
    public async Task<Result<byte[]>> GetReportAsync(Guid projectId, Guid runId, CancellationToken cancellationToken)
    {
        Result<AnalysisRun> run = GetRun(projectId, runId);
        if (!run.IsSuccess)
        {
            return run.FailAs<byte[]>();
        }

        return run.Value.Status == RunStatus.Succeeded
            ? await _artifacts.LoadReportAsync(run.Value, cancellationToken).ConfigureAwait(false)
            : Result<byte[]>.NotFound("Report");
    }

    /// <summary>Exports a project's metadata, inputs and runs (Owner only).</summary>
    /// <param name="projectId">Project to export.</param>
    /// <returns>The export, or the failure.</returns>
    public Result<ProjectExport> ExportProject(Guid projectId)
    {
        Result<Project> project = _access.AuthorizeProject(ProjectAction.ExportProject, projectId);
        if (!project.IsSuccess)
        {
            return project.FailAs<ProjectExport>();
        }

        return Result<ProjectExport>.Success(new ProjectExport(
            project.Value,
            _repository.ListInputs(_access.TenantId, projectId),
            _repository.ListRuns(_access.TenantId, projectId)));
    }
}
