namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// A security-relevant event: who did what to which resource, and when.
/// </summary>
public sealed class AuditEvent
{
    /// <summary>Creates an audit event.</summary>
    /// <param name="tenantId">Tenant in which the action happened.</param>
    /// <param name="actorId">User who performed the action.</param>
    /// <param name="action">Dotted action name, e.g. <c>project.deleted</c>.</param>
    /// <param name="resourceId">Identifier of the affected project, input or run.</param>
    /// <param name="occurredAtUtc">Time of the action from the injected clock.</param>
    public AuditEvent(string tenantId, string actorId, string action, string resourceId, DateTimeOffset occurredAtUtc)
    {
        TenantId = tenantId;
        ActorId = actorId;
        Action = action;
        ResourceId = resourceId;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>Gets the tenant in which the action happened.</summary>
    public string TenantId { get; }

    /// <summary>Gets the user who performed the action.</summary>
    public string ActorId { get; }

    /// <summary>Gets the dotted action name, e.g. <c>project.deleted</c>.</summary>
    public string Action { get; }

    /// <summary>Gets the identifier of the affected project, input or run.</summary>
    public string ResourceId { get; }

    /// <summary>Gets the time of the action.</summary>
    public DateTimeOffset OccurredAtUtc { get; }
}
