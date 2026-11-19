using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Units;

namespace ToolpathOptimizer.Core.Verification;

/// <summary>
/// Checks a route against the job directly, sharing no state with the optimiser (ADR-0007):
/// permutation, precedence, locked-group contiguity and order, then recomputes distance.
/// </summary>
public static class FeasibilityValidator
{
    /// <summary>Maximum number of violations listed in a report.</summary>
    public const int MaxReportedViolations = 100;

    /// <summary>Validates a route.</summary>
    /// <param name="job">The job whose constraints apply.</param>
    /// <param name="route">The route to check.</param>
    /// <returns>The feasibility report.</returns>
    public static FeasibilityReport Validate(DrillJob job, Route route)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(route);
        var collector = new ViolationCollector();
        CollectPermutationViolations(job, route, collector);
        if (collector.Count > 0)
        {
            return collector.CreateReport(Length.Zero);
        }

        Dictionary<PointId, int> positions = MapRoutePositions(route);
        CollectPrecedenceViolations(job, positions, collector);
        CollectLockedGroupViolations(job, positions, collector);
        return collector.CreateReport(RouteDistanceCalculator.Calculate(job, route.OrderedIds));
    }

    private static void CollectPermutationViolations(DrillJob job, Route route, ViolationCollector collector)
    {
        var visited = new HashSet<PointId>();
        foreach (PointId id in route.OrderedIds)
        {
            if (!job.TryGetIndex(id, out _))
            {
                collector.Add(FeasibilityCodes.RouteUnknownPoint, $"Route contains unknown point '{id}'.");
            }
            else if (!visited.Add(id))
            {
                collector.Add(FeasibilityCodes.RouteDuplicatePoint, $"Point '{id}' is visited more than once.");
            }
        }

        foreach (DrillPoint point in job.Points.Where(p => !visited.Contains(p.Id)))
        {
            collector.Add(FeasibilityCodes.RouteMissingPoint, $"Point '{point.Id}' is not visited.");
        }
    }

    private static Dictionary<PointId, int> MapRoutePositions(Route route)
    {
        var positions = new Dictionary<PointId, int>(route.OrderedIds.Count);
        for (int i = 0; i < route.OrderedIds.Count; i++)
        {
            positions.Add(route.OrderedIds[i], i);
        }

        return positions;
    }

    private static void CollectPrecedenceViolations(DrillJob job, Dictionary<PointId, int> positions, ViolationCollector collector)
    {
        foreach (PrecedenceEdge edge in job.Constraints.Precedence)
        {
            bool isBeforeVisited = positions.TryGetValue(edge.Before, out int before);
            bool isAfterVisited = positions.TryGetValue(edge.After, out int after);
            if (!isBeforeVisited || !isAfterVisited || before >= after)
            {
                collector.Add(FeasibilityCodes.PrecedenceViolated, $"Point '{edge.Before}' must be visited before '{edge.After}'.");
            }
        }
    }

    private static void CollectLockedGroupViolations(DrillJob job, Dictionary<PointId, int> positions, ViolationCollector collector)
    {
        foreach (string group in job.Constraints.LockedGroups.Distinct(StringComparer.Ordinal))
        {
            int[] routePositions = job.Points
                .Where(p => string.Equals(p.OperationGroup, group, StringComparison.Ordinal))
                .Select(p => positions[p.Id])
                .ToArray();
            if (routePositions.Length == 0)
            {
                continue;
            }

            if (routePositions.Max() - routePositions.Min() != routePositions.Length - 1)
            {
                collector.Add(FeasibilityCodes.LockedGroupNotContiguous, $"Locked group '{group}' is not visited contiguously.");
            }

            if (!IsStrictlyIncreasing(routePositions))
            {
                collector.Add(FeasibilityCodes.LockedGroupOrderChanged, $"Locked group '{group}' is not visited in baseline order.");
            }
        }
    }

    private static bool IsStrictlyIncreasing(int[] values)
    {
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] <= values[i - 1])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Counts every violation but keeps only the first <see cref="MaxReportedViolations"/> so reports stay bounded.
    /// </summary>
    private sealed class ViolationCollector
    {
        private readonly List<FeasibilityViolation> _violations = [];

        /// <summary>Gets the total number of violations added.</summary>
        public int Count { get; private set; }

        /// <summary>Records a violation.</summary>
        /// <param name="code">Stable violation code.</param>
        /// <param name="message">Explanation.</param>
        public void Add(string code, string message)
        {
            Count++;
            if (_violations.Count < MaxReportedViolations)
            {
                _violations.Add(new FeasibilityViolation(code, message));
            }
        }

        /// <summary>Creates the report.</summary>
        /// <param name="distance">Authoritative distance.</param>
        /// <returns>The report.</returns>
        public FeasibilityReport CreateReport(Length distance) => new(_violations, Count, distance);
    }
}
