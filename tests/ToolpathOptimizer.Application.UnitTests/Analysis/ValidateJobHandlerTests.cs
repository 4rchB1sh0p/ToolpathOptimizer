using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.UnitTests.Support;
using ToolpathOptimizer.Core.Validation;

namespace ToolpathOptimizer.Application.UnitTests.Analysis;

/// <summary>Tests for <see cref="ValidateJobHandler"/> and the canonical input hash.</summary>
public sealed class ValidateJobHandlerTests
{
    private const string ReorderedSquareCsv = "point_id,x_mm,y_mm,sequence,operation_group\nH02,100,0,2,A\nH01,0,0,1,A\nH04,0,100,4,A\nH03,100,100,3,A\n";
    private const string MovedSquareCsv = "point_id,x_mm,y_mm,sequence,operation_group\nH01,0,0,1,A\nH02,100,0,2,A\nH03,100,100,3,A\nH04,0,101,4,A\n";

    private static string HashOf(string csv) => new TestHandlers().Validate.Handle(TestInputs.CreateJob(csv)).Value.InputSha256;

    [Fact]
    public void Handle_ValidJob_CountsPoints()
    {
        Assert.Equal(4, new TestHandlers().Validate.Handle(TestInputs.CreateJob(TestInputs.SquareCsv)).Value.PointCount);
    }

    [Fact]
    public void Handle_UnknownLockedGroup_ReturnsUnknownGroup()
    {
        string manifest = TestInputs.ManifestWith(",\"constraints\":{\"locked_groups\":[\"B\"]}");

        var result = new TestHandlers().Validate.Handle(TestInputs.CreateJob(TestInputs.SquareCsv, manifest));

        Assert.Equal(ValidationCodes.ConstraintUnknownGroup, result.Outcome.Errors[0].Code);
    }

    [Fact]
    public void Handle_RowsReordered_KeepsCanonicalHash()
    {
        Assert.Equal(HashOf(TestInputs.SquareCsv), HashOf(ReorderedSquareCsv));
    }

    [Fact]
    public void Handle_CoordinateChanged_ChangesCanonicalHash()
    {
        Assert.NotEqual(HashOf(TestInputs.SquareCsv), HashOf(MovedSquareCsv));
    }
}
