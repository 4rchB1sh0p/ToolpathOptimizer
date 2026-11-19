using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Infrastructure.Persistence;

/// <summary>
/// Thread-safe in-memory repository for development and tests. Every lookup is keyed by tenant first.
/// Data is lost on restart; a database adapter replaces it once TD-005 is decided.
/// </summary>
public sealed class InMemoryProjectRepository : IProjectRepository
{
    private readonly Lock _gate = new();
    private readonly Dictionary<(string Tenant, Guid Project), Project> _projects = [];
    private readonly Dictionary<(string Tenant, Guid Project), List<InputArtifact>> _inputsByProject = [];
    private readonly Dictionary<(string Tenant, Guid Project), List<AnalysisRun>> _runsByProject = [];

    /// <inheritdoc />
    public void AddProject(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        lock (_gate)
        {
            _projects.Add((project.TenantId, project.Id), project);
        }
    }

    /// <inheritdoc />
    public Result<Project> FindProject(string tenantId, Guid projectId)
    {
        lock (_gate)
        {
            return _projects.TryGetValue((tenantId, projectId), out Project? project)
                ? Result<Project>.Success(project)
                : Result<Project>.NotFound("Project");
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<Project> ListProjects(string tenantId)
    {
        lock (_gate)
        {
            return _projects.Values.Where(p => p.TenantId == tenantId).OrderBy(p => p.CreatedAtUtc).ThenBy(p => p.Id).ToArray();
        }
    }

    /// <inheritdoc />
    public void DeleteProject(string tenantId, Guid projectId)
    {
        lock (_gate)
        {
            _projects.Remove((tenantId, projectId));
            _inputsByProject.Remove((tenantId, projectId));
            _runsByProject.Remove((tenantId, projectId));
        }
    }

    /// <inheritdoc />
    public void AddInput(InputArtifact input)
    {
        ArgumentNullException.ThrowIfNull(input);
        lock (_gate)
        {
            GetOrCreateList(_inputsByProject, input.TenantId, input.ProjectId).Add(input);
        }
    }

    /// <inheritdoc />
    public Result<InputArtifact> FindInput(string tenantId, Guid projectId, Guid inputId)
    {
        lock (_gate)
        {
            InputArtifact? input = ListOrEmpty(_inputsByProject, tenantId, projectId).FirstOrDefault(i => i.Id == inputId);
            return input is null ? Result<InputArtifact>.NotFound("Input") : Result<InputArtifact>.Success(input);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<InputArtifact> ListInputs(string tenantId, Guid projectId)
    {
        lock (_gate)
        {
            return ListOrEmpty(_inputsByProject, tenantId, projectId).ToArray();
        }
    }

    /// <inheritdoc />
    public AnalysisRun AddRunIfAbsent(AnalysisRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        lock (_gate)
        {
            List<AnalysisRun> runs = GetOrCreateList(_runsByProject, run.TenantId, run.ProjectId);
            AnalysisRun? existing = run.IdempotencyKey.Length == 0 ? null : FindByKey(runs, run.IdempotencyKey);
            if (existing is not null)
            {
                return existing;
            }

            runs.Add(run);
            return run;
        }
    }

    /// <inheritdoc />
    public Result<AnalysisRun> FindRunByIdempotencyKey(string tenantId, Guid projectId, string idempotencyKey)
    {
        lock (_gate)
        {
            AnalysisRun? run = idempotencyKey.Length == 0 ? null : FindByKey(ListOrEmpty(_runsByProject, tenantId, projectId), idempotencyKey);
            return run is null ? Result<AnalysisRun>.NotFound("Run") : Result<AnalysisRun>.Success(run);
        }
    }

    /// <inheritdoc />
    public Result<AnalysisRun> FindRun(string tenantId, Guid projectId, Guid runId)
    {
        lock (_gate)
        {
            AnalysisRun? run = ListOrEmpty(_runsByProject, tenantId, projectId).FirstOrDefault(r => r.Id == runId);
            return run is null ? Result<AnalysisRun>.NotFound("Run") : Result<AnalysisRun>.Success(run);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AnalysisRun> ListRuns(string tenantId, Guid projectId)
    {
        lock (_gate)
        {
            return ListOrEmpty(_runsByProject, tenantId, projectId).ToArray();
        }
    }

    private static AnalysisRun? FindByKey(IEnumerable<AnalysisRun> runs, string idempotencyKey) =>
        runs.FirstOrDefault(r => string.Equals(r.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));

    /// <summary>Query: the stored list for a project, or an empty sequence; never creates an entry.</summary>
    private static IEnumerable<T> ListOrEmpty<T>(Dictionary<(string Tenant, Guid Project), List<T>> store, string tenantId, Guid projectId) =>
        store.TryGetValue((tenantId, projectId), out List<T>? list) ? list : [];

    /// <summary>Command: returns the stored list for a project, creating it on first use. Caller must hold the lock.</summary>
    private static List<T> GetOrCreateList<T>(Dictionary<(string Tenant, Guid Project), List<T>> store, string tenantId, Guid projectId)
    {
        if (!store.TryGetValue((tenantId, projectId), out List<T>? list))
        {
            list = [];
            store.Add((tenantId, projectId), list);
        }

        return list;
    }
}
