using ToolpathOptimizer.Core.Geometry;
using ToolpathOptimizer.Core.Validation;

namespace ToolpathOptimizer.Core.Optimisation;

/// <summary>
/// First-improvement 2-opt over an open path with a fixed start and optional fixed end.
/// A move is accepted only if it shortens the path by more than <see cref="MoveEpsilonMetres"/> and keeps
/// every precedence edge and locked block intact.
/// </summary>
public static class ConstrainedTwoOptImprover
{
    /// <summary>Minimum improvement in metres for a move to count as improving.</summary>
    public const double MoveEpsilonMetres = 1e-9;

    private const int CancellationCheckInterval = 4096;

    /// <summary>Improves an order within the budget.</summary>
    /// <param name="model">Validated constraint model.</param>
    /// <param name="initialOrder">Feasible starting order (baseline indices); not modified.</param>
    /// <param name="budget">Deterministic pass and evaluation budgets.</param>
    /// <param name="cancellationToken">Checked once per pass and every 4,096 evaluations.</param>
    /// <returns>The improved order, statistics and the running distance in metres.</returns>
    public static TwoOptOutcome Improve(ConstraintModel model, IReadOnlyList<int> initialOrder, SearchBudget budget, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(initialOrder);
        ArgumentNullException.ThrowIfNull(budget);
        var path = new OpenPath(model, initialOrder.ToArray());
        int pointCount = path.Order.Length;
        long passes = 0, evaluations = 0, acceptedMoves = 0;

        while (true)
        {
            if (passes == budget.MaxPasses)
            {
                return CreateOutcome(path, passes, evaluations, acceptedMoves, SearchTermination.BudgetExhausted);
            }

            cancellationToken.ThrowIfCancellationRequested();
            passes++;
            long acceptedThisPass = 0;
            for (int i = 0; i <= pointCount - 2; i++)
            {
                for (int j = i + 2; j <= pointCount; j++)
                {
                    if (evaluations == budget.MaxMoveEvaluations)
                    {
                        return CreateOutcome(path, passes, evaluations, acceptedMoves, SearchTermination.BudgetExhausted);
                    }

                    evaluations++;
                    if (evaluations % CancellationCheckInterval == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    double delta = path.ComputeReversalDelta(i, j);
                    if (delta < -MoveEpsilonMetres && path.IsReversalFeasible(i + 1, j))
                    {
                        path.ApplyReversal(i + 1, j, delta);
                        acceptedMoves++;
                        acceptedThisPass++;
                    }
                }
            }

            if (acceptedThisPass == 0)
            {
                return CreateOutcome(path, passes, evaluations, acceptedMoves, SearchTermination.Converged);
            }
        }
    }

    private static TwoOptOutcome CreateOutcome(OpenPath path, long passes, long evaluations, long acceptedMoves, SearchTermination termination) =>
        new(path.Order, new SearchStatistics(passes, evaluations, acceptedMoves, termination), path.RunningDistanceMetres);

    /// <summary>
    /// Mutable path state for one search. Route positions run 0..n+1: 0 is the start, 1..n the points in
    /// visit order, and n+1 the end when present.
    /// </summary>
    private sealed class OpenPath
    {
        private readonly ConstraintModel _model;
        private readonly int[] _routePositionOfPoint;
        private readonly bool _hasEnd;
        private readonly Point2 _end;

        /// <summary>Creates the path and computes its initial distance.</summary>
        /// <param name="model">Validated constraint model.</param>
        /// <param name="order">Visit order (baseline indices); owned and mutated by this instance.</param>
        public OpenPath(ConstraintModel model, int[] order)
        {
            _model = model;
            Order = order;
            _hasEnd = model.Job.End.HasValue;
            _end = model.Job.End.GetValueOrDefault();
            _routePositionOfPoint = new int[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                _routePositionOfPoint[order[i]] = i + 1;
            }

            RunningDistanceMetres = ComputeTotalDistance();
        }

        /// <summary>Gets the current visit order.</summary>
        public int[] Order { get; }

        /// <summary>Gets the distance maintained from accepted deltas.</summary>
        public double RunningDistanceMetres { get; private set; }

        /// <summary>Computes the distance change of reversing route positions <c>i+1..j</c>.</summary>
        /// <param name="i">Position before the reversed segment.</param>
        /// <param name="j">Last position of the reversed segment.</param>
        /// <returns>The delta in metres (negative means shorter).</returns>
        public double ComputeReversalDelta(int i, int j)
        {
            Point2 before = PositionAt(i), first = PositionAt(i + 1), last = PositionAt(j);
            double delta = EuclideanMetric.Distance(before, last) - EuclideanMetric.Distance(before, first);
            if (j < Order.Length || _hasEnd)
            {
                Point2 after = PositionAt(j + 1);
                delta += EuclideanMetric.Distance(first, after) - EuclideanMetric.Distance(last, after);
            }

            return delta;
        }

        /// <summary>Determines whether reversing a segment keeps locked blocks and precedence edges intact.</summary>
        /// <param name="first">First route position of the segment.</param>
        /// <param name="last">Last route position of the segment.</param>
        /// <returns><c>true</c> when the reversal is feasible.</returns>
        public bool IsReversalFeasible(int first, int last) =>
            !ContainsLockedMember(first, last) && !ContainsPrecedenceEdge(first, last);

        /// <summary>Reverses a segment and updates positions and the running distance.</summary>
        /// <param name="first">First route position of the segment.</param>
        /// <param name="last">Last route position of the segment.</param>
        /// <param name="delta">Precomputed distance delta.</param>
        public void ApplyReversal(int first, int last, double delta)
        {
            Array.Reverse(Order, first - 1, last - first + 1);
            for (int position = first; position <= last; position++)
            {
                _routePositionOfPoint[Order[position - 1]] = position;
            }

            RunningDistanceMetres += delta;
        }

        private bool ContainsLockedMember(int first, int last)
        {
            for (int position = first; position <= last; position++)
            {
                if (_model.IsLockedMember[Order[position - 1]])
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A reversal flips any edge with both endpoints inside the segment; edges crossing the boundary keep their order.</summary>
        private bool ContainsPrecedenceEdge(int first, int last)
        {
            for (int position = first; position <= last; position++)
            {
                foreach (int successor in _model.PointSuccessors[Order[position - 1]])
                {
                    int successorPosition = _routePositionOfPoint[successor];
                    if (successorPosition >= first && successorPosition <= last)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Point2 PositionAt(int routePosition)
        {
            if (routePosition == 0)
            {
                return _model.Job.Start;
            }

            return routePosition <= Order.Length ? _model.Positions[Order[routePosition - 1]] : _end;
        }

        private double ComputeTotalDistance()
        {
            double total = 0;
            int lastLegStart = _hasEnd ? Order.Length + 1 : Order.Length;
            for (int position = 0; position < lastLegStart; position++)
            {
                total += EuclideanMetric.Distance(PositionAt(position), PositionAt(position + 1));
            }

            return total;
        }
    }
}
