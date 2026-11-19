using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Core.Optimisation;

/// <summary>
/// Candidate route produced by <see cref="RouteOptimiser"/>; not yet verified.
/// </summary>
public sealed class OptimisationResult
{
    /// <summary>Creates a result.</summary>
    /// <param name="route">Candidate visit order.</param>
    /// <param name="statistics">Search counters and termination reason.</param>
    /// <param name="runningDistanceMetres">Optimiser's incrementally maintained distance in metres.</param>
    public OptimisationResult(Route route, SearchStatistics statistics, double runningDistanceMetres)
    {
        Route = route;
        Statistics = statistics;
        RunningDistanceMetres = runningDistanceMetres;
    }

    /// <summary>Gets the candidate visit order.</summary>
    public Route Route { get; }

    /// <summary>Gets the search counters and termination reason.</summary>
    public SearchStatistics Statistics { get; }

    /// <summary>Gets the optimiser's running distance in metres (not authoritative; compared with the validator's).</summary>
    public double RunningDistanceMetres { get; }
}
