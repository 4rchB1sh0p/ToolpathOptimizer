using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Core.UnitTests.Domain;

public sealed class PointIdTests
{
    [Theory]
    [InlineData("H01")]
    [InlineData("a")]
    [InlineData("A.b-c_9")]
    public void Constructor_AcceptsValidIdentifiers(string value)
    {
        Assert.Equal(value, new PointId(value).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("=SUM(A1)")]
    [InlineData("-1")]
    [InlineData("has space")]
    [InlineData("ümlaut")]
    public void Constructor_RejectsInvalidIdentifiers(string value)
    {
        Assert.Throws<ArgumentException>(() => new PointId(value));
    }

    [Fact]
    public void Constructor_RejectsIdentifiersLongerThan64Characters()
    {
        Assert.Throws<ArgumentException>(() => new PointId(new string('a', 65)));
    }

    [Fact]
    public void CompareTo_IsOrdinal()
    {
        Assert.True(new PointId("B").CompareTo(new PointId("a")) < 0);
    }
}
