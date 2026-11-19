using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// Plan quotas and credits from the billing provider.
/// </summary>
public interface IEntitlementService
{
    /// <summary>Checks whether the tenant's plan allows analysing a job of the given size.</summary>
    /// <param name="tenantId">Tenant whose plan applies.</param>
    /// <param name="pointCount">Number of drill points in the job.</param>
    /// <returns>Success, or <c>QUOTA_EXCEEDED</c>.</returns>
    OperationOutcome CheckAnalysisAllowed(string tenantId, int pointCount);
}
