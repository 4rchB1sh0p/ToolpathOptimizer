using ToolpathOptimizer.Core.Geometry;

namespace ToolpathOptimizer.Core.UnitTests.Geometry;

public sealed class Point2Tests
{
    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void Constructor_RejectsNonFiniteCoordinates(double x, double y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Point2(x, y));
    }

    [Fact]
    public void FromMillimetres_ConvertsToMetres()
    {
        var point = Point2.FromMillimetres(1500, -250);

        Assert.Equal(1.5, point.X);
        Assert.Equal(-0.25, point.Y);
    }
}
