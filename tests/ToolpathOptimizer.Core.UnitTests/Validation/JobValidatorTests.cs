using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Validation;
using static ToolpathOptimizer.Core.UnitTests.TestJobs;

namespace ToolpathOptimizer.Core.UnitTests.Validation;

public sealed class JobValidatorTests
{
    private static readonly DrillPoint[] s_points =
        [P("A", 0, 0, "G"), P("B", 10, 0, "G"), P("C", 20, 0), P("D", 30, 0)];

    [Fact]
    public void Validate_AcceptsJobWithoutConstraints()
    {
        ValidationResult result = JobValidator.Validate(Job(s_points));

        Assert.True(result.IsValid);
        Assert.Equal(4, result.Model.Nodes.Count);
    }

    [Fact]
    public void Validate_RejectsUnknownPrecedencePoint()
    {
        ValidationResult result = JobValidator.Validate(Job(s_points, precedence: [("A", "Z")]));

        ValidationIssue issue = Assert.Single(result.Issues);
        Assert.Equal(ValidationCodes.ConstraintUnknownPoint, issue.Code);
        Assert.Contains("'Z'", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsUnknownLockedGroup()
    {
        ValidationResult result = JobValidator.Validate(Job(s_points, lockedGroups: ["NOPE"]));

        Assert.Equal(ValidationCodes.ConstraintUnknownGroup, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void Validate_RejectsCycleAndListsMembersOrdinally()
    {
        ValidationResult result = JobValidator.Validate(Job(s_points, precedence: [("D", "C"), ("C", "B"), ("B", "D")]));

        ValidationIssue issue = Assert.Single(result.Issues);
        Assert.Equal(ValidationCodes.ConstraintCycle, issue.Code);
        Assert.Contains("B, C, D", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsSelfEdgeAsCycle()
    {
        ValidationResult result = JobValidator.Validate(Job(s_points, precedence: [("C", "C")]));

        Assert.Equal(ValidationCodes.ConstraintCycle, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void Validate_RejectsPrecedenceContradictingLockedGroupOrder()
    {
        ValidationResult result = JobValidator.Validate(Job(s_points, precedence: [("B", "A")], lockedGroups: ["G"]));

        Assert.Equal(ValidationCodes.ConstraintLockedGroupConflict, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void Validate_RejectsCycleCreatedOnlyByBlockContraction()
    {
        // C -> A and B -> C are acyclic on points, but A and B form one block, so block -> C -> block is a cycle.
        ValidationResult result = JobValidator.Validate(Job(s_points, precedence: [("C", "A"), ("B", "C")], lockedGroups: ["G"]));

        Assert.Equal(ValidationCodes.ConstraintLockedGroupConflict, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void Validate_ContractsLockedGroupIntoOneNode()
    {
        ValidationResult result = JobValidator.Validate(Job(s_points, lockedGroups: ["G"]));

        Assert.Equal(3, result.Model.Nodes.Count);
        Assert.Equal([0, 1], result.Model.Nodes[0].Members);
    }
}
