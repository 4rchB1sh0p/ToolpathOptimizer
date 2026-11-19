using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// A project with its inputs and runs (export view; reports are fetched separately).
/// </summary>
public sealed class ProjectExport
{
    /// <summary>Creates an export.</summary>
    /// <param name="project">Project.</param>
    /// <param name="inputs">Inputs.</param>
    /// <param name="runs">Runs.</param>
    public ProjectExport(Project project, IReadOnlyList<InputArtifact> inputs, IReadOnlyList<AnalysisRun> runs)
    {
        Project = project;
        Inputs = inputs.ToArray();
        Runs = runs.ToArray();
    }

    /// <summary>Gets the project.</summary>
    public Project Project { get; }

    /// <summary>Gets the inputs.</summary>
    public IReadOnlyList<InputArtifact> Inputs { get; }

    /// <summary>Gets the runs.</summary>
    public IReadOnlyList<AnalysisRun> Runs { get; }
}

/// <summary>
/// Queries on projects: list, get run, get report, export. Never mutates state.
/// </summary>
public sealed class ProjectQueryService
{
    private readonly ProjectAccess _access;
    private readonly IProjectRepository _repository;
    private readonly IArtifactStore _artifacts;

    /// <summary>Creates the service.</summary>
    /// <param name="access">Access helper.</param>
    /// <param name="repository">Repository.</param>
    /// <param name="artifacts">Artifact store.</param>
    public ProjectQueryService(ProjectAccess access, IProjectRepository repository, IArtifactStore artifacts)
    {
        _access = access;
        _repository = repository;
        _artifacts = artifacts;
    }

    /// <summary>Lists the tenant's projects.</summary>
    /// <returns>The projects, or the failure.</returns>
    public Result<IReadOnlyList<Project>> ListProjects()
    {
        OperationOutcome authorized = _access.Authorize(ProjectAction.Read);
        return authorized.IsSuccess
            ? Result<IReadOnlyList<Project>>.Success(_repository.ListProjects(_access.TenantId))
            : Result<IReadOnlyList<Project>>.Failure(authorized);
    }

    /// <summary>Gets a run.</summary>
    /// <param name="projectId">Project.</param>
    /// <param name="runId">Run.</param>
    /// <returns>The run, or the failure.</returns>
    public Result<AnalysisRun> GetRun(Guid projectId, Guid runId)
    {
        Result<Project> project = _access.AuthorizeProject(ProjectAction.Read, projectId);
        if (!project.IsSuccess)
        {
            return Result<AnalysisRun>.Failure(project.Outcome);
        }

        AnalysisRun? run = _repository.FindRun(_access.TenantId, projectId, runId);
        return run is null ? ProjectAccess.NotFound<AnalysisRun>("Run") : Result<AnalysisRun>.Success(run);
    }

    /// <summary>Gets a run's report bytes.</summary>
    /// <param name="projectId">Project.</param>
    /// <param name="runId">Run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The report bytes, or the failure.</returns>
    public async Task<Result<byte[]>> GetReportAsync(Guid projectId, Guid runId, CancellationToken cancellationToken)
    {
        Result<AnalysisRun> run = GetRun(projectId, runId);
        if (!run.IsSuccess)
        {
            return Result<byte[]>.Failure(run.Outcome);
        }

        if (run.Value.Status != RunStatus.Succeeded)
        {
            return ProjectAccess.NotFound<byte[]>("Report");
        }

        byte[]? content = await _artifacts.GetAsync(_access.TenantId, run.Value.ReportKey, cancellationToken).ConfigureAwait(false);
        return content is null ? ProjectAccess.NotFound<byte[]>("Report") : Result<byte[]>.Success(content);
    }

    /// <summary>Exports a project's metadata, inputs and runs.</summary>
    /// <param name="projectId">Project.</param>
    /// <returns>The export, or the failure.</returns>
    public Result<ProjectExport> ExportProject(Guid projectId)
    {
        Result<Project> project = _access.AuthorizeProject(ProjectAction.ExportProject, projectId);
        if (!project.IsSuccess)
        {
            return Result<ProjectExport>.Failure(project.Outcome);
        }

        return Result<ProjectExport>.Success(new ProjectExport(
            project.Value,
            _repository.ListInputs(_access.TenantId, projectId),
            _repository.ListRuns(_access.TenantId, projectId)));
    }
}
