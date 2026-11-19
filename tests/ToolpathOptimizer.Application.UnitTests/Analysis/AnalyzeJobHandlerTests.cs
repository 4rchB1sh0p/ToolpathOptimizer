using System.Text.Json;
using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.UnitTests.Support;
using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Validation;

namespace ToolpathOptimizer.Application.UnitTests.Analysis;

/// <summary>Tests for <see cref="AnalyzeJobHandler"/>.</summary>
public sealed class AnalyzeJobHandlerTests
{
    private static Result<AnalysisReport> Analyse(string csv, string manifest) =>
        new TestHandlers().Analyze.Handle(TestInputs.CreateJob(csv, manifest), CancellationToken.None);

    private static string CycleManifest() =>
        TestInputs.ManifestWith(",\"constraints\":{\"precedence\":[{\"before\":\"H01\",\"after\":\"H02\"},{\"before\":\"H02\",\"after\":\"H01\"}]}");

    [Fact]
    public void Handle_OptimalSample_SelectsBaseline()
    {
        Result<AnalysisReport> result = Analyse(TestInputs.SquareCsv, TestInputs.MinimalManifest);

        Assert.Equal("baseline", result.Value.Summary.SelectedRoute);
    }

    [Fact]
    public void Handle_OptimalSample_ReportsBaselineDistance()
    {
        Result<AnalysisReport> result = Analyse(TestInputs.SquareCsv, TestInputs.MinimalManifest);

        Assert.Equal(300, result.Value.Summary.BaselineDistanceMm, 9);
    }

    [Fact]
    public void Handle_OptimalSample_WarnsNoImprovement()
    {
        Result<AnalysisReport> result = Analyse(TestInputs.SquareCsv, TestInputs.MinimalManifest);

        Assert.Contains(AnalysisCodes.NoImprovementBaselineSelected, result.Value.Summary.WarningCodes);
    }

    [Fact]
    public void Handle_SameInputAndClockTwice_ProducesIdenticalBytes()
    {
        var handlers = new TestHandlers();

        byte[] first = handlers.Analyze.Handle(TestInputs.CreateJob(TestInputs.CrossingCsv), CancellationToken.None).Value.Content;
        byte[] second = handlers.Analyze.Handle(TestInputs.CreateJob(TestInputs.CrossingCsv), CancellationToken.None).Value.Content;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Handle_CyclicConstraints_ReturnsInfeasibleCycle()
    {
        OperationOutcome outcome = Analyse(TestInputs.SquareCsv, CycleManifest()).Outcome;

        Assert.Equal((OutcomeCategory.Infeasible, ValidationCodes.ConstraintCycle), (outcome.Category, outcome.Errors[0].Code));
    }

    [Fact]
    public void Handle_UnknownPrecedencePoint_ReturnsInputCategory()
    {
        string manifest = TestInputs.ManifestWith(",\"constraints\":{\"precedence\":[{\"before\":\"H01\",\"after\":\"ZZ\"}]}");

        Assert.Equal(OutcomeCategory.Input, Analyse(TestInputs.SquareCsv, manifest).Outcome.Category);
    }

    [Fact]
    public void Handle_CallerCancelled_ReturnsCancelled()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        OperationOutcome outcome = new TestHandlers().Analyze.Handle(TestInputs.CreateJob(TestInputs.SquareCsv), cancelled.Token).Outcome;

        Assert.Equal((OutcomeCategory.Cancelled, ErrorCodes.Cancelled), (outcome.Category, outcome.Errors[0].Code));
    }

    [Fact]
    public void Handle_DeadlineReached_ReturnsBudgetFailure()
    {
        var handlers = new TestHandlers(new ExpiredDeadlineTimeProvider());

        OperationOutcome outcome = handlers.Analyze.Handle(TestInputs.CreateJob(TestInputs.SquareCsv), CancellationToken.None).Outcome;

        Assert.Equal((OutcomeCategory.Budget, ErrorCodes.OptDeadlineExceeded), (outcome.Category, outcome.Errors[0].Code));
    }

    [Fact]
    public void Handle_DefaultManifest_RecordsEffectivePassBudget()
    {
        using JsonDocument report = JsonDocument.Parse(Analyse(TestInputs.SquareCsv, TestInputs.MinimalManifest).Value.Content);

        Assert.Equal(100, report.RootElement.GetProperty("parameters").GetProperty("max_passes").GetInt64());
    }

    [Fact]
    public void Handle_AnyInput_RecordsBothSourceFiles()
    {
        using JsonDocument report = JsonDocument.Parse(Analyse(TestInputs.SquareCsv, TestInputs.MinimalManifest).Value.Content);

        Assert.Equal(2, report.RootElement.GetProperty("source_files").GetArrayLength());
    }

    [Fact]
    public void Handle_FixedClock_StampsGenerationTime()
    {
        using JsonDocument report = JsonDocument.Parse(Analyse(TestInputs.SquareCsv, TestInputs.MinimalManifest).Value.Content);

        Assert.Equal("2026-11-19T12:00:00.000Z", report.RootElement.GetProperty("generated_at_utc").GetString());
    }
}
