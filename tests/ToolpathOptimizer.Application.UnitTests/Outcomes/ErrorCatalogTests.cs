using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Application.UnitTests.Outcomes;

public sealed class ErrorCatalogTests
{
    [Theory]
    [InlineData("ARGS_MISSING_OPTION", OutcomeCategory.Arguments)]
    [InlineData("CONFIG_INVALID", OutcomeCategory.Arguments)]
    [InlineData("INPUT_DUPLICATE_POINT_ID", OutcomeCategory.Input)]
    [InlineData("CONSTRAINT_UNKNOWN_POINT", OutcomeCategory.Input)]
    [InlineData("CONSTRAINT_UNKNOWN_GROUP", OutcomeCategory.Input)]
    [InlineData("CONSTRAINT_CYCLE", OutcomeCategory.Infeasible)]
    [InlineData("CONSTRAINT_LOCKED_GROUP_CONFLICT", OutcomeCategory.Infeasible)]
    [InlineData("OPT_CANDIDATE_REJECTED", OutcomeCategory.Processing)]
    [InlineData("INTERNAL_ERROR", OutcomeCategory.Processing)]
    [InlineData("CANCELLED", OutcomeCategory.Cancelled)]
    [InlineData("IO_OUTPUT_EXISTS", OutcomeCategory.Io)]
    [InlineData("OPT_DEADLINE_EXCEEDED", OutcomeCategory.Budget)]
    [InlineData("SOMETHING_NEW", OutcomeCategory.Processing)]
    public void CategoryOf_MapsStableCodes(string code, OutcomeCategory expected)
    {
        Assert.Equal(expected, ErrorCatalog.CategoryOf(code));
    }
}
