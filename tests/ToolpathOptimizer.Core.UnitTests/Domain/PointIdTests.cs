using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Core.UnitTests.Domain;

/// <summary>Tests for <see cref="PointId"/>.</summary>
public sealed class PointIdTests
{
    [Theory]
    [InlineData("H01")]
    [InlineData("a")]
    [InlineData("A.b-c_9")]
    public void Constructor_ValidIdentifier_KeepsValue(string value)
    {
        Assert.Equal(value, new PointId(value).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("=SUM(A1)")]
    [InlineData("-1")]
    [InlineData("has space")]
    [InlineData("ümlaut")]
    public void Constructor_InvalidIdentifier_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new PointId(value));
    }

    [Fact]
    public void Constructor_IdentifierLongerThan64Characters_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PointId(new string('a', 65)));
    }

    [Fact]
    public void CompareTo_UpperAndLowerCase_OrdersOrdinally()
    {
        Assert.True(new PointId("B").CompareTo(new PointId("a")) < 0, "Ordinal comparison must place 'B' (0x42) before 'a' (0x61).");
    }
}
