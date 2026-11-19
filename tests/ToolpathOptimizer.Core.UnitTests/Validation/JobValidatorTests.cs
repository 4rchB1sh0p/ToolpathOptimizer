using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.UnitTests.Support;
using ToolpathOptimizer.Core.Validation;
using static ToolpathOptimizer.Core.UnitTests.Support.TestData;

namespace ToolpathOptimizer.Core.UnitTests.Validation;

/// <summary>Tests for <see cref="JobValidator"/>.</summary>
public sealed class JobValidatorTests
{
    /// <summary>A and B form operation group G; C and D are ungrouped.</summary>
    private static JobBuilder FourPoints() =>
        JobBuilder.WithPoints(PointInGroup("A", 0, 0, "G"), PointInGroup("B", 10, 0, "G"), PointAt("C", 20, 0), PointAt("D", 30, 0));

    [Fact]
    public void Validate_NoConstraints_CreatesOneNodePerPoint()
    {
        ValidationResult result = JobValidator.Validate(FourPoints().Build());

        Assert.Equal(4, result.Model.Nodes.Count);
    }

    [Fact]
    public void Validate_UnknownPrecedencePoint_ReportsUnknownPoint()
    {
        ValidationResult result = JobValidator.Validate(FourPoints().WithPrecedence("A", "Z").Build());

        ValidationIssue issue = Assert.Single(result.Issues);
        Assert.Equal((ValidationCodes.ConstraintUnknownPoint, true), (issue.Code, issue.Message.Contains("'Z'", StringComparison.Ordinal)));
    }

    [Fact]
    public void Validate_UnknownLockedGroup_ReportsUnknownGroup()
    {
        ValidationResult result = JobValidator.Validate(FourPoints().WithLockedGroup("NOPE").Build());

        Assert.Equal(ValidationCodes.ConstraintUnknownGroup, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void Validate_ThreePointCycle_ListsMembersOrdinally()
    {
        DrillJob job = FourPoints().WithPrecedence("D", "C").WithPrecedence("C", "B").WithPrecedence("B", "D").Build();

        ValidationIssue issue = Assert.Single(JobValidator.Validate(job).Issues);

        Assert.Equal((ValidationCodes.ConstraintCycle, true), (issue.Code, issue.Message.Contains("B, C, D", StringComparison.Ordinal)));
    }

    [Fact]
    public void Validate_SelfEdge_ReportsCycle()
    {
        ValidationResult result = JobValidator.Validate(FourPoints().WithPrecedence("C", "C").Build());

        Assert.Equal(ValidationCodes.ConstraintCycle, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void Validate_PrecedenceAgainstLockedGroupOrder_ReportsConflict()
    {
        ValidationResult result = JobValidator.Validate(FourPoints().WithPrecedence("B", "A").WithLockedGroup("G").Build());

        Assert.Equal(ValidationCodes.ConstraintLockedGroupConflict, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void Validate_CycleCreatedOnlyByBlockContraction_ReportsConflict()
    {
        // C -> A and B -> C are acyclic on points, but A and B form one block, so block -> C -> block is a cycle.
        ValidationResult result = JobValidator.Validate(FourPoints().WithPrecedence("C", "A").WithPrecedence("B", "C").WithLockedGroup("G").Build());

        Assert.Equal(ValidationCodes.ConstraintLockedGroupConflict, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void Validate_LockedGroup_ContractsMembersIntoFirstNode()
    {
        ValidationResult result = JobValidator.Validate(FourPoints().WithLockedGroup("G").Build());

        Assert.Equal([0, 1], result.Model.Nodes[0].Members);
    }
}
