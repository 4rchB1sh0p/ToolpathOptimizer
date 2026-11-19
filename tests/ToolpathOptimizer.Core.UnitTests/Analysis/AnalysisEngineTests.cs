using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Units;
using ToolpathOptimizer.Core.Validation;
using ToolpathOptimizer.Core.Verification;
using static ToolpathOptimizer.Core.UnitTests.TestJobs;

namespace ToolpathOptimizer.Core.UnitTests.Analysis;

public sealed class AnalysisEngineTests
{
    private static AnalysisResult Analyse(DrillJob job, AnalysisOptions? options = null)
    {
        AnalysisOutcome outcome = AnalysisEngine.Analyse(job, options ?? DefaultOptions, CancellationToken.None);
        Assert.True(outcome.IsSuccess, string.Join("; ", outcome.Issues.Select(i => i.Code)));
        return outcome.Result;
    }

    [Fact]
    public void Analyse_SelectsShorterCandidate()
    {
        DrillJob job = Job([P("A", 0, 10), P("D", 10, 20), P("C", 0, 20), P("B", 10, 10)]);

        AnalysisResult result = Analyse(job);

        Assert.Equal(RouteSelection.Candidate, result.Selection);
        Assert.Equal(AnalysisEngine.ReasonCandidateShorter, result.SelectionReason);
        Assert.True(result.Delta < Length.Zero);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Analyse_RetainsOptimalBaselineWithWarning()
    {
        DrillJob job = Job([P("H01", 0, 0), P("H02", 100, 0), P("H03", 100, 100), P("H04", 0, 100)]);

        AnalysisResult result = Analyse(job);

        Assert.Equal(RouteSelection.Baseline, result.Selection);
        Assert.Contains(result.Warnings, w => w.Code == AnalysisCodes.NoImprovementBaselineSelected);
        Assert.Equal(300, result.BaselineCheck.Distance.Millimetres, 9);
    }

    [Fact]
    public void Analyse_SelectsCandidateWhenBaselineInfeasible()
    {
        DrillJob job = Job([P("A", 0, 0), P("B", 10, 0)], precedence: [("B", "A")]);

        AnalysisResult result = Analyse(job);

        Assert.Equal(RouteSelection.Candidate, result.Selection);
        Assert.False(result.BaselineCheck.IsFeasible);
        Assert.Contains(result.Warnings, w => w.Code == AnalysisCodes.BaselineViolatesConstraints);
        Assert.Equal(["B", "A"], Ids(result.CandidateRoute));
    }

    [Fact]
    public void Analyse_ReportsZeroBaselineWithNullPercent()
    {
        AnalysisResult result = Analyse(Job([P("A", 0, 0)]));

        Assert.Null(result.DeltaPercent);
        Assert.Contains(result.Warnings, w => w.Code == AnalysisCodes.ZeroBaselineDistance);
    }

    [Fact]
    public void Analyse_WarnsWhenBudgetExhausted()
    {
        var options = new AnalysisOptions(new SearchBudget(0, 0), Length.Zero);

        AnalysisResult result = Analyse(Random(1, 10), options);

        Assert.Contains(result.Warnings, w => w.Code == AnalysisCodes.SearchBudgetExhausted);
    }

    [Fact]
    public void Analyse_ReturnsValidationIssuesForInfeasibleConstraints()
    {
        AnalysisOutcome outcome = AnalysisEngine.Analyse(Job([P("A", 0, 0), P("B", 1, 1)], precedence: [("A", "B"), ("B", "A")]), DefaultOptions, CancellationToken.None);

        Assert.False(outcome.IsSuccess);
        Assert.Equal(ValidationCodes.ConstraintCycle, outcome.Issues[0].Code);
    }

    [Fact]
    public void Analyse_IsDeterministicAcrossRuns()
    {
        DrillJob job = Random(42, 60, edgeCount: 15, withLockedGroup: true, withEnd: true);

        AnalysisResult first = Analyse(job);
        AnalysisResult second = Analyse(job);

        Assert.Equal(Ids(first.CandidateRoute), Ids(second.CandidateRoute));
        Assert.Equal(first.CandidateCheck.Distance, second.CandidateCheck.Distance);
    }

    /// <summary>Brute-force oracle: the candidate is feasible and never shorter than the true optimum; the selected route is never worse than a feasible baseline.</summary>
    [Theory]
    [MemberData(nameof(SmallSeeds))]
    public void Analyse_IsFeasibleAndBoundedByBruteForceOptimum(int seed)
    {
        var random = new Random(seed);
        int count = random.Next(3, 8);
        DrillJob job = Random(seed, count, edgeCount: random.Next(0, 4), withLockedGroup: count >= 4 && seed % 2 == 0, withEnd: seed % 3 == 0);

        AnalysisResult result = Analyse(job);
        double optimum = BruteForceOptimum(job);

        Assert.True(result.CandidateCheck.IsFeasible);
        Assert.True(result.CandidateCheck.Distance.Metres >= optimum - 1e-12);
        Assert.True(result.SelectedRoute.OrderedIds.Count == count);
        if (result.BaselineCheck.IsFeasible)
        {
            double selected = result.Selection == RouteSelection.Candidate ? result.CandidateCheck.Distance.Metres : result.BaselineCheck.Distance.Metres;
            Assert.True(selected <= result.BaselineCheck.Distance.Metres);
        }
    }

    public static TheoryData<int> SmallSeeds()
    {
        var data = new TheoryData<int>();
        for (int seed = 1; seed <= 40; seed++)
        {
            data.Add(seed);
        }

        return data;
    }

    [Fact]
    public void Analyse_ScalingByTwo_DoublesDistanceExactly()
    {
        DrillJob job = Random(11, 40, edgeCount: 8, withLockedGroup: true);
        DrillJob scaled = Transform(job, p => new Point2(p.X * 2, p.Y * 2));

        AnalysisResult original = Analyse(job);
        AnalysisResult doubled = Analyse(scaled);

        Assert.Equal(Ids(original.CandidateRoute), Ids(doubled.CandidateRoute));
        Assert.Equal(original.CandidateCheck.Distance.Metres * 2, doubled.CandidateCheck.Distance.Metres);
    }

    [Theory]
    [InlineData("mirror")]
    [InlineData("rotate90")]
    public void Analyse_DistancePreservingTransform_PreservesRouteAndDistance(string transform)
    {
        DrillJob job = Random(17, 35, edgeCount: 5, withEnd: true);
        Func<Point2, Point2> map = transform == "mirror" ? p => new Point2(-p.X, p.Y) : p => new Point2(-p.Y, p.X);

        AnalysisResult original = Analyse(job);
        AnalysisResult moved = Analyse(Transform(job, map));

        Assert.Equal(Ids(original.CandidateRoute), Ids(moved.CandidateRoute));
        Assert.Equal(original.CandidateCheck.Distance.Metres, moved.CandidateCheck.Distance.Metres);
    }

    private static double BruteForceOptimum(DrillJob job)
    {
        PointId[] ids = job.Points.Select(p => p.Id).ToArray();
        double best = double.PositiveInfinity;
        foreach (PointId[] permutation in Permutations(ids))
        {
            FeasibilityReport report = FeasibilityValidator.Validate(job, new Route(permutation));
            if (report.IsFeasible)
            {
                best = Math.Min(best, report.Distance.Metres);
            }
        }

        return best;
    }

    private static IEnumerable<PointId[]> Permutations(PointId[] items)
    {
        if (items.Length <= 1)
        {
            yield return items;
            yield break;
        }

        for (int i = 0; i < items.Length; i++)
        {
            PointId[] rest = items.Where((_, k) => k != i).ToArray();
            foreach (PointId[] tail in Permutations(rest))
            {
                yield return [items[i], .. tail];
            }
        }
    }
}
