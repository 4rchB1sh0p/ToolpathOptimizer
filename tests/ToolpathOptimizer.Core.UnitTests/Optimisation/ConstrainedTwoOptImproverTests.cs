using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.UnitTests.Support;
using ToolpathOptimizer.Core.Validation;
using static ToolpathOptimizer.Core.UnitTests.Support.TestData;

namespace ToolpathOptimizer.Core.UnitTests.Optimisation;

/// <summary>Tests for <see cref="ConstrainedTwoOptImprover"/>.</summary>
public sealed class ConstrainedTwoOptImproverTests
{
    private static readonly SearchBudget _smallBudget = new(10, 1000);

    /// <summary>A 10 mm square above the start; the order A-D-C-B crosses itself.</summary>
    private static JobBuilder CrossingSquare() =>
        JobBuilder.WithPoints(PointAt("A", 0, 10), PointAt("B", 10, 10), PointAt("C", 0, 20), PointAt("D", 10, 20));

    private static (string[] Order, TwoOptOutcome Outcome) Improve(DrillJob job, SearchBudget budget, params string[] initialIds)
    {
        ConstraintModel model = JobValidator.Validate(job).Model;
        int[] initialOrder = initialIds.Select(id => job.TryGetIndex(new PointId(id), out int index) ? index : -1).ToArray();
        TwoOptOutcome outcome = ConstrainedTwoOptImprover.Improve(model, initialOrder, budget, CancellationToken.None);
        return (outcome.Order.Select(index => job.Points[index].Id.Value).ToArray(), outcome);
    }

    [Fact]
    public void Improve_CrossingPath_ReachesOptimalOrder()
    {
        // First improvement (i=1, j=3) reverses D,C giving the optimal 40 mm path S-A-C-D-B.
        (string[] order, _) = Improve(CrossingSquare().Build(), _smallBudget, "A", "D", "C", "B");

        Assert.Equal(["A", "C", "D", "B"], order);
    }

    [Fact]
    public void Improve_CrossingPath_TracksRunningDistance()
    {
        (_, TwoOptOutcome outcome) = Improve(CrossingSquare().Build(), _smallBudget, "A", "D", "C", "B");

        Assert.Equal(0.040, outcome.RunningDistanceMetres, 12);
    }

    [Fact]
    public void Improve_CrossingPath_Converges()
    {
        (_, TwoOptOutcome outcome) = Improve(CrossingSquare().Build(), _smallBudget, "A", "D", "C", "B");

        Assert.Equal(SearchTermination.Converged, outcome.Statistics.Termination);
    }

    [Fact]
    public void Improve_PrecedenceInsideSegment_KeepsEdgeOrder()
    {
        (string[] order, _) = Improve(CrossingSquare().WithPrecedence("D", "C").Build(), _smallBudget, "A", "D", "C", "B");

        Assert.True(Array.IndexOf(order, "D") < Array.IndexOf(order, "C"), $"D must stay before C; got {string.Join(",", order)}.");
    }

    [Fact]
    public void Improve_LockedBlock_KeepsMembersAdjacent()
    {
        DrillJob job = JobBuilder.WithPoints(PointAt("A", 0, 10), PointInGroup("B", 10, 10, "G"), PointInGroup("C", 0, 20, "G"), PointAt("D", 10, 20))
            .WithLockedGroup("G").Build();

        (string[] order, _) = Improve(job, _smallBudget, "A", "D", "B", "C");

        Assert.Equal(Array.IndexOf(order, "B") + 1, Array.IndexOf(order, "C"));
    }

    [Fact]
    public void Improve_ZeroPasses_ReturnsInitialOrder()
    {
        (string[] order, _) = Improve(CrossingSquare().Build(), new SearchBudget(0, 1000), "A", "D", "C", "B");

        Assert.Equal(["A", "D", "C", "B"], order);
    }

    [Fact]
    public void Improve_ZeroPasses_ReportsBudgetExhaustedWithoutEvaluations()
    {
        (_, TwoOptOutcome outcome) = Improve(CrossingSquare().Build(), new SearchBudget(0, 1000), "A", "D", "C", "B");

        Assert.Equal((0L, SearchTermination.BudgetExhausted), (outcome.Statistics.MoveEvaluations, outcome.Statistics.Termination));
    }

    [Fact]
    public void Improve_EvaluationBudget_StopsExactlyAtBudget()
    {
        DrillJob job = CreateRandomJob(seed: 3, pointCount: 30, precedenceEdgeCount: 0, RandomJobFeatures.None);

        (_, TwoOptOutcome outcome) = Improve(job, new SearchBudget(100, 7), IdsOf(job.CreateBaselineRoute()));

        Assert.Equal((7L, SearchTermination.BudgetExhausted), (outcome.Statistics.MoveEvaluations, outcome.Statistics.Termination));
    }

    [Fact]
    public void Improve_CancelledToken_Throws()
    {
        DrillJob job = CreateRandomJob(seed: 5, pointCount: 20, precedenceEdgeCount: 0, RandomJobFeatures.None);
        ConstraintModel model = JobValidator.Validate(job).Model;
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            ConstrainedTwoOptImprover.Improve(model, Enumerable.Range(0, 20).ToArray(), _smallBudget, cancelled.Token));
    }
}
