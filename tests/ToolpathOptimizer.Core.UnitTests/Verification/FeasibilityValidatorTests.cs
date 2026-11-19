using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.UnitTests.Support;
using ToolpathOptimizer.Core.Verification;
using static ToolpathOptimizer.Core.UnitTests.Support.TestData;

namespace ToolpathOptimizer.Core.UnitTests.Verification;

/// <summary>Tests for <see cref="FeasibilityValidator"/>, including self-tests with deliberately corrupted routes.</summary>
public sealed class FeasibilityValidatorTests
{
    /// <summary>A before D; B and C form locked group G.</summary>
    private static readonly DrillJob _job = JobBuilder
        .WithPoints(PointAt("A", 0, 0), PointInGroup("B", 10, 0, "G"), PointInGroup("C", 20, 0, "G"), PointAt("D", 30, 0))
        .WithPrecedence("A", "D")
        .WithLockedGroup("G")
        .Build();

    [Fact]
    public void Validate_FeasibleRoute_HasNoViolations()
    {
        FeasibilityReport report = FeasibilityValidator.Validate(_job, RouteOf("A", "B", "C", "D"));

        Assert.True(report.IsFeasible, string.Join("; ", report.Violations.Select(v => v.Code)));
    }

    [Fact]
    public void Validate_FeasibleRoute_RecomputesDistance()
    {
        FeasibilityReport report = FeasibilityValidator.Validate(_job, RouteOf("A", "B", "C", "D"));

        Assert.Equal(0.030, report.Distance.Metres, 12);
    }

    [Theory]
    [InlineData(FeasibilityCodes.RouteMissingPoint, new[] { "A", "B", "C" })]
    [InlineData(FeasibilityCodes.RouteDuplicatePoint, new[] { "A", "B", "C", "D", "D" })]
    [InlineData(FeasibilityCodes.RouteUnknownPoint, new[] { "A", "B", "C", "D", "Z" })]
    [InlineData(FeasibilityCodes.PrecedenceViolated, new[] { "D", "B", "C", "A" })]
    [InlineData(FeasibilityCodes.LockedGroupNotContiguous, new[] { "A", "B", "D", "C" })]
    [InlineData(FeasibilityCodes.LockedGroupOrderChanged, new[] { "A", "C", "B", "D" })]
    public void Validate_CorruptedRoute_ReportsViolation(string expectedCode, string[] ids)
    {
        FeasibilityReport report = FeasibilityValidator.Validate(_job, RouteOf(ids));

        Assert.Contains(report.Violations, violation => violation.Code == expectedCode);
    }

    [Fact]
    public void Validate_ManyViolations_CapsListButCountsAll()
    {
        DrillPoint[] points = Enumerable.Range(0, 120).Select(i => PointAt($"P{i:D3}", i, 0)).ToArray();
        JobBuilder builder = JobBuilder.WithPoints(points);
        for (int i = 0; i < 119; i++)
        {
            builder.WithPrecedence($"P{i + 1:D3}", $"P{i:D3}");
        }

        DrillJob job = builder.Build();
        FeasibilityReport report = FeasibilityValidator.Validate(job, job.CreateBaselineRoute());

        Assert.Equal((119, FeasibilityValidator.MaxReportedViolations), (report.ViolationCount, report.Violations.Count));
    }
}
