using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;
using ToolpathOptimizer.Core.UnitTests.Support;
using ToolpathOptimizer.Core.Verification;
using static ToolpathOptimizer.Core.UnitTests.Support.TestData;

namespace ToolpathOptimizer.Core.UnitTests.Analysis;

/// <summary>
/// Property and metamorphic tests for <see cref="AnalysisEngine"/> on seeded random jobs, with a brute-force
/// optimum (n ≤ 7) as the oracle. Optimality is not asserted, only bounds.
/// </summary>
public sealed class AnalysisEnginePropertyTests
{
    /// <summary>Gets 40 seeds for small random jobs.</summary>
    public static TheoryData<int> SmallJobSeeds { get; } = new(Enumerable.Range(1, 40));

    private static DrillJob CreateSmallJob(int seed)
    {
        int pointCount = new Random(seed).Next(3, 8);
        RandomJobFeatures features = RandomJobFeatures.None;
        if (pointCount >= 4 && seed % 2 == 0)
        {
            features |= RandomJobFeatures.LockedGroup;
        }

        if (seed % 3 == 0)
        {
            features |= RandomJobFeatures.FixedEnd;
        }

        return CreateRandomJob(seed, pointCount, precedenceEdgeCount: seed % 4, features);
    }

    [Theory]
    [MemberData(nameof(SmallJobSeeds))]
    public void Analyse_RandomSmallJob_CandidateIsFeasible(int seed)
    {
        AnalysisResult result = AnalysisEngineTests.AnalyseSuccessfully(CreateSmallJob(seed), DefaultOptions);

        Assert.True(result.CandidateCheck.IsFeasible, $"Seed {seed}: " + string.Join("; ", result.CandidateCheck.Violations.Select(v => v.Code)));
    }

    [Theory]
    [MemberData(nameof(SmallJobSeeds))]
    public void Analyse_RandomSmallJob_CandidateIsNotShorterThanBruteForceOptimum(int seed)
    {
        DrillJob job = CreateSmallJob(seed);

        double candidate = AnalysisEngineTests.AnalyseSuccessfully(job, DefaultOptions).CandidateCheck.Distance.Metres;
        double optimum = FindBruteForceOptimum(job);

        Assert.True(candidate >= optimum - 1e-12, $"Seed {seed}: candidate {candidate} m is below the optimum {optimum} m.");
    }

    [Theory]
    [MemberData(nameof(SmallJobSeeds))]
    public void Analyse_RandomSmallJob_SelectedRouteIsNotWorseThanFeasibleBaseline(int seed)
    {
        AnalysisResult result = AnalysisEngineTests.AnalyseSuccessfully(CreateSmallJob(seed), DefaultOptions);
        double baseline = result.BaselineCheck.Distance.Metres;
        double selected = result.Selection == RouteSelection.Candidate ? result.CandidateCheck.Distance.Metres : baseline;

        Assert.True(selected <= baseline, $"Seed {seed}: selected {selected} m is longer than the feasible baseline {baseline} m.");
    }

    [Fact]
    public void Analyse_ScaledByTwo_KeepsRoute()
    {
        DrillJob job = CreateRandomJob(seed: 11, pointCount: 40, precedenceEdgeCount: 8, RandomJobFeatures.LockedGroup);

        Assert.Equal(CandidateIds(job), CandidateIds(TransformPositions(job, ScaleByTwo)));
    }

    [Fact]
    public void Analyse_ScaledByTwo_DoublesDistanceExactly()
    {
        DrillJob job = CreateRandomJob(seed: 11, pointCount: 40, precedenceEdgeCount: 8, RandomJobFeatures.LockedGroup);

        Assert.Equal(CandidateMetres(job) * 2, CandidateMetres(TransformPositions(job, ScaleByTwo)));
    }

    [Theory]
    [InlineData("mirror")]
    [InlineData("rotate90")]
    public void Analyse_DistancePreservingTransform_KeepsRoute(string transformName)
    {
        DrillJob job = CreateRandomJob(seed: 17, pointCount: 35, precedenceEdgeCount: 5, RandomJobFeatures.FixedEnd);

        Assert.Equal(CandidateIds(job), CandidateIds(TransformPositions(job, DistancePreservingTransform(transformName))));
    }

    [Theory]
    [InlineData("mirror")]
    [InlineData("rotate90")]
    public void Analyse_DistancePreservingTransform_KeepsDistanceExactly(string transformName)
    {
        DrillJob job = CreateRandomJob(seed: 17, pointCount: 35, precedenceEdgeCount: 5, RandomJobFeatures.FixedEnd);

        Assert.Equal(CandidateMetres(job), CandidateMetres(TransformPositions(job, DistancePreservingTransform(transformName))));
    }

    private static Point2 ScaleByTwo(Point2 point) => new(point.X * 2, point.Y * 2);

    private static Func<Point2, Point2> DistancePreservingTransform(string name) =>
        name == "mirror" ? point => new Point2(-point.X, point.Y) : point => new Point2(-point.Y, point.X);

    private static string[] CandidateIds(DrillJob job) => IdsOf(AnalysisEngineTests.AnalyseSuccessfully(job, DefaultOptions).CandidateRoute);

    private static double CandidateMetres(DrillJob job) => AnalysisEngineTests.AnalyseSuccessfully(job, DefaultOptions).CandidateCheck.Distance.Metres;

    private static double FindBruteForceOptimum(DrillJob job)
    {
        double best = double.PositiveInfinity;
        foreach (PointId[] permutation in EnumeratePermutations(job.Points.Select(point => point.Id).ToArray()))
        {
            FeasibilityReport report = FeasibilityValidator.Validate(job, new Route(permutation));
            if (report.IsFeasible)
            {
                best = Math.Min(best, report.Distance.Metres);
            }
        }

        return best;
    }

    private static IEnumerable<PointId[]> EnumeratePermutations(PointId[] items)
    {
        if (items.Length <= 1)
        {
            yield return items;
            yield break;
        }

        for (int i = 0; i < items.Length; i++)
        {
            PointId[] remaining = items.Where((_, index) => index != i).ToArray();
            foreach (PointId[] tail in EnumeratePermutations(remaining))
            {
                yield return [items[i], .. tail];
            }
        }
    }
}
