namespace ToolpathOptimizer.Core.Optimisation;

/// <summary>
/// Counters describing a local-search run, recorded in the report's <c>search</c> block.
/// </summary>
public sealed class SearchStatistics
{
    /// <summary>Creates statistics.</summary>
    /// <param name="passes">Number of 2-opt passes started.</param>
    /// <param name="moveEvaluations">Number of candidate moves whose distance delta was computed.</param>
    /// <param name="acceptedMoves">Number of improving, feasible moves applied.</param>
    /// <param name="termination">Why the search stopped.</param>
    public SearchStatistics(long passes, long moveEvaluations, long acceptedMoves, SearchTermination termination)
    {
        Passes = passes;
        MoveEvaluations = moveEvaluations;
        AcceptedMoves = acceptedMoves;
        Termination = termination;
    }

    /// <summary>Gets the number of 2-opt passes started.</summary>
    public long Passes { get; }

    /// <summary>Gets the number of candidate moves whose distance delta was computed.</summary>
    public long MoveEvaluations { get; }

    /// <summary>Gets the number of improving, feasible moves applied.</summary>
    public long AcceptedMoves { get; }

    /// <summary>Gets why the search stopped.</summary>
    public SearchTermination Termination { get; }
}
