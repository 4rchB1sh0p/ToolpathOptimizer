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
    /// <param name="cancellationToken">Propagated into the local search.</param>
    /// <returns>The candidate route with search statistics and running distance.</returns>
    public static OptimisationResult Optimise(ConstraintModel model, SearchBudget budget, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        int[] initialOrder = NearestNeighbourConstructor.Construct(model);
        TwoOptOutcome improved = ConstrainedTwoOptImprover.Improve(model, initialOrder, budget, cancellationToken);
        PointId[] ids = improved.Order.Select(index => model.Job.Points[index].Id).ToArray();
        return new OptimisationResult(new Route(ids), improved.Statistics, improved.RunningDistanceMetres);
    }
}
