using ToolpathOptimizer.Core.Geometry;
using ToolpathOptimizer.Core.Validation;

namespace ToolpathOptimizer.Core.Optimisation;

/// <summary>
/// Builds a feasible initial order: repeatedly visits the eligible node whose entry is nearest to the
/// current position (ties broken by ordinal point ID). Locked blocks are visited whole.
/// </summary>
public static class NearestNeighbourConstructor
{
    /// <summary>Constructs a feasible visit order.</summary>
    /// <param name="model">Validated constraint model.</param>
    /// <returns>Baseline point indices in visit order.</returns>
    public static int[] Construct(ConstraintModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        RouteNode[] nodes = model.NodeArray;
        int[] remaining = (int[])model.NodePredecessorCounts.Clone();
        var eligible = new List<int>();
        for (int k = 0; k < nodes.Length; k++)
        {
            if (remaining[k] == 0)
            {
                eligible.Add(k);
            }
        }

        var order = new List<int>(model.PointCount);
        Point2 current = model.Job.Start;
        while (order.Count < model.PointCount)
        {
            if (eligible.Count == 0)
            {
                throw new InvalidOperationException("Nearest-neighbour construction reached a dead end on an acyclic graph (defect).");
            }

            int slot = SelectNearest(model, eligible, current);
            int chosen = eligible[slot];
            eligible[slot] = eligible[^1];
            eligible.RemoveAt(eligible.Count - 1);

            order.AddRange(nodes[chosen].MemberArray);
            current = model.Positions[nodes[chosen].Exit];
            foreach (int successor in model.NodeSuccessors[chosen])
            {
                if (--remaining[successor] == 0)
                {
                    eligible.Add(successor);
                }
            }
        }

        return order.ToArray();
    }

    private static int SelectNearest(ConstraintModel model, List<int> eligible, Point2 current)
    {
        int bestSlot = -1;
        double bestDistance = 0;
        int bestRank = 0;
        for (int slot = 0; slot < eligible.Count; slot++)
        {
            int entry = model.NodeArray[eligible[slot]].Entry;
            double distance = EuclideanMetric.SquaredDistance(current, model.Positions[entry]);
            int rank = model.OrdinalRank[entry];
            if (bestSlot < 0 || distance < bestDistance || (distance == bestDistance && rank < bestRank))
            {
                bestSlot = slot;
                bestDistance = distance;
                bestRank = rank;
            }
        }

        return bestSlot;
    }
}
