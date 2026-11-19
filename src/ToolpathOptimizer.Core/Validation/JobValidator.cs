using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Core.Validation;

/// <summary>
/// Validates constraint references and the constraint graph, then contracts locked groups into blocks.
/// Issues are reported in deterministic order (input order; cycle members ordinal-sorted).
/// </summary>
public static class JobValidator
{
    private const int MaxListedCycleMembers = 10;
    private const string PointCycleLead = "Precedence constraints form a cycle among";
    private const string BlockCycleLead = "Precedence constraints conflict with locked groups among";

    /// <summary>Validates a job and prepares its constraint model.</summary>
    /// <param name="job">Job to validate.</param>
    /// <returns>Issues, or a prepared <see cref="ConstraintModel"/>.</returns>
    public static ValidationResult Validate(DrillJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        Dictionary<string, List<int>> groupMembers = CollectGroupMembers(job);
        List<string> lockedGroups = DistinctOrdinal(job.Constraints.LockedGroups);

        List<ValidationIssue> referenceIssues = [.. FindUnknownGroups(lockedGroups, groupMembers), .. FindUnknownPoints(job)];
        if (referenceIssues.Count > 0)
        {
            return ValidationResult.Invalid(referenceIssues);
        }

        List<(int Before, int After)> edges = ResolveEdges(job);
        int[][] pointSuccessors = BuildAdjacency(job.Points.Count, edges);
        if (!GraphAlgorithms.IsAcyclic(job.Points.Count, pointSuccessors))
        {
            return ValidationResult.Invalid([DescribeCycle(job, pointSuccessors, node => [node], ValidationCodes.ConstraintCycle, PointCycleLead)]);
        }

        BlockAssignment blocks = BlockAssignment.Contract(job, lockedGroups, groupMembers);
        List<ValidationIssue> orderConflicts = FindBlockOrderConflicts(job, blocks, edges);
        if (orderConflicts.Count > 0)
        {
            return ValidationResult.Invalid(orderConflicts);
        }

        int[][] nodeSuccessors = BuildAdjacency(blocks.Nodes.Length, LiftEdgesToNodes(blocks, edges));
        if (!GraphAlgorithms.IsAcyclic(blocks.Nodes.Length, nodeSuccessors))
        {
            return ValidationResult.Invalid([DescribeCycle(job, nodeSuccessors, node => blocks.Nodes[node].MemberArray, ValidationCodes.ConstraintLockedGroupConflict, BlockCycleLead)]);
        }

        return ValidationResult.Valid(new ConstraintModel(job, blocks.Nodes, nodeSuccessors, CountPredecessors(nodeSuccessors), pointSuccessors, blocks.IsLockedMember));
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

    private static IEnumerable<ValidationIssue> FindUnknownGroups(List<string> lockedGroups, Dictionary<string, List<int>> groupMembers) =>
        lockedGroups
            .Where(group => !groupMembers.ContainsKey(group))
            .Select(group => new ValidationIssue(ValidationCodes.ConstraintUnknownGroup, $"Locked group '{group}' has no points."));

    private static IEnumerable<ValidationIssue> FindUnknownPoints(DrillJob job) =>
        job.Constraints.Precedence
            .SelectMany(edge => new[] { edge.Before, edge.After })
            .Where(id => !job.TryGetIndex(id, out _))
            .Select(id => new ValidationIssue(ValidationCodes.ConstraintUnknownPoint, $"Precedence references unknown point '{id}'."));

    /// <summary>Maps precedence edges to baseline indices (all references are known), removing duplicates.</summary>
    private static List<(int Before, int After)> ResolveEdges(DrillJob job)
    {
        var resolved = new List<(int Before, int After)>();
        foreach (PrecedenceEdge edge in job.Constraints.Precedence)
        {
            job.TryGetIndex(edge.Before, out int before);
            job.TryGetIndex(edge.After, out int after);
            resolved.Add((before, after));
        }

        return DistinctEdges(resolved);
    }

    private static List<ValidationIssue> FindBlockOrderConflicts(DrillJob job, BlockAssignment blocks, List<(int Before, int After)> edges) =>
        edges
            .Where(edge => blocks.AreInSameNode(edge.Before, edge.After) && blocks.PositionInBlock[edge.Before] > blocks.PositionInBlock[edge.After])
            .Select(edge => new ValidationIssue(
                ValidationCodes.ConstraintLockedGroupConflict,
                $"Precedence '{job.Points[edge.Before].Id}' -> '{job.Points[edge.After].Id}' contradicts the baseline order of locked group '{job.Points[edge.Before].OperationGroup}'."))
            .ToList();

    /// <summary>Lifts point edges to node edges; edges inside one block are already satisfied by block order and dropped.</summary>
    private static List<(int Before, int After)> LiftEdgesToNodes(BlockAssignment blocks, List<(int Before, int After)> edges) =>
        DistinctEdges(edges
            .Where(edge => !blocks.AreInSameNode(edge.Before, edge.After))
            .Select(edge => (blocks.NodeOfPoint[edge.Before], blocks.NodeOfPoint[edge.After]))
            .ToList());

    private static List<(int Before, int After)> DistinctEdges(List<(int Before, int After)> edges)
    {
        var seen = new HashSet<(int, int)>();
        return edges.Where(edge => seen.Add(edge)).ToList();
    }

    private static int[][] BuildAdjacency(int nodeCount, List<(int Before, int After)> edges)
    {
        var successorLists = new List<int>[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            successorLists[i] = [];
        }

        foreach ((int before, int after) in edges)
        {
            successorLists[before].Add(after);
        }

        return successorLists.Select(list => list.ToArray()).ToArray();
    }

    private static int[] CountPredecessors(int[][] successors)
    {
        int[] counts = new int[successors.Length];
        foreach (int[] targets in successors)
        {
            foreach (int target in targets)
            {
                counts[target]++;
            }
        }

        return counts;
    }

    private static ValidationIssue DescribeCycle(DrillJob job, int[][] successors, Func<int, int[]> pointsOfNode, string code, string lead)
    {
        List<string> members = GraphAlgorithms.FindCyclicComponents(successors.Length, successors)
            .Select(component => component.SelectMany(pointsOfNode).Select(point => job.Points[point].Id.Value).Order(StringComparer.Ordinal).ToList())
            .OrderBy(ids => ids[0], StringComparer.Ordinal)
            .First();
        string listed = string.Join(", ", members.Take(MaxListedCycleMembers));
        if (members.Count > MaxListedCycleMembers)
        {
            listed += $", ... ({members.Count} points)";
        }

        return new ValidationIssue(code, $"{lead}: {listed}.");
    }

    private static List<string> DistinctOrdinal(IReadOnlyList<string> values)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return values.Where(seen.Add).ToList();
    }
}
