using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Units;

namespace ToolpathOptimizer.Core.Analysis;

/// <summary>
/// Effective parameters of an analysis run.
/// </summary>
public sealed class AnalysisOptions
{
    /// <summary>Creates options.</summary>
    /// <param name="budget">Deterministic search budgets.</param>
    /// <param name="minImprovement">Minimum improvement (&gt;= 0) required to select the candidate over a feasible baseline.</param>
    public AnalysisOptions(SearchBudget budget, Length minImprovement)
    {
        ArgumentNullException.ThrowIfNull(budget);
        if (minImprovement < Length.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minImprovement), "Minimum improvement must not be negative.");
        }

        Budget = budget;
        MinImprovement = minImprovement;
    }

    /// <summary>Gets the search budgets.</summary>
    public SearchBudget Budget { get; }

    /// <summary>Gets the minimum improvement used for selection.</summary>
    public Length MinImprovement { get; }
}
