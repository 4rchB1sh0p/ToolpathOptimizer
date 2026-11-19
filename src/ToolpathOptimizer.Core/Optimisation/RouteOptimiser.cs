using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Validation;

namespace ToolpathOptimizer.Core.Optimisation;

/// <summary>
/// Nearest-neighbour construction followed by constrained 2-opt (ADR-0007).
/// </summary>
public static class RouteOptimiser
{
    /// <summary>Stable algorithm name used in manifests and reports.</summary>
    public const string AlgorithmName = "nearest-neighbor-2opt";

    /// <summary>Algorithm version; bump whenever routes for the same input could change.</summary>
    public const string AlgorithmVersion = "1.0.0";

    /// <summary>Produces a candidate route.</summary>
    /// <param name="model">Validated constraint model.</param>
    /// <param name="budget">Deterministic search budgets.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The candidate route with search statistics and running distance.</returns>
    public static OptimisationResult Optimise(ConstraintModel model, SearchBudget budget, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        int[] initial = NearestNeighbourConstructor.Construct(model);
        TwoOptOutcome improved = ConstrainedTwoOptImprover.Improve(model, initial, budget, cancellationToken);
        PointId[] ids = improved.Order.Select(index => model.Job.Points[index].Id).ToArray();
        return new OptimisationResult(new Route(ids), improved.Statistics, improved.RunningDistanceMetres);
    }
}

/// <summary>
/// Candidate route produced by <see cref="RouteOptimiser"/>.
/// </summary>
public sealed class OptimisationResult
{
    /// <summary>Creates a result.</summary>
    /// <param name="route">Candidate route.</param>
    /// <param name="statistics">Search statistics.</param>
    /// <param name="runningDistanceMetres">Optimiser's running distance in metres.</param>
    public OptimisationResult(Route route, SearchStatistics statistics, double runningDistanceMetres)
    {
        Route = route;
        Statistics = statistics;
        RunningDistanceMetres = runningDistanceMetres;
    }

    /// <summary>Gets the candidate route.</summary>
    public Route Route { get; }

    /// <summary>Gets the search statistics.</summary>
    public SearchStatistics Statistics { get; }

    /// <summary>Gets the optimiser's running distance in metres (not authoritative).</summary>
    public double RunningDistanceMetres { get; }
}
