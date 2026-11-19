namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// The tenant of the current request, taken only from the validated credential (never from route, query or body).
/// </summary>
public interface ITenantContext
{
    /// <summary>Gets the tenant identifier; empty when unauthenticated.</summary>
    string TenantId { get; }
}
