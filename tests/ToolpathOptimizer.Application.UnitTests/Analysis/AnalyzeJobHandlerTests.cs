using System.Text.Json;
using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Validation;

namespace ToolpathOptimizer.Application.UnitTests.Analysis;

public sealed class AnalyzeJobHandlerTests
{
    [Fact]
    public void Handle_ProducesReportForSample()
    {
        Result<AnalysisReport> result = new Handlers().Analyze.Handle(Inputs.Job(Inputs.SquareCsv), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("baseline", result.Value.Summary.SelectedRoute);
        Assert.Equal(300, result.Value.Summary.BaselineDistanceMm, 9);
        Assert.Contains(AnalysisCodes.NoImprovementBaselineSelected, result.Value.Summary.WarningCodes);
    }

    [Fact]
    public void Handle_IsByteIdenticalForSameInputAndClock()
    {
        var handlers = new Handlers();

        byte[] first = handlers.Analyze.Handle(Inputs.Job(Inputs.CrossingCsv), CancellationToken.None).Value.Content;
        byte[] second = handlers.Analyze.Handle(Inputs.Job(Inputs.CrossingCsv), CancellationToken.None).Value.Content;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Handle_MapsCycleToInfeasibleCategory()
    {
        string manifest = Inputs.Manifest(",\"constraints\":{\"precedence\":[{\"before\":\"H01\",\"after\":\"H02\"},{\"before\":\"H02\",\"after\":\"H01\"}]}");

        Result<AnalysisReport> result = new Handlers().Analyze.Handle(Inputs.Job(Inputs.SquareCsv, manifest), CancellationToken.None);

        Assert.Equal(OutcomeCategory.Infeasible, result.Outcome.Category);
        Assert.Equal(ValidationCodes.ConstraintCycle, result.Outcome.Errors[0].Code);
    }

    [Fact]
    public void Handle_MapsUnknownPointToInputCategory()
    {
        string manifest = Inputs.Manifest(",\"constraints\":{\"precedence\":[{\"before\":\"H01\",\"after\":\"ZZ\"}]}");

        Result<AnalysisReport> result = new Handlers().Analyze.Handle(Inputs.Job(Inputs.SquareCsv, manifest), CancellationToken.None);

        Assert.Equal(OutcomeCategory.Input, result.Outcome.Category);
    }

    [Fact]
    public void Handle_ReturnsCancelledWhenCallerCancels()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Result<AnalysisReport> result = new Handlers().Analyze.Handle(Inputs.Job(Inputs.SquareCsv), cancelled.Token);

        Assert.Equal(ErrorCodes.Cancelled, result.Outcome.Errors[0].Code);
        Assert.Equal(OutcomeCategory.Cancelled, result.Outcome.Category);
    }

    [Fact]
    public void Handle_ReturnsDeadlineExceededWithoutReport()
    {
        var handlers = new Handlers(timeProvider: new ExpiredDeadlineTimeProvider());

        Result<AnalysisReport> result = handlers.Analyze.Handle(Inputs.Job(Inputs.SquareCsv), CancellationToken.None);

        Assert.Equal(ErrorCodes.OptDeadlineExceeded, result.Outcome.Errors[0].Code);
        Assert.Equal(OutcomeCategory.Budget, result.Outcome.Category);
    }

    [Fact]
    public void Handle_ReportRecordsEffectiveParametersAndProvenance()
    {
        byte[] content = new Handlers().Analyze.Handle(Inputs.Job(Inputs.SquareCsv), CancellationToken.None).Value.Content;

        using JsonDocument report = JsonDocument.Parse(content);
        JsonElement root = report.RootElement;
        Assert.Equal(100, root.GetProperty("parameters").GetProperty("max_passes").GetInt64());
        Assert.Equal(2, root.GetProperty("source_files").GetArrayLength());
        Assert.Equal(64, root.GetProperty("input_sha256").GetString()!.Length);
        Assert.Equal("2026-11-19T12:00:00.000Z", root.GetProperty("generated_at_utc").GetString());
    }
}
