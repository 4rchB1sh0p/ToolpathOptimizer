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

    /// <summary>Improves an order in place within the budget.</summary>
    /// <param name="model">Validated constraint model.</param>
    /// <param name="initialOrder">Feasible starting order (baseline indices).</param>
    /// <param name="budget">Deterministic budgets.</param>
    /// <param name="cancellationToken">Checked once per pass and every 4,096 evaluations.</param>
    /// <returns>The improved order, statistics and the running distance total in metres.</returns>
    public static TwoOptOutcome Improve(ConstraintModel model, IReadOnlyList<int> initialOrder, SearchBudget budget, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(initialOrder);
        ArgumentNullException.ThrowIfNull(budget);
        var state = new PathState(model, initialOrder.ToArray());
        int n = state.Order.Length;
        long passes = 0, evaluations = 0, accepted = 0;

        while (true)
        {
            if (passes == budget.MaxPasses)
            {
                return Finish(state, passes, evaluations, accepted, SearchTermination.BudgetExhausted);
            }

            cancellationToken.ThrowIfCancellationRequested();
            passes++;
            long acceptedThisPass = 0;
            for (int i = 0; i <= n - 2; i++)
            {
                for (int j = i + 2; j <= n; j++)
                {
                    if (evaluations == budget.MaxMoveEvaluations)
                    {
                        return Finish(state, passes, evaluations, accepted, SearchTermination.BudgetExhausted);
                    }

                    evaluations++;
                    if (evaluations % CancellationCheckInterval == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    double delta = state.Delta(i, j);
                    if (delta < -MoveEpsilonMetres && state.IsFeasibleReversal(i + 1, j))
                    {
                        state.Reverse(i + 1, j, delta);
                        accepted++;
                        acceptedThisPass++;
                    }
                }
            }

            if (acceptedThisPass == 0)
            {
                return Finish(state, passes, evaluations, accepted, SearchTermination.Converged);
            }
        }
    }

    private static TwoOptOutcome Finish(PathState state, long passes, long evaluations, long accepted, SearchTermination termination) =>
        new(state.Order, new SearchStatistics(passes, evaluations, accepted, termination), state.RunningDistance);

    /// <summary>Route positions 0..n+1: 0 is the start, 1..n the points, n+1 the end when present.</summary>
    private sealed class PathState
    {
        private readonly ConstraintModel _model;
        private readonly int[] _routePositionOf;
        private readonly bool _hasEnd;
        private readonly Point2 _end;

        public PathState(ConstraintModel model, int[] order)
        {
            _model = model;
            Order = order;
            _hasEnd = model.Job.End.HasValue;
            _end = model.Job.End.GetValueOrDefault();
            _routePositionOf = new int[order.Length];
            for (int k = 0; k < order.Length; k++)
            {
                _routePositionOf[order[k]] = k + 1;
            }

            RunningDistance = InitialDistance();
        }

        public int[] Order { get; }

        public double RunningDistance { get; private set; }

        public double Delta(int i, int j)
        {
            Point2 a = At(i), b = At(i + 1), c = At(j);
            double delta = EuclideanMetric.Distance(a, c) - EuclideanMetric.Distance(a, b);
            if (j < Order.Length || _hasEnd)
            {
                Point2 d = At(j + 1);
                delta += EuclideanMetric.Distance(b, d) - EuclideanMetric.Distance(c, d);
            }

            return delta;
        }

        public bool IsFeasibleReversal(int first, int last)
        {
            for (int k = first; k <= last; k++)
            {
                if (_model.IsLockedMember[Order[k - 1]])
                {
                    return false;
                }
            }

            for (int k = first; k <= last; k++)
            {
                foreach (int successor in _model.PointSuccessors[Order[k - 1]])
                {
                    int position = _routePositionOf[successor];
                    if (position >= first && position <= last)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void Reverse(int first, int last, double delta)
        {
            Array.Reverse(Order, first - 1, last - first + 1);
            for (int k = first; k <= last; k++)
            {
                _routePositionOf[Order[k - 1]] = k;
            }

            RunningDistance += delta;
        }

        private Point2 At(int routePosition)
        {
            if (routePosition == 0)
            {
                return _model.Job.Start;
            }

            return routePosition <= Order.Length ? _model.Positions[Order[routePosition - 1]] : _end;
        }

        private double InitialDistance()
        {
            double total = 0;
            int last = _hasEnd ? Order.Length + 1 : Order.Length;
            for (int k = 0; k < last; k++)
            {
                total += EuclideanMetric.Distance(At(k), At(k + 1));
            }

            return total;
        }
    }
}

/// <summary>
/// Result of <see cref="ConstrainedTwoOptImprover.Improve"/>.
/// </summary>
public sealed class TwoOptOutcome
{
    /// <summary>Creates an outcome.</summary>
    /// <param name="order">Improved order (baseline indices).</param>
    /// <param name="statistics">Search statistics.</param>
    /// <param name="runningDistanceMetres">Incrementally maintained distance in metres.</param>
    public TwoOptOutcome(IReadOnlyList<int> order, SearchStatistics statistics, double runningDistanceMetres)
    {
        Order = order.ToArray();
        Statistics = statistics;
        RunningDistanceMetres = runningDistanceMetres;
    }

    /// <summary>Gets the improved order.</summary>
    public IReadOnlyList<int> Order { get; }

    /// <summary>Gets the search statistics.</summary>
    public SearchStatistics Statistics { get; }

    /// <summary>Gets the incrementally maintained distance in metres (cross-checked by the validator).</summary>
    public double RunningDistanceMetres { get; }
}
