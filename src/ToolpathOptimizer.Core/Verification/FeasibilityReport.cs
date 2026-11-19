using ToolpathOptimizer.Core.Units;

namespace ToolpathOptimizer.Core.Verification;

/// <summary>
/// A single constraint violation found in a route.
/// </summary>
public sealed class FeasibilityViolation
{
    /// <summary>Creates a violation.</summary>
    /// <param name="code">Stable code from <see cref="FeasibilityCodes"/>.</param>
    /// <param name="message">Explanation naming the points or group.</param>
    public FeasibilityViolation(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>Gets the stable code.</summary>
    public string Code { get; }

    /// <summary>Gets the explanation.</summary>
    public string Message { get; }
}

/// <summary>
/// Independent verdict on a route: feasibility, violations (capped) and authoritative distance.
/// </summary>
public sealed class FeasibilityReport
{
    /// <summary>Creates a report.</summary>
    /// <param name="violations">Reported violations (at most <see cref="FeasibilityValidator.MaxReportedViolations"/>).</param>
    /// <param name="violationCount">Total number of violations found.</param>
    /// <param name="distance">Authoritative distance; zero when the route is not a permutation.</param>
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

    /// <summary>Gets the authoritative route distance.</summary>
    public Length Distance { get; }
}
