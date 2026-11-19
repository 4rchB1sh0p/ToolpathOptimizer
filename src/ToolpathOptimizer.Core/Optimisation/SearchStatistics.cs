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

/// <summary>
/// Counters describing a local-search run.
/// </summary>
public sealed class SearchStatistics
{
    /// <summary>Creates statistics.</summary>
    /// <param name="passes">Passes started.</param>
    /// <param name="moveEvaluations">Moves evaluated.</param>
    /// <param name="acceptedMoves">Moves accepted.</param>
    /// <param name="termination">Termination reason.</param>
    public SearchStatistics(long passes, long moveEvaluations, long acceptedMoves, SearchTermination termination)
    {
        Passes = passes;
        MoveEvaluations = moveEvaluations;
        AcceptedMoves = acceptedMoves;
        Termination = termination;
    }

    /// <summary>Gets the number of passes started.</summary>
    public long Passes { get; }

    /// <summary>Gets the number of move evaluations.</summary>
    public long MoveEvaluations { get; }

    /// <summary>Gets the number of accepted moves.</summary>
    public long AcceptedMoves { get; }

    /// <summary>Gets the termination reason.</summary>
    public SearchTermination Termination { get; }
}
