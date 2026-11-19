using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Validation;
using static ToolpathOptimizer.Core.UnitTests.TestJobs;

namespace ToolpathOptimizer.Core.UnitTests.Optimisation;

public sealed class NearestNeighbourConstructorTests
{
    private static string[] Construct(DrillJob job)
    {
        ConstraintModel model = JobValidator.Validate(job).Model;
        return NearestNeighbourConstructor.Construct(model).Select(i => job.Points[i].Id.Value).ToArray();
    }

    [Fact]
    public void Construct_VisitsNearestPointFirst()
    {
        string[] order = Construct(Job([P("FAR", 100, 0), P("NEAR", 10, 0), P("MID", 50, 0)]));

        Assert.Equal(["NEAR", "MID", "FAR"], order);
    }

    [Fact]
    public void Construct_BreaksDistanceTiesByOrdinalId()
    {
        string[] order = Construct(Job([P("b", 10, 0), P("B", -10, 0)]));

        Assert.Equal("B", order[0]);
    }

    [Fact]
    public void Construct_RespectsPrecedence()
    {
        string[] order = Construct(Job([P("FAR", 100, 0), P("NEAR", 10, 0)], precedence: [("FAR", "NEAR")]));

        Assert.Equal(["FAR", "NEAR"], order);
    }

    [Fact]
    public void Construct_VisitsLockedBlockWholeInBaselineOrder()
    {
        string[] order = Construct(Job([P("L1", 100, 0, "G"), P("L2", 5, 0, "G"), P("X", 50, 0)], lockedGroups: ["G"]));

        Assert.Equal(["X", "L1", "L2"], order);
    }
}
