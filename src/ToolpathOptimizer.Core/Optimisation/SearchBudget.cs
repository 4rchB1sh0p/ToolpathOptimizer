namespace ToolpathOptimizer.Core.Optimisation;

/// <summary>
/// Deterministic local-search budgets (ADR-0008). Zero passes means construction only.
/// </summary>
public sealed class SearchBudget
{
    /// <summary>Creates a budget.</summary>
    /// <param name="maxPasses">Maximum 2-opt passes (&gt;= 0).</param>
    /// <param name="maxMoveEvaluations">Maximum move evaluations (&gt;= 0).</param>
    public SearchBudget(long maxPasses, long maxMoveEvaluations)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxPasses);
        ArgumentOutOfRangeException.ThrowIfNegative(maxMoveEvaluations);
        MaxPasses = maxPasses;
        MaxMoveEvaluations = maxMoveEvaluations;
    }

    /// <summary>Gets the maximum number of passes.</summary>
    public long MaxPasses { get; }

    /// <summary>Gets the maximum number of move evaluations.</summary>
    public long MaxMoveEvaluations { get; }
}
