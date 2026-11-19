using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Core.Validation;

/// <summary>
/// Validates constraint references and the constraint graph, then contracts locked groups into blocks.
/// Issues are reported in deterministic order (input order; cycle members ordinal-sorted).
/// </summary>
public static class JobValidator
{
    private const int MaxListedCycleMembers = 10;

    /// <summary>Validates a job and prepares its constraint model.</summary>
    /// <param name="job">Job to validate.</param>
    /// <returns>Issues, or a prepared <see cref="ConstraintModel"/>.</returns>
    public static ValidationResult Validate(DrillJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        var issues = new List<ValidationIssue>();

        Dictionary<string, List<int>> groupMembers = CollectGroupMembers(job);
        List<string> lockedGroups = DistinctOrdinal(job.Constraints.LockedGroups);
        foreach (string group in lockedGroups)
        {
            if (!groupMembers.ContainsKey(group))
            {
                issues.Add(new ValidationIssue(ValidationCodes.ConstraintUnknownGroup, $"Locked group '{group}' has no points."));
            }
        }

        List<(int Before, int After)> edges = ResolveEdges(job, issues);
        if (issues.Count > 0)
        {
            return ValidationResult.Invalid(issues);
        }

        int[][] pointSuccessors = BuildAdjacency(job.Points.Count, edges);
        if (!GraphAlgorithms.IsAcyclic(job.Points.Count, pointSuccessors))
        {
            issues.Add(DescribeCycle(job, pointSuccessors, ValidationCodes.ConstraintCycle, n => [n]));
            return ValidationResult.Invalid(issues);
        }

        return BuildModel(job, lockedGroups, groupMembers, edges, pointSuccessors, issues);
    }

    private static ValidationResult BuildModel(
        DrillJob job,
        List<string> lockedGroups,
        Dictionary<string, List<int>> groupMembers,
        List<(int Before, int After)> edges,
        int[][] pointSuccessors,
        List<ValidationIssue> issues)
    {
        int pointCount = job.Points.Count;
        int[] nodeOf = new int[pointCount];
        Array.Fill(nodeOf, -1);
        int[] positionInBlock = new int[pointCount];
        bool[] isLockedMember = new bool[pointCount];
        var nodes = new List<RouteNode>();

        foreach (string group in lockedGroups)
        {
            int[] members = groupMembers[group].ToArray();
            for (int k = 0; k < members.Length; k++)
            {
                nodeOf[members[k]] = nodes.Count;
                positionInBlock[members[k]] = k;
                isLockedMember[members[k]] = members.Length >= 2;
            }

            nodes.Add(new RouteNode(members));
        }

        for (int i = 0; i < pointCount; i++)
        {
            if (nodeOf[i] < 0)
            {
                nodeOf[i] = nodes.Count;
                nodes.Add(new RouteNode([i]));
            }
        }

        var nodeEdges = new List<(int Before, int After)>();
        foreach ((int before, int after) in edges)
        {
            if (nodeOf[before] != nodeOf[after])
            {
                nodeEdges.Add((nodeOf[before], nodeOf[after]));
                continue;
            }

            if (positionInBlock[before] > positionInBlock[after])
            {
                issues.Add(new ValidationIssue(
                    ValidationCodes.ConstraintLockedGroupConflict,
                    $"Precedence '{job.Points[before].Id}' -> '{job.Points[after].Id}' contradicts the baseline order of locked group '{job.Points[before].OperationGroup}'."));
            }
        }

        if (issues.Count > 0)
        {
            return ValidationResult.Invalid(issues);
        }

        RouteNode[] nodeArray = nodes.ToArray();
        int[][] nodeSuccessors = BuildAdjacency(nodeArray.Length, Distinct(nodeEdges));
        if (!GraphAlgorithms.IsAcyclic(nodeArray.Length, nodeSuccessors))
        {
            issues.Add(DescribeCycle(job, nodeSuccessors, ValidationCodes.ConstraintLockedGroupConflict, n => nodeArray[n].MemberArray));
            return ValidationResult.Invalid(issues);
        }

        int[] predecessorCounts = new int[nodeArray.Length];
        foreach (int[] targets in nodeSuccessors)
        {
            foreach (int target in targets)
            {
                predecessorCounts[target]++;
            }
        }

        return ValidationResult.Valid(new ConstraintModel(job, nodeArray, nodeSuccessors, predecessorCounts, pointSuccessors, isLockedMember));
    }

    private static Dictionary<string, List<int>> CollectGroupMembers(DrillJob job)
    {
        var groups = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (int i = 0; i < job.Points.Count; i++)
        {
            DrillPoint point = job.Points[i];
            if (!point.HasOperationGroup)
            {
                continue;
            }

            if (!groups.TryGetValue(point.OperationGroup, out List<int>? members))
            {
                members = [];
                groups.Add(point.OperationGroup, members);
            }

            members.Add(i);
        }

        return groups;
    }

    private static List<(int Before, int After)> ResolveEdges(DrillJob job, List<ValidationIssue> issues)
    {
        var edges = new List<(int Before, int After)>();
        foreach (PrecedenceEdge edge in job.Constraints.Precedence)
        {
            bool hasBefore = job.TryGetIndex(edge.Before, out int before);
            bool hasAfter = job.TryGetIndex(edge.After, out int after);
            if (!hasBefore)
            {
                issues.Add(new ValidationIssue(ValidationCodes.ConstraintUnknownPoint, $"Precedence references unknown point '{edge.Before}'."));
            }

            if (!hasAfter)
            {
                issues.Add(new ValidationIssue(ValidationCodes.ConstraintUnknownPoint, $"Precedence references unknown point '{edge.After}'."));
            }

            if (hasBefore && hasAfter)
            {
                edges.Add((before, after));
            }
        }

        return Distinct(edges);
    }

    private static List<(int Before, int After)> Distinct(List<(int Before, int After)> edges)
    {
        var seen = new HashSet<(int, int)>();
        var result = new List<(int Before, int After)>(edges.Count);
        foreach ((int Before, int After) edge in edges)
        {
            if (seen.Add(edge))
            {
                result.Add(edge);
            }
        }

        return result;
    }

    private static int[][] BuildAdjacency(int nodeCount, List<(int Before, int After)> edges)
    {
        var lists = new List<int>[nodeCount];
        for (int v = 0; v < nodeCount; v++)
        {
            lists[v] = [];
        }

        foreach ((int before, int after) in edges)
        {
            lists[before].Add(after);
        }

        return lists.Select(l => l.ToArray()).ToArray();
    }

    private static ValidationIssue DescribeCycle(DrillJob job, int[][] successors, string code, Func<int, int[]> pointsOfNode)
    {
        List<int[]> components = GraphAlgorithms.FindCyclicComponents(successors.Length, successors);
        List<string>[] described = components
            .Select(c => c.SelectMany(pointsOfNode).Select(p => job.Points[p].Id.Value).Order(StringComparer.Ordinal).ToList())
            .OrderBy(ids => ids[0], StringComparer.Ordinal)
            .ToArray();
        List<string> members = described[0];
        string listed = string.Join(", ", members.Take(MaxListedCycleMembers));
        if (members.Count > MaxListedCycleMembers)
        {
            listed += $", ... ({members.Count} points)";
        }

        string what = code == ValidationCodes.ConstraintCycle
            ? "Precedence constraints form a cycle among"
            : "Precedence constraints conflict with locked groups among";
        return new ValidationIssue(code, $"{what}: {listed}.");
    }

    private static List<string> DistinctOrdinal(IReadOnlyList<string> values)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return values.Where(seen.Add).ToList();
    }
}
