using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.UnitTests.Support;

/// <summary>Entitlement service that allows every job.</summary>
internal sealed class AllowAllEntitlements : IEntitlementService
{
    /// <inheritdoc />
    public OperationOutcome CheckAnalysisAllowed(string tenantId, int pointCount) => OperationOutcome.Success;
}
