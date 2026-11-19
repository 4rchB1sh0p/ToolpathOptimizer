using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Validation;
using static ToolpathOptimizer.Core.UnitTests.TestJobs;

namespace ToolpathOptimizer.Core.UnitTests.Optimisation;

public sealed class ConstrainedTwoOptImproverTests
{
    private static readonly DrillPoint[] s_square = [P("A", 0, 10), P("B", 10, 0), P("C", 10, 10), P("D", 0, 20)];

    private static (string[] Order, TwoOptOutcome Outcome) Improve(DrillJob job, SearchBudget budget, params string[] initial)
    {
        ConstraintModel model = JobValidator.Validate(job).Model;
        int[] order = initial.Select(id => { job.TryGetIndex(new PointId(id), out int index); return index; }).ToArray();
        TwoOptOutcome outcome = ConstrainedTwoOptImprover.Improve(model, order, budget, CancellationToken.None);
        return (outcome.Order.Select(i => job.Points[i].Id.Value).ToArray(), outcome);
    }

    [Fact]
    public void Improve_RemovesCrossing()
    {
        DrillJob job = Job([P("A", 0, 10), P("B", 10, 10), P("C", 0, 20), P("D", 10, 20)], startMm: (0, 0));

        (string[] order, TwoOptOutcome outcome) = Improve(job, new SearchBudget(10, 1000), "A", "D", "C", "B");

        // First improvement (i=1, j=3) reverses D,C giving the optimal 40 mm path S-A-C-D-B.
        Assert.Equal(["A", "C", "D", "B"], order);
        Assert.Equal(0.040, outcome.RunningDistanceMetres, 12);
        Assert.Equal(SearchTermination.Converged, outcome.Statistics.Termination);
        Assert.True(outcome.Statistics.AcceptedMoves > 0);
    }

    [Fact]
    public void Improve_DoesNotReverseAcrossPrecedenceEdgeInsideSegment()
    {
        DrillJob job = Job([P("A", 0, 10), P("B", 10, 10), P("C", 0, 20), P("D", 10, 20)], precedence: [("D", "C")], startMm: (0, 0));

        (string[] order, _) = Improve(job, new SearchBudget(10, 1000), "A", "D", "C", "B");

        Assert.True(Array.IndexOf(order, "D") < Array.IndexOf(order, "C"));
    }

    [Fact]
    public void Improve_NeverMovesLockedMembers()
    {
        DrillJob job = Job([P("A", 0, 10), P("B", 10, 10, "G"), P("C", 0, 20, "G"), P("D", 10, 20)], lockedGroups: ["G"], startMm: (0, 0));

        (string[] order, _) = Improve(job, new SearchBudget(10, 1000), "A", "D", "B", "C");

        Assert.Equal(Array.IndexOf(order, "B") + 1, Array.IndexOf(order, "C"));
    }

    [Fact]
    public void Improve_WithZeroPasses_ReturnsInitialOrderAsBudgetExhausted()
    {
        DrillJob job = Job(s_square);

        (string[] order, TwoOptOutcome outcome) = Improve(job, new SearchBudget(0, 1000), "A", "B", "C", "D");

        Assert.Equal(["A", "B", "C", "D"], order);
        Assert.Equal(SearchTermination.BudgetExhausted, outcome.Statistics.Termination);
        Assert.Equal(0, outcome.Statistics.MoveEvaluations);
    }

    [Fact]
    public void Improve_StopsExactlyAtMoveEvaluationBudget()
    {
        (_, TwoOptOutcome outcome) = Improve(Random(3, 30), new SearchBudget(100, 7), Ids(Random(3, 30).CreateBaselineRoute()));

        Assert.Equal(7, outcome.Statistics.MoveEvaluations);
        Assert.Equal(SearchTermination.BudgetExhausted, outcome.Statistics.Termination);
    }

    [Fact]
    public void Improve_ObservesCancellation()
    {
        DrillJob job = Random(5, 20);
        ConstraintModel model = JobValidator.Validate(job).Model;
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            ConstrainedTwoOptImprover.Improve(model, Enumerable.Range(0, 20).ToArray(), new SearchBudget(10, 1000), cancelled.Token));
    }
}
