using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Records audit events for the current tenant and user, stamped by the injected clock.
/// </summary>
public sealed class ProjectAuditor
{
    private readonly IAuditLog _auditLog;
    private readonly ProjectAccess _access;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the auditor.</summary>
    /// <param name="auditLog">Audit sink.</param>
    /// <param name="access">Supplies the current tenant and user.</param>
    /// <param name="timeProvider">Clock for event timestamps.</param>
    public ProjectAuditor(IAuditLog auditLog, ProjectAccess access, TimeProvider timeProvider)
    {
        _auditLog = auditLog;
        _access = access;
        _timeProvider = timeProvider;
    }

    /// <summary>Records that the current user performed an action on a resource.</summary>
    /// <param name="action">Dotted action name, e.g. <c>project.deleted</c>.</param>
    /// <param name="resourceId">Affected project, input or run.</param>
    public void Record(string action, Guid resourceId) =>
        _auditLog.Record(new AuditEvent(_access.TenantId, _access.UserId, action, resourceId.ToString("N"), _timeProvider.GetUtcNow()));
}
