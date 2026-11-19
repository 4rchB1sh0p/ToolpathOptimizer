using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Verification;
using static ToolpathOptimizer.Core.UnitTests.TestJobs;

namespace ToolpathOptimizer.Core.UnitTests.Verification;

/// <summary>Validator self-tests: deliberately corrupted routes must be rejected.</summary>
public sealed class FeasibilityValidatorTests
{
    private static readonly DrillJob s_job = Job(
        [P("A", 0, 0), P("B", 10, 0, "G"), P("C", 20, 0, "G"), P("D", 30, 0)],
        precedence: [("A", "D")],
        lockedGroups: ["G"]);

    [Fact]
    public void Validate_AcceptsFeasibleRoute()
    {
        FeasibilityReport report = FeasibilityValidator.Validate(s_job, Route("A", "B", "C", "D"));

        Assert.True(report.IsFeasible);
        Assert.Equal(0.030, report.Distance.Metres, 12);
    }

    [Theory]
    [InlineData(FeasibilityCodes.RouteMissingPoint, new[] { "A", "B", "C" })]
    [InlineData(FeasibilityCodes.RouteDuplicatePoint, new[] { "A", "B", "C", "D", "D" })]
    [InlineData(FeasibilityCodes.RouteUnknownPoint, new[] { "A", "B", "C", "D", "Z" })]
    [InlineData(FeasibilityCodes.PrecedenceViolated, new[] { "D", "B", "C", "A" })]
    [InlineData(FeasibilityCodes.LockedGroupNotContiguous, new[] { "A", "B", "D", "C" })]
    [InlineData(FeasibilityCodes.LockedGroupOrderChanged, new[] { "A", "C", "B", "D" })]
    public void Validate_RejectsCorruptedRoute(string expectedCode, string[] ids)
    {
        FeasibilityReport report = FeasibilityValidator.Validate(s_job, Route(ids));

        Assert.False(report.IsFeasible);
        Assert.Contains(report.Violations, v => v.Code == expectedCode);
    }

    [Fact]
    public void Validate_CapsReportedViolationsButCountsAll()
    {
        DrillPoint[] points = Enumerable.Range(0, 120).Select(i => P($"P{i:D3}", i, 0)).ToArray();
        (string, string)[] edges = Enumerable.Range(0, 119).Select(i => ($"P{i + 1:D3}", $"P{i:D3}")).ToArray();
        DrillJob job = Job(points, precedence: edges);

        FeasibilityReport report = FeasibilityValidator.Validate(job, job.CreateBaselineRoute());

        Assert.Equal(119, report.ViolationCount);
        Assert.Equal(FeasibilityValidator.MaxReportedViolations, report.Violations.Count);
    }
}
