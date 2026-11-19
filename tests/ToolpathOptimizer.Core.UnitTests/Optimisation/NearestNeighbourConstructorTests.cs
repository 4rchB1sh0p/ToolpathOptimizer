using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.UnitTests.Support;
using ToolpathOptimizer.Core.Validation;
using static ToolpathOptimizer.Core.UnitTests.Support.TestData;

namespace ToolpathOptimizer.Core.UnitTests.Optimisation;

/// <summary>Tests for <see cref="NearestNeighbourConstructor"/>.</summary>
public sealed class NearestNeighbourConstructorTests
{
    private static string[] ConstructOrder(DrillJob job)
    {
        ConstraintModel model = JobValidator.Validate(job).Model;
        return NearestNeighbourConstructor.Construct(model).Select(index => job.Points[index].Id.Value).ToArray();
    }

    [Fact]
    public void Construct_UnconstrainedLine_VisitsNearestFirst()
    {
        string[] order = ConstructOrder(JobBuilder.WithPoints(PointAt("FAR", 100, 0), PointAt("NEAR", 10, 0), PointAt("MID", 50, 0)).Build());

        Assert.Equal(["NEAR", "MID", "FAR"], order);
    }

    [Fact]
    public void Construct_EquidistantPoints_BreaksTieByOrdinalId()
    {
        string[] order = ConstructOrder(JobBuilder.WithPoints(PointAt("b", 10, 0), PointAt("B", -10, 0)).Build());

        Assert.Equal("B", order[0]);
    }

    [Fact]
    public void Construct_PrecedenceOnNearerPoint_VisitsPredecessorFirst()
    {
        string[] order = ConstructOrder(JobBuilder.WithPoints(PointAt("FAR", 100, 0), PointAt("NEAR", 10, 0)).WithPrecedence("FAR", "NEAR").Build());

        Assert.Equal(["FAR", "NEAR"], order);
    }

    [Fact]
    public void Construct_LockedBlock_VisitsWholeBlockInBaselineOrder()
    {
        DrillJob job = JobBuilder.WithPoints(PointInGroup("L1", 100, 0, "G"), PointInGroup("L2", 5, 0, "G"), PointAt("X", 50, 0)).WithLockedGroup("G").Build();

        Assert.Equal(["X", "L1", "L2"], ConstructOrder(job));
    }
}
