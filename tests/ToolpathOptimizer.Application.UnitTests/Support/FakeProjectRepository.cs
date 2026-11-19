using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Application.UnitTests.Support;

/// <summary>Simple list-backed repository; every lookup filters by tenant like a real adapter must.</summary>
internal sealed class FakeProjectRepository : IProjectRepository
{
    private readonly List<Project> _projects = [];
    private readonly List<InputArtifact> _inputs = [];
    private readonly List<AnalysisRun> _runs = [];

    /// <inheritdoc />
    public void AddProject(Project project) => _projects.Add(project);

    /// <inheritdoc />
    public Result<Project> FindProject(string tenantId, Guid projectId) =>
        Found(_projects.FirstOrDefault(p => p.TenantId == tenantId && p.Id == projectId), "Project");

    /// <inheritdoc />
    public IReadOnlyList<Project> ListProjects(string tenantId) => _projects.Where(p => p.TenantId == tenantId).ToArray();

    /// <inheritdoc />
    public void DeleteProject(string tenantId, Guid projectId)
    {
        _projects.RemoveAll(p => p.TenantId == tenantId && p.Id == projectId);
        _inputs.RemoveAll(i => i.TenantId == tenantId && i.ProjectId == projectId);
        _runs.RemoveAll(r => r.TenantId == tenantId && r.ProjectId == projectId);
    }

    /// <inheritdoc />
    public void AddInput(InputArtifact input) => _inputs.Add(input);

    /// <inheritdoc />
    public Result<InputArtifact> FindInput(string tenantId, Guid projectId, Guid inputId) =>
        Found(_inputs.FirstOrDefault(i => i.TenantId == tenantId && i.ProjectId == projectId && i.Id == inputId), "Input");

    /// <inheritdoc />
    public IReadOnlyList<InputArtifact> ListInputs(string tenantId, Guid projectId) =>
        _inputs.Where(i => i.TenantId == tenantId && i.ProjectId == projectId).ToArray();

    /// <inheritdoc />
    public AnalysisRun AddRunIfAbsent(AnalysisRun run)
    {
        Result<AnalysisRun> existing = FindRunByIdempotencyKey(run.TenantId, run.ProjectId, run.IdempotencyKey);
        if (run.IdempotencyKey.Length > 0 && existing.IsSuccess)
        {
            return existing.Value;
        }

        _runs.Add(run);
        return run;
    }

    /// <inheritdoc />
    public Result<AnalysisRun> FindRunByIdempotencyKey(string tenantId, Guid projectId, string idempotencyKey) =>
        Found(_runs.FirstOrDefault(r => r.TenantId == tenantId && r.ProjectId == projectId && r.IdempotencyKey.Length > 0 && r.IdempotencyKey == idempotencyKey), "Run");

    /// <inheritdoc />
    public Result<AnalysisRun> FindRun(string tenantId, Guid projectId, Guid runId) =>
        Found(_runs.FirstOrDefault(r => r.TenantId == tenantId && r.ProjectId == projectId && r.Id == runId), "Run");

    /// <inheritdoc />
    public IReadOnlyList<AnalysisRun> ListRuns(string tenantId, Guid projectId) =>
        _runs.Where(r => r.TenantId == tenantId && r.ProjectId == projectId).ToArray();

    private static Result<T> Found<T>(T? item, string resourceKind)
        where T : class =>
        item is null ? Result<T>.NotFound(resourceKind) : Result<T>.Success(item);
}
