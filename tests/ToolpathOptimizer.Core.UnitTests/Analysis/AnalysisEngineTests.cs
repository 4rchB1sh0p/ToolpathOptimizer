using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Units;
using ToolpathOptimizer.Core.UnitTests.Support;
using ToolpathOptimizer.Core.Validation;
using static ToolpathOptimizer.Core.UnitTests.Support.TestData;

namespace ToolpathOptimizer.Core.UnitTests.Analysis;

/// <summary>Tests for <see cref="AnalysisEngine"/>: selection rules and warnings on exact-geometry cases.</summary>
public sealed class AnalysisEngineTests
{
    internal static AnalysisResult AnalyseSuccessfully(DrillJob job, AnalysisOptions options)
    {
        AnalysisOutcome outcome = AnalysisEngine.Analyse(job, options, CancellationToken.None);
        Assert.True(outcome.IsSuccess, "Analysis failed: " + string.Join("; ", outcome.Issues.Select(issue => issue.Code)));
        return outcome.Result;
    }

    private static DrillJob CrossingJob() =>
        JobBuilder.WithPoints(PointAt("A", 0, 10), PointAt("D", 10, 20), PointAt("C", 0, 20), PointAt("B", 10, 10)).Build();

    private static DrillJob OptimalSquareJob() =>
        JobBuilder.WithPoints(PointAt("H01", 0, 0), PointAt("H02", 100, 0), PointAt("H03", 100, 100), PointAt("H04", 0, 100)).Build();

    [Fact]
    public void Analyse_CrossingBaseline_SelectsCandidate()
    {
        AnalysisResult result = AnalyseSuccessfully(CrossingJob(), DefaultOptions);

        Assert.Equal((RouteSelection.Candidate, AnalysisEngine.ReasonCandidateShorter), (result.Selection, result.SelectionReason));
    }

    [Fact]
    public void Analyse_CrossingBaseline_ReportsNegativeDelta()
    {
        AnalysisResult result = AnalyseSuccessfully(CrossingJob(), DefaultOptions);

        Assert.True(result.Delta < Length.Zero, $"Expected a shorter candidate; delta was {result.Delta}.");
    }

    [Fact]
    public void Analyse_CrossingBaseline_HasNoWarnings()
    {
        Assert.Empty(AnalyseSuccessfully(CrossingJob(), DefaultOptions).Warnings);
    }

    [Fact]
    public void Analyse_OptimalBaseline_RetainsBaseline()
    {
        AnalysisResult result = AnalyseSuccessfully(OptimalSquareJob(), DefaultOptions);

        Assert.Equal(RouteSelection.Baseline, result.Selection);
    }

    [Fact]
    public void Analyse_OptimalBaseline_WarnsNoImprovement()
    {
        AnalysisResult result = AnalyseSuccessfully(OptimalSquareJob(), DefaultOptions);

        Assert.Contains(result.Warnings, warning => warning.Code == AnalysisCodes.NoImprovementBaselineSelected);
    }

    [Fact]
    public void Analyse_InfeasibleBaseline_SelectsValidatedCandidate()
    {
        DrillJob job = JobBuilder.WithPoints(PointAt("A", 0, 0), PointAt("B", 10, 0)).WithPrecedence("B", "A").Build();

        AnalysisResult result = AnalyseSuccessfully(job, DefaultOptions);

        Assert.Equal(["B", "A"], IdsOf(result.SelectedRoute));
    }

    [Fact]
    public void Analyse_InfeasibleBaseline_WarnsBaselineViolatesConstraints()
    {
        DrillJob job = JobBuilder.WithPoints(PointAt("A", 0, 0), PointAt("B", 10, 0)).WithPrecedence("B", "A").Build();

        AnalysisResult result = AnalyseSuccessfully(job, DefaultOptions);

        Assert.Contains(result.Warnings, warning => warning.Code == AnalysisCodes.BaselineViolatesConstraints);
    }

    [Fact]
    public void Analyse_ZeroBaselineDistance_ReportsNullPercent()
    {
        AnalysisResult result = AnalyseSuccessfully(JobBuilder.WithPoints(PointAt("A", 0, 0)).Build(), DefaultOptions);

        Assert.Null(result.DeltaPercent);
    }

    [Fact]
    public void Analyse_ZeroBaselineDistance_WarnsZeroBaseline()
    {
        AnalysisResult result = AnalyseSuccessfully(JobBuilder.WithPoints(PointAt("A", 0, 0)).Build(), DefaultOptions);

        Assert.Contains(result.Warnings, warning => warning.Code == AnalysisCodes.ZeroBaselineDistance);
    }

    [Fact]
    public void Analyse_ZeroBudget_WarnsBudgetExhausted()
    {
        var options = new AnalysisOptions(new SearchBudget(0, 0), Length.Zero);

        AnalysisResult result = AnalyseSuccessfully(CreateRandomJob(seed: 1, pointCount: 10, precedenceEdgeCount: 0, RandomJobFeatures.None), options);

        Assert.Contains(result.Warnings, warning => warning.Code == AnalysisCodes.SearchBudgetExhausted);
    }

    [Fact]
    public void Analyse_CyclicConstraints_ReturnsCycleIssue()
    {
        DrillJob job = JobBuilder.WithPoints(PointAt("A", 0, 0), PointAt("B", 1, 1)).WithPrecedence("A", "B").WithPrecedence("B", "A").Build();

        AnalysisOutcome outcome = AnalysisEngine.Analyse(job, DefaultOptions, CancellationToken.None);

        Assert.Equal(ValidationCodes.ConstraintCycle, Assert.Single(outcome.Issues).Code);
    }

    [Fact]
    public void Analyse_SameJobTwice_ProducesSameRoute()
    {
        DrillJob job = CreateRandomJob(seed: 42, pointCount: 60, precedenceEdgeCount: 15, RandomJobFeatures.LockedGroup | RandomJobFeatures.FixedEnd);

        Assert.Equal(IdsOf(AnalyseSuccessfully(job, DefaultOptions).CandidateRoute), IdsOf(AnalyseSuccessfully(job, DefaultOptions).CandidateRoute));
    }
}
