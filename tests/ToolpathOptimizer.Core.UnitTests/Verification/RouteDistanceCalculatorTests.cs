using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Units;
using ToolpathOptimizer.Core.UnitTests.Support;
using ToolpathOptimizer.Core.Verification;
using static ToolpathOptimizer.Core.UnitTests.Support.TestData;

namespace ToolpathOptimizer.Core.UnitTests.Verification;

/// <summary>Tests for <see cref="RouteDistanceCalculator"/>.</summary>
public sealed class RouteDistanceCalculatorTests
{
    [Fact]
    public void Calculate_SquareWithoutEnd_IsThreeSides()
    {
        DrillJob job = JobBuilder.WithPoints(PointAt("H01", 0, 0), PointAt("H02", 100, 0), PointAt("H03", 100, 100), PointAt("H04", 0, 100)).Build();

        Length distance = RouteDistanceCalculator.Calculate(job, job.CreateBaselineRoute().OrderedIds);

        Assert.Equal(300, distance.Millimetres, 9);
    }

    [Fact]
    public void Calculate_FixedEnd_IncludesStartAndEndLegs()
    {
        DrillJob job = JobBuilder.WithPoints(PointAt("A", 3, 4)).EndingAt(6, 8).Build();

        Length distance = RouteDistanceCalculator.Calculate(job, job.CreateBaselineRoute().OrderedIds);

        Assert.Equal(10, distance.Millimetres, 9);
    }

    [Fact]
    public void Calculate_UnknownPoint_Throws()
    {
        DrillJob job = JobBuilder.WithPoints(PointAt("A", 3, 4)).Build();

        Assert.Throws<ArgumentException>(() => RouteDistanceCalculator.Calculate(job, RouteOf("Z").OrderedIds));
    }
}
