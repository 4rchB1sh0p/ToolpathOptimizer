using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Application.Projects;
using ToolpathOptimizer.Application.Provenance;

namespace ToolpathOptimizer.Application.UnitTests;

internal static class Inputs
{
    public const string SquareCsv = "point_id,x_mm,y_mm,sequence,operation_group\nH01,0,0,1,A\nH02,100,0,2,A\nH03,100,100,3,A\nH04,0,100,4,A\n";

    public const string CrossingCsv = "point_id,x_mm,y_mm,sequence,operation_group\nA,0,10,1,\nD,10,20,2,\nC,0,20,3,\nB,10,10,4,\n";

    public const string MinimalManifest =
        """{"schema_version":"1.0","coordinate_system":"XY_CARTESIAN","units":"mm","start_x_mm":0,"start_y_mm":0,"algorithm":{"name":"nearest-neighbor-2opt"}}""";

    public static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    public static JobInput Job(string csv, string manifest = MinimalManifest) => new(Utf8(csv), Utf8(manifest));

    public static string Manifest(string extraFields) =>
        "{\"schema_version\":\"1.0\",\"coordinate_system\":\"XY_CARTESIAN\",\"units\":\"mm\",\"start_x_mm\":0,\"start_y_mm\":0,\"algorithm\":{\"name\":\"nearest-neighbor-2opt\"}" + extraFields + "}";
}

/// <summary>Builds handlers with fakes and a fixed clock.</summary>
internal sealed class Handlers
{
    public Handlers(AnalysisLimitsOptions? limits = null, TimeProvider? timeProvider = null)
    {
        Limits = limits ?? new AnalysisLimitsOptions();
        Clock = timeProvider ?? new FakeTimeProvider(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero));
        IOptions<AnalysisLimitsOptions> options = Options.Create(Limits);
        Preparer = new JobPreparer(options);
        Validate = new ValidateJobHandler(Preparer, NullLogger<ValidateJobHandler>.Instance);
        Analyze = new AnalyzeJobHandler(Preparer, new ProvenanceFactory(Clock, new ToolInfo("toolpath-optimize", "test")), options, Clock, NullLogger<AnalyzeJobHandler>.Instance);
    }

    public AnalysisLimitsOptions Limits { get; }

    public TimeProvider Clock { get; }

    public JobPreparer Preparer { get; }

    public ValidateJobHandler Validate { get; }

    public AnalyzeJobHandler Analyze { get; }
}

/// <summary>A clock whose timers fire immediately, simulating a reached deadline.</summary>
internal sealed class ExpiredDeadlineTimeProvider : TimeProvider
{
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        callback(state);
        return new NoopTimer();
    }

    private sealed class NoopTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

internal sealed class FakePrincipal : ICurrentPrincipal, ITenantContext
{
    public bool IsAuthenticated { get; set; } = true;

    public string UserId { get; set; } = "user-1";

    public Role Role { get; set; } = Role.Engineer;

    public string TenantId { get; set; } = "tenant-a";
}

internal sealed class FakeArtifactStore : IArtifactStore
{
    private readonly Dictionary<string, byte[]> _items = new(StringComparer.Ordinal);

    public Task PutAsync(string tenantId, string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        _items.Add($"{tenantId}/{key}", content.ToArray());
        return Task.CompletedTask;
    }

    public Task<byte[]?> GetAsync(string tenantId, string key, CancellationToken cancellationToken) =>
        Task.FromResult(_items.GetValueOrDefault($"{tenantId}/{key}"));

    public Task DeletePrefixAsync(string tenantId, string prefix, CancellationToken cancellationToken)
    {
        foreach (string key in _items.Keys.Where(k => k.StartsWith($"{tenantId}/{prefix}", StringComparison.Ordinal)).ToArray())
        {
            _items.Remove(key);
        }

        return Task.CompletedTask;
    }
}

internal sealed class FakeProjectRepository : IProjectRepository
{
    private readonly List<Project> _projects = [];
    private readonly List<InputArtifact> _inputs = [];
    private readonly List<AnalysisRun> _runs = [];

    public void AddProject(Project project) => _projects.Add(project);

    public Project? FindProject(string tenantId, Guid projectId) => _projects.FirstOrDefault(p => p.TenantId == tenantId && p.Id == projectId);

    public IReadOnlyList<Project> ListProjects(string tenantId) => _projects.Where(p => p.TenantId == tenantId).ToArray();

    public void DeleteProject(string tenantId, Guid projectId)
    {
        _projects.RemoveAll(p => p.TenantId == tenantId && p.Id == projectId);
        _inputs.RemoveAll(i => i.TenantId == tenantId && i.ProjectId == projectId);
        _runs.RemoveAll(r => r.TenantId == tenantId && r.ProjectId == projectId);
    }

    public void AddInput(InputArtifact input) => _inputs.Add(input);

    public InputArtifact? FindInput(string tenantId, Guid projectId, Guid inputId) =>
        _inputs.FirstOrDefault(i => i.TenantId == tenantId && i.ProjectId == projectId && i.Id == inputId);

    public IReadOnlyList<InputArtifact> ListInputs(string tenantId, Guid projectId) =>
        _inputs.Where(i => i.TenantId == tenantId && i.ProjectId == projectId).ToArray();

    public AnalysisRun AddRunIfAbsent(AnalysisRun run)
    {
        AnalysisRun? existing = run.IdempotencyKey.Length > 0 ? FindRunByIdempotencyKey(run.TenantId, run.ProjectId, run.IdempotencyKey) : null;
        if (existing is not null)
        {
            return existing;
        }

        _runs.Add(run);
        return run;
    }

    public AnalysisRun? FindRunByIdempotencyKey(string tenantId, Guid projectId, string idempotencyKey) =>
        _runs.FirstOrDefault(r => r.TenantId == tenantId && r.ProjectId == projectId && r.IdempotencyKey == idempotencyKey);

    public AnalysisRun? FindRun(string tenantId, Guid projectId, Guid runId) =>
        _runs.FirstOrDefault(r => r.TenantId == tenantId && r.ProjectId == projectId && r.Id == runId);

    public IReadOnlyList<AnalysisRun> ListRuns(string tenantId, Guid projectId) =>
        _runs.Where(r => r.TenantId == tenantId && r.ProjectId == projectId).ToArray();
}

internal sealed class AllowAllEntitlements : IEntitlementService
{
    public OperationOutcome CheckAnalysisAllowed(string tenantId, int pointCount) => OperationOutcome.Success;
}

internal sealed class RecordingAuditLog : IAuditLog
{
    public List<AuditEvent> Events { get; } = [];

    public void Record(AuditEvent auditEvent) => Events.Add(auditEvent);
}
