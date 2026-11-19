namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// Records security-relevant events.
/// </summary>
public interface IAuditLog
{
    /// <summary>Records an event.</summary>
    /// <param name="auditEvent">The event to record.</param>
    void Record(AuditEvent auditEvent);
}
