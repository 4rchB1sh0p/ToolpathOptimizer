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
    private readonly Dictionary<(string Tenant, Guid Project), List<InputArtifact>> _inputs = [];
    private readonly Dictionary<(string Tenant, Guid Project), List<AnalysisRun>> _runs = [];

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
    public Project? FindProject(string tenantId, Guid projectId)
    {
        lock (_gate)
        {
            return _projects.GetValueOrDefault((tenantId, projectId));
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
            _inputs.Remove((tenantId, projectId));
            _runs.Remove((tenantId, projectId));
        }
    }

    /// <inheritdoc />
    public void AddInput(InputArtifact input)
    {
        ArgumentNullException.ThrowIfNull(input);
        lock (_gate)
        {
            ListFor(_inputs, input.TenantId, input.ProjectId).Add(input);
        }
    }

    /// <inheritdoc />
    public InputArtifact? FindInput(string tenantId, Guid projectId, Guid inputId)
    {
        lock (_gate)
        {
            return _inputs.GetValueOrDefault((tenantId, projectId))?.FirstOrDefault(i => i.Id == inputId);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<InputArtifact> ListInputs(string tenantId, Guid projectId)
    {
        lock (_gate)
        {
            return _inputs.GetValueOrDefault((tenantId, projectId))?.ToArray() ?? [];
        }
    }

    /// <inheritdoc />
    public AnalysisRun AddRunIfAbsent(AnalysisRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        lock (_gate)
        {
            List<AnalysisRun> runs = ListFor(_runs, run.TenantId, run.ProjectId);
            AnalysisRun? existing = run.IdempotencyKey.Length == 0
                ? null
                : runs.FirstOrDefault(r => string.Equals(r.IdempotencyKey, run.IdempotencyKey, StringComparison.Ordinal));
            if (existing is not null)
            {
                return existing;
            }

            runs.Add(run);
            return run;
        }
    }

    /// <inheritdoc />
    public AnalysisRun? FindRunByIdempotencyKey(string tenantId, Guid projectId, string idempotencyKey)
    {
        lock (_gate)
        {
            return _runs.GetValueOrDefault((tenantId, projectId))?
                .FirstOrDefault(r => r.IdempotencyKey.Length > 0 && string.Equals(r.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));
        }
    }

    /// <inheritdoc />
    public AnalysisRun? FindRun(string tenantId, Guid projectId, Guid runId)
    {
        lock (_gate)
        {
            return _runs.GetValueOrDefault((tenantId, projectId))?.FirstOrDefault(r => r.Id == runId);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AnalysisRun> ListRuns(string tenantId, Guid projectId)
    {
        lock (_gate)
        {
            return _runs.GetValueOrDefault((tenantId, projectId))?.ToArray() ?? [];
        }
    }

    private static List<T> ListFor<T>(Dictionary<(string Tenant, Guid Project), List<T>> store, string tenantId, Guid projectId)
    {
        if (!store.TryGetValue((tenantId, projectId), out List<T>? list))
        {
            list = [];
            store.Add((tenantId, projectId), list);
        }

        return list;
    }
}
