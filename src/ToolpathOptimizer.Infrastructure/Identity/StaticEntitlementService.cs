using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Infrastructure.Identity;

/// <summary>
/// Development entitlement service: one static plan for all tenants until a billing adapter exists (plan 0004).
/// </summary>
public sealed class StaticEntitlementService : IEntitlementService
{
    private readonly IOptions<EntitlementOptions> _options;

    /// <summary>Creates the service.</summary>
    /// <param name="options">Plan options.</param>
    public StaticEntitlementService(IOptions<EntitlementOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public OperationOutcome CheckAnalysisAllowed(string tenantId, int pointCount) =>
        pointCount <= _options.Value.MaxPointsPerJob
            ? OperationOutcome.Success
            : OperationOutcome.Failure(ErrorCodes.QuotaExceeded, $"The plan allows at most {_options.Value.MaxPointsPerJob} points per job.");
}
