using ToolpathOptimizer.Core.Units;

namespace ToolpathOptimizer.Core.Verification;

/// <summary>
/// Independent verdict on a route: feasibility, violations (capped) and authoritative distance.
/// </summary>
public sealed class FeasibilityReport
{
    /// <summary>Creates a report.</summary>
    /// <param name="violations">Reported violations (at most <see cref="FeasibilityValidator.MaxReportedViolations"/>).</param>
    /// <param name="violationCount">Total number of violations found, including unreported ones.</param>
    /// <param name="distance">Authoritative distance; zero when the route is not a permutation of the job's points.</param>
    public FeasibilityReport(IReadOnlyList<FeasibilityViolation> violations, int violationCount, Length distance)
    {
        Violations = violations.ToArray();
        ViolationCount = violationCount;
        Distance = distance;
    }

    /// <summary>Gets a value indicating whether the route satisfies every check.</summary>
    public bool IsFeasible => ViolationCount == 0;

    /// <summary>Gets the reported violations in deterministic order.</summary>
    public IReadOnlyList<FeasibilityViolation> Violations { get; }

    /// <summary>Gets the total number of violations (may exceed the reported list).</summary>
    public int ViolationCount { get; }

    /// <summary>Gets the authoritative route distance recomputed by in-order summation.</summary>
    public Length Distance { get; }
}
