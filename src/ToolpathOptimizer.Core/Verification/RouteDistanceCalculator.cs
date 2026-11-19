using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;
using ToolpathOptimizer.Core.Units;

namespace ToolpathOptimizer.Core.Verification;

/// <summary>
/// Recomputes route distance by straightforward in-order summation: start, each point, then the end if present.
/// </summary>
public static class RouteDistanceCalculator
{
    /// <summary>Calculates the open-path distance of a route.</summary>
    /// <param name="job">Job providing positions, start and end.</param>
    /// <param name="orderedIds">Visit order; every ID must exist in the job.</param>
    /// <returns>The total distance.</returns>
    public static Length Calculate(DrillJob job, IReadOnlyList<PointId> orderedIds)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(orderedIds);
        double total = 0;
        Point2 current = job.Start;
        foreach (PointId id in orderedIds)
        {
            if (!job.TryGetIndex(id, out int index))
            {
                throw new ArgumentException("Route contains an unknown point.", nameof(orderedIds));
            }

            Point2 next = job.Points[index].Position;
            total += EuclideanMetric.Distance(current, next);
            current = next;
        }

        if (job.End is Point2 end)
        {
            total += EuclideanMetric.Distance(current, end);
        }

        return Length.FromMetres(total);
    }
}
