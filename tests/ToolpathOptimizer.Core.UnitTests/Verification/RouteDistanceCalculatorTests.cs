using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Units;
using ToolpathOptimizer.Core.Verification;
using static ToolpathOptimizer.Core.UnitTests.TestJobs;

namespace ToolpathOptimizer.Core.UnitTests.Verification;

public sealed class RouteDistanceCalculatorTests
{
    [Fact]
    public void Calculate_SquareTourWithoutEnd_IsThreeSides()
    {
        DrillJob job = Job([P("H01", 0, 0), P("H02", 100, 0), P("H03", 100, 100), P("H04", 0, 100)]);

        Length distance = RouteDistanceCalculator.Calculate(job, job.CreateBaselineRoute().OrderedIds);

        Assert.Equal(300, distance.Millimetres, 9);
    }

    [Fact]
    public void Calculate_IncludesStartAndEndLegs()
    {
        DrillJob job = Job([P("A", 3, 4)], startMm: (0, 0), endMm: (6, 8));

        Length distance = RouteDistanceCalculator.Calculate(job, job.CreateBaselineRoute().OrderedIds);

        Assert.Equal(10, distance.Millimetres, 9);
    }

    [Fact]
    public void Calculate_RejectsUnknownPoint()
    {
        DrillJob job = Job([P("A", 3, 4)]);

        Assert.Throws<ArgumentException>(() => RouteDistanceCalculator.Calculate(job, Route("Z").OrderedIds));
    }
}
