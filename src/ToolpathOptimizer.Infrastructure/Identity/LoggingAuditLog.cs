using Microsoft.Extensions.Logging;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Infrastructure.Identity;

/// <summary>
/// Writes audit events to the structured log (event IDs 3000–3999) until a database sink exists.
/// </summary>
public sealed partial class LoggingAuditLog : IAuditLog
{
    private readonly ILogger<LoggingAuditLog> _logger;

    /// <summary>Creates the audit log.</summary>
    /// <param name="logger">Logger.</param>
    public LoggingAuditLog(ILogger<LoggingAuditLog> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void Record(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        LogAudit(_logger, auditEvent.Action, auditEvent.TenantId, auditEvent.ActorId, auditEvent.ResourceId, auditEvent.OccurredAtUtc);
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Audit: action={Action} tenant={TenantId} actor={ActorId} resource={ResourceId} at={OccurredAtUtc}")]
    private static partial void LogAudit(ILogger logger, string action, string tenantId, string actorId, string resourceId, DateTimeOffset occurredAtUtc);
}
