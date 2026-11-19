using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Core.Validation;

/// <summary>
/// Result of contracting locked operation groups into blocks: which node each point belongs to and its
/// position inside that block. Ungrouped points become singleton nodes after all locked blocks.
/// </summary>
internal sealed class BlockAssignment
{
    private BlockAssignment(RouteNode[] nodes, int[] nodeOfPoint, int[] positionInBlock, bool[] isLockedMember)
    {
        Nodes = nodes;
        NodeOfPoint = nodeOfPoint;
        PositionInBlock = positionInBlock;
        IsLockedMember = isLockedMember;
    }

    /// <summary>Gets the contracted nodes in deterministic order (locked groups in input order, then singletons).</summary>
    public RouteNode[] Nodes { get; }

    /// <summary>Gets, per baseline point index, the index of the node that contains it.</summary>
    public int[] NodeOfPoint { get; }

    /// <summary>Gets, per baseline point index, its zero-based position within its node.</summary>
    public int[] PositionInBlock { get; }

    /// <summary>Gets, per baseline point index, whether it belongs to a locked block of two or more points.</summary>
    public bool[] IsLockedMember { get; }

    /// <summary>Contracts locked groups into blocks.</summary>
    /// <param name="job">Validated job.</param>
    /// <param name="lockedGroups">Distinct locked group names, all known.</param>
    /// <param name="groupMembers">Baseline member indices per operation group.</param>
    /// <returns>The block assignment.</returns>
    public static BlockAssignment Contract(DrillJob job, IReadOnlyList<string> lockedGroups, IReadOnlyDictionary<string, List<int>> groupMembers)
    {
        int pointCount = job.Points.Count;
        int[] nodeOfPoint = new int[pointCount];
        int[] positionInBlock = new int[pointCount];
        bool[] isLockedMember = new bool[pointCount];
        bool[] isAssigned = new bool[pointCount];
        var nodes = new List<RouteNode>();

        foreach (string group in lockedGroups)
        {
            int[] members = groupMembers[group].ToArray();
            for (int i = 0; i < members.Length; i++)
            {
                nodeOfPoint[members[i]] = nodes.Count;
                positionInBlock[members[i]] = i;
                isLockedMember[members[i]] = members.Length >= 2;
                isAssigned[members[i]] = true;
            }

            nodes.Add(new RouteNode(members));
        }

        for (int i = 0; i < pointCount; i++)
        {
            if (!isAssigned[i])
            {
                nodeOfPoint[i] = nodes.Count;
                nodes.Add(new RouteNode([i]));
            }
        }

        return new BlockAssignment(nodes.ToArray(), nodeOfPoint, positionInBlock, isLockedMember);
    }

    /// <summary>Determines whether both points lie in the same node.</summary>
    /// <param name="first">First baseline index.</param>
    /// <param name="second">Second baseline index.</param>
    /// <returns><c>true</c> when they share a node.</returns>
    public bool AreInSameNode(int first, int second) => NodeOfPoint[first] == NodeOfPoint[second];
}
