using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.UnitTests.Support;

/// <summary>Audit log that keeps events in memory for assertions.</summary>
internal sealed class RecordingAuditLog : IAuditLog
{
    /// <summary>Gets the recorded events in order.</summary>
    public List<AuditEvent> Events { get; } = [];

    /// <inheritdoc />
    public void Record(AuditEvent auditEvent) => Events.Add(auditEvent);
}
