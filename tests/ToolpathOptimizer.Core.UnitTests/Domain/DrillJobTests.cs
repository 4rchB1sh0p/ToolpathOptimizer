using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;
using ToolpathOptimizer.Core.UnitTests.Support;
using static ToolpathOptimizer.Core.UnitTests.Support.TestData;

namespace ToolpathOptimizer.Core.UnitTests.Domain;

/// <summary>Tests for <see cref="DrillJob"/>.</summary>
public sealed class DrillJobTests
{
    [Fact]
    public void Constructor_DuplicateIdentifiers_Throws()
    {
        Assert.Throws<ArgumentException>(() => JobBuilder.WithPoints(PointAt("A", 0, 0), PointAt("A", 1, 1)).Build());
    }

    [Fact]
    public void Constructor_NoPoints_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DrillJob(new Point2(0, 0), null, [], ConstraintSet.Empty));
    }

    [Fact]
    public void CreateBaselineRoute_AnyJob_FollowsInputOrder()
    {
        DrillJob job = JobBuilder.WithPoints(PointAt("B", 0, 0), PointAt("A", 1, 1)).Build();

        Assert.Equal(["B", "A"], IdsOf(job.CreateBaselineRoute()));
    }
}
