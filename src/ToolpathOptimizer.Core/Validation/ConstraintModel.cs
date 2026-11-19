using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;

namespace ToolpathOptimizer.Core.Validation;

/// <summary>
/// A validated, index-based view of a <see cref="DrillJob"/> used by the optimiser:
/// contracted nodes, lifted precedence, point-level adjacency and ordinal tie-break ranks.
/// </summary>
public sealed class ConstraintModel
{
    internal ConstraintModel(
        DrillJob job,
        RouteNode[] nodes,
        int[][] nodeSuccessors,
        int[] nodePredecessorCounts,
        int[][] pointSuccessors,
        bool[] isLockedMember)
    {
        Job = job;
        NodeArray = nodes;
        NodeSuccessors = nodeSuccessors;
        NodePredecessorCounts = nodePredecessorCounts;
        PointSuccessors = pointSuccessors;
        IsLockedMember = isLockedMember;
        Positions = job.Points.Select(p => p.Position).ToArray();
        OrdinalRank = ComputeOrdinalRanks(job);
    }

    /// <summary>Gets the validated job.</summary>
    public DrillJob Job { get; }

    /// <summary>Gets the contracted nodes (singletons and locked blocks).</summary>
    public IReadOnlyList<RouteNode> Nodes => NodeArray;

    /// <summary>Gets the number of drill points.</summary>
    public int PointCount => Positions.Length;

    internal RouteNode[] NodeArray { get; }

    internal int[][] NodeSuccessors { get; }

    internal int[] NodePredecessorCounts { get; }

    internal int[][] PointSuccessors { get; }

    internal bool[] IsLockedMember { get; }

    internal Point2[] Positions { get; }

    internal int[] OrdinalRank { get; }

    private static int[] ComputeOrdinalRanks(DrillJob job)
    {
        int[] byOrdinal = Enumerable.Range(0, job.Points.Count).ToArray();
        Array.Sort(byOrdinal, (a, b) => job.Points[a].Id.CompareTo(job.Points[b].Id));
        int[] rank = new int[byOrdinal.Length];
        for (int r = 0; r < byOrdinal.Length; r++)
        {
            rank[byOrdinal[r]] = r;
        }

        return rank;
    }
}
