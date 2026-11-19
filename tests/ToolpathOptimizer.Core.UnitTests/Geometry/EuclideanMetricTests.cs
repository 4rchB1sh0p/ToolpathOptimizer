using ToolpathOptimizer.Core.Geometry;

namespace ToolpathOptimizer.Core.UnitTests.Geometry;

public sealed class EuclideanMetricTests
{
    [Fact]
    public void Distance_OfThreeFourFiveTriangle_IsFive()
    {
        Assert.Equal(5.0, EuclideanMetric.Distance(new Point2(0, 0), new Point2(3, 4)));
    }

    [Fact]
    public void SquaredDistance_IsSymmetric()
    {
        var a = new Point2(1.5, -2);
        var b = new Point2(-7, 3.25);

        Assert.Equal(EuclideanMetric.SquaredDistance(a, b), EuclideanMetric.SquaredDistance(b, a));
    }
}
