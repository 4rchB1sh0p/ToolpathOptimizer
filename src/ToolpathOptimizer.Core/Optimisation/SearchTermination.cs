namespace ToolpathOptimizer.Core.Optimisation;

/// <summary>
/// Why local search stopped.
/// </summary>
public enum SearchTermination
{
    /// <summary>A full pass found no improving feasible move.</summary>
    Converged,

    /// <summary>A pass or move-evaluation budget was reached first.</summary>
    BudgetExhausted,
}
