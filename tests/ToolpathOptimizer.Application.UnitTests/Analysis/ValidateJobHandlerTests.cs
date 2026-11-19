using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Core.Validation;

namespace ToolpathOptimizer.Application.UnitTests.Analysis;

public sealed class ValidateJobHandlerTests
{
    [Fact]
    public void Handle_SummarisesValidJob()
    {
        Result<JobValidationSummary> result = new Handlers().Validate.Handle(Inputs.Job(Inputs.SquareCsv));

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.PointCount);
    }

    [Fact]
    public void Handle_ReportsUnknownGroup()
    {
        string manifest = Inputs.Manifest(",\"constraints\":{\"locked_groups\":[\"B\"]}");

        Result<JobValidationSummary> result = new Handlers().Validate.Handle(Inputs.Job(Inputs.SquareCsv, manifest));

        Assert.Equal(ValidationCodes.ConstraintUnknownGroup, result.Outcome.Errors[0].Code);
    }

    [Fact]
    public void Handle_CanonicalHashIgnoresRowOrderButNotCoordinates()
    {
        var handlers = new Handlers();
        const string reordered = "point_id,x_mm,y_mm,sequence,operation_group\nH02,100,0,2,A\nH01,0,0,1,A\nH04,0,100,4,A\nH03,100,100,3,A\n";
        const string moved = "point_id,x_mm,y_mm,sequence,operation_group\nH01,0,0,1,A\nH02,100,0,2,A\nH03,100,100,3,A\nH04,0,101,4,A\n";

        string original = handlers.Validate.Handle(Inputs.Job(Inputs.SquareCsv)).Value.InputSha256;

        Assert.Equal(original, handlers.Validate.Handle(Inputs.Job(reordered)).Value.InputSha256);
        Assert.NotEqual(original, handlers.Validate.Handle(Inputs.Job(moved)).Value.InputSha256);
    }
}
