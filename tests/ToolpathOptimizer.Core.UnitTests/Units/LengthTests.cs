using ToolpathOptimizer.Core.Units;

namespace ToolpathOptimizer.Core.UnitTests.Units;

public sealed class LengthTests
{
    [Fact]
    public void FromMillimetres_StoresMetres()
    {
        Assert.Equal(0.25, Length.FromMillimetres(250).Metres);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FromMetres_RejectsNonFinite(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Length.FromMetres(value));
    }

    [Fact]
    public void Subtraction_CanBeNegative()
    {
        Length delta = Length.FromMetres(1) - Length.FromMetres(3);

        Assert.Equal(-2, delta.Metres);
        Assert.True(delta < Length.Zero);
    }
}
