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
    /// <param name="job">The job.</param>
    /// <param name="route">The route to check.</param>
    /// <returns>The feasibility report.</returns>
    public static FeasibilityReport Validate(DrillJob job, Route route)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(route);
        var collector = new ViolationCollector();
        Dictionary<PointId, int>? positions = CheckPermutation(job, route, collector);
        if (positions is null)
        {
            return collector.ToReport(Length.Zero);
        }

        CheckPrecedence(job, positions, collector);
        CheckLockedGroups(job, positions, collector);
        return collector.ToReport(RouteDistanceCalculator.Calculate(job, route.OrderedIds));
    }

    private static Dictionary<PointId, int>? CheckPermutation(DrillJob job, Route route, ViolationCollector collector)
    {
        var positions = new Dictionary<PointId, int>(route.OrderedIds.Count);
        for (int k = 0; k < route.OrderedIds.Count; k++)
        {
            PointId id = route.OrderedIds[k];
            if (!job.TryGetIndex(id, out _))
            {
                collector.Add(FeasibilityCodes.RouteUnknownPoint, $"Route contains unknown point '{id}'.");
            }
            else if (!positions.TryAdd(id, k))
            {
                collector.Add(FeasibilityCodes.RouteDuplicatePoint, $"Point '{id}' is visited more than once.");
            }
        }

        foreach (DrillPoint point in job.Points)
        {
            if (!positions.ContainsKey(point.Id))
            {
                collector.Add(FeasibilityCodes.RouteMissingPoint, $"Point '{point.Id}' is not visited.");
            }
        }

        return collector.Count == 0 ? positions : null;
    }

    private static void CheckPrecedence(DrillJob job, Dictionary<PointId, int> positions, ViolationCollector collector)
    {
        foreach (PrecedenceEdge edge in job.Constraints.Precedence)
        {
            bool known = positions.TryGetValue(edge.Before, out int before) & positions.TryGetValue(edge.After, out int after);
            if (!known || before >= after)
            {
                collector.Add(FeasibilityCodes.PrecedenceViolated, $"Point '{edge.Before}' must be visited before '{edge.After}'.");
            }
        }
    }

    private static void CheckLockedGroups(DrillJob job, Dictionary<PointId, int> positions, ViolationCollector collector)
    {
        var checkedGroups = new HashSet<string>(StringComparer.Ordinal);
        foreach (string group in job.Constraints.LockedGroups)
        {
            if (!checkedGroups.Add(group))
            {
                continue;
            }

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

            for (int k = 1; k < routePositions.Length; k++)
            {
                if (routePositions[k] <= routePositions[k - 1])
                {
                    collector.Add(FeasibilityCodes.LockedGroupOrderChanged, $"Locked group '{group}' is not visited in baseline order.");
                    break;
                }
            }
        }
    }

    private sealed class ViolationCollector
    {
        private readonly List<FeasibilityViolation> _violations = [];

        public int Count { get; private set; }

        public void Add(string code, string message)
        {
            Count++;
            if (_violations.Count < MaxReportedViolations)
            {
                _violations.Add(new FeasibilityViolation(code, message));
            }
        }

        public FeasibilityReport ToReport(Length distance) => new(_violations, Count, distance);
    }
}
