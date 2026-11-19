using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// The authenticated user, derived only from a validated credential.
/// </summary>
public interface ICurrentPrincipal
{
    /// <summary>Gets a value indicating whether a validated identity is present.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Gets the user identifier.</summary>
    string UserId { get; }

    /// <summary>Gets the user's role in the tenant.</summary>
    Role Role { get; }
}

/// <summary>
/// The tenant of the current request, never taken from route, query or body.
/// </summary>
public interface ITenantContext
{
    /// <summary>Gets the tenant identifier; empty when unauthenticated.</summary>
    string TenantId { get; }
}

/// <summary>
/// Plan quotas and credits.
/// </summary>
public interface IEntitlementService
{
    /// <summary>Checks whether the tenant may analyse a job of the given size.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="pointCount">Number of points.</param>
    /// <returns>Success, or <c>QUOTA_EXCEEDED</c>.</returns>
    OperationOutcome CheckAnalysisAllowed(string tenantId, int pointCount);
}

/// <summary>
/// A security-relevant event.
/// </summary>
public sealed class AuditEvent
{
    /// <summary>Creates an audit event.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="actorId">Acting user.</param>
    /// <param name="action">Action name.</param>
    /// <param name="resourceId">Affected resource.</param>
    /// <param name="occurredAtUtc">Time of the event.</param>
    public AuditEvent(string tenantId, string actorId, string action, string resourceId, DateTimeOffset occurredAtUtc)
    {
        TenantId = tenantId;
        ActorId = actorId;
        Action = action;
        ResourceId = resourceId;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>Gets the tenant.</summary>
    public string TenantId { get; }

    /// <summary>Gets the acting user.</summary>
    public string ActorId { get; }

    /// <summary>Gets the action name.</summary>
    public string Action { get; }

    /// <summary>Gets the affected resource.</summary>
    public string ResourceId { get; }

    /// <summary>Gets the time of the event.</summary>
    public DateTimeOffset OccurredAtUtc { get; }
}

/// <summary>
/// Records security-relevant events.
/// </summary>
public interface IAuditLog
{
    /// <summary>Records an event.</summary>
    /// <param name="auditEvent">The event.</param>
    void Record(AuditEvent auditEvent);
}

/// <summary>
/// Tenant-scoped persistence of projects, input artifacts and analysis runs. Every method filters by tenant.
/// </summary>
public interface IProjectRepository
{
    /// <summary>Adds a project.</summary>
    /// <param name="project">The project.</param>
    void AddProject(Project project);

    /// <summary>Finds a project.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="projectId">Project.</param>
    /// <returns>The project, or <c>null</c> when absent for this tenant.</returns>
    Project? FindProject(string tenantId, Guid projectId);

    /// <summary>Lists a tenant's projects ordered by creation time.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <returns>The projects.</returns>
    IReadOnlyList<Project> ListProjects(string tenantId);

    /// <summary>Deletes a project with its inputs and runs.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="projectId">Project.</param>
    void DeleteProject(string tenantId, Guid projectId);

    /// <summary>Adds an input artifact.</summary>
    /// <param name="input">The input.</param>
    void AddInput(InputArtifact input);

    /// <summary>Finds an input artifact.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="projectId">Project.</param>
    /// <param name="inputId">Input.</param>
    /// <returns>The input, or <c>null</c>.</returns>
    InputArtifact? FindInput(string tenantId, Guid projectId, Guid inputId);

    /// <summary>Lists a project's inputs.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="projectId">Project.</param>
    /// <returns>The inputs.</returns>
    IReadOnlyList<InputArtifact> ListInputs(string tenantId, Guid projectId);

    /// <summary>Adds a run unless one with the same idempotency key exists, atomically.</summary>
    /// <param name="run">The run to add.</param>
    /// <returns>The stored run (the existing one when the key was already used).</returns>
    AnalysisRun AddRunIfAbsent(AnalysisRun run);

    /// <summary>Finds a run by idempotency key.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="projectId">Project.</param>
    /// <param name="idempotencyKey">Key.</param>
    /// <returns>The run, or <c>null</c>.</returns>
    AnalysisRun? FindRunByIdempotencyKey(string tenantId, Guid projectId, string idempotencyKey);

    /// <summary>Finds a run.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="projectId">Project.</param>
    /// <param name="runId">Run.</param>
    /// <returns>The run, or <c>null</c>.</returns>
    AnalysisRun? FindRun(string tenantId, Guid projectId, Guid runId);

    /// <summary>Lists a project's runs.</summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="projectId">Project.</param>
    /// <returns>The runs.</returns>
    IReadOnlyList<AnalysisRun> ListRuns(string tenantId, Guid projectId);
}
