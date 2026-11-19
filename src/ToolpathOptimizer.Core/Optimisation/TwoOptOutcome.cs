namespace ToolpathOptimizer.Core.Optimisation;

/// <summary>
/// Result of <see cref="ConstrainedTwoOptImprover.Improve"/>.
/// </summary>
public sealed class TwoOptOutcome
{
    /// <summary>Creates an outcome.</summary>
    /// <param name="order">Improved visit order as baseline point indices.</param>
    /// <param name="statistics">Search counters and termination reason.</param>
    /// <param name="runningDistanceMetres">Distance maintained incrementally from accepted move deltas.</param>
    public TwoOptOutcome(IReadOnlyList<int> order, SearchStatistics statistics, double runningDistanceMetres)
    {
        Order = order.ToArray();
        Statistics = statistics;
        RunningDistanceMetres = runningDistanceMetres;
    }

    /// <summary>Gets the improved visit order as baseline point indices.</summary>
    public IReadOnlyList<int> Order { get; }

    /// <summary>Gets the search counters and termination reason.</summary>
    public SearchStatistics Statistics { get; }

    /// <summary>Gets the incrementally maintained distance in metres; the validator cross-checks it.</summary>
    public double RunningDistanceMetres { get; }
}
