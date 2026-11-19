using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;
using static ToolpathOptimizer.Core.UnitTests.TestJobs;

namespace ToolpathOptimizer.Core.UnitTests.Domain;

public sealed class DrillJobTests
{
    [Fact]
    public void Constructor_RejectsDuplicateIdentifiers()
    {
        Assert.Throws<ArgumentException>(() => Job([P("A", 0, 0), P("A", 1, 1)]));
    }

    [Fact]
    public void Constructor_RejectsEmptyPointList()
    {
        Assert.Throws<ArgumentException>(() => new DrillJob(new Point2(0, 0), null, [], ConstraintSet.Empty));
    }

    [Fact]
    public void CreateBaselineRoute_FollowsInputOrder()
    {
        DrillJob job = Job([P("B", 0, 0), P("A", 1, 1)]);

        Assert.Equal(["B", "A"], Ids(job.CreateBaselineRoute()));
    }
}
