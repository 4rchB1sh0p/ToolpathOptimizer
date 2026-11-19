using ToolpathOptimizer.Core.Geometry;

namespace ToolpathOptimizer.Core.UnitTests.Geometry;

/// <summary>Tests for <see cref="EuclideanMetric"/>.</summary>
public sealed class EuclideanMetricTests
{
    [Fact]
    public void Distance_ThreeFourFiveTriangle_IsFive()
    {
        Assert.Equal(5.0, EuclideanMetric.Distance(new Point2(0, 0), new Point2(3, 4)));
    }

    [Fact]
    public void SquaredDistance_SwappedArguments_IsEqual()
    {
        var first = new Point2(1.5, -2);
        var second = new Point2(-7, 3.25);

        Assert.Equal(EuclideanMetric.SquaredDistance(first, second), EuclideanMetric.SquaredDistance(second, first));
    }
}
