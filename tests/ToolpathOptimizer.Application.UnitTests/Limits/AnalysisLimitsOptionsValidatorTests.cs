using ToolpathOptimizer.Application.Limits;

namespace ToolpathOptimizer.Application.UnitTests.Limits;

public sealed class AnalysisLimitsOptionsValidatorTests
{
    [Fact]
    public void Validate_AcceptsDefaults()
    {
        Assert.True(new AnalysisLimitsOptionsValidator().Validate(null, new AnalysisLimitsOptions()).Succeeded);
    }

    [Fact]
    public void Validate_RejectsNonPositiveDeadline()
    {
        Assert.True(new AnalysisLimitsOptionsValidator().Validate(null, new AnalysisLimitsOptions { Deadline = TimeSpan.Zero }).Failed);
    }

    [Fact]
    public void Validate_RejectsDefaultAboveCap()
    {
        var options = new AnalysisLimitsOptions { DefaultMaxPasses = 10, MaxPassesCap = 5 };

        Assert.True(new AnalysisLimitsOptionsValidator().Validate(null, options).Failed);
    }
}
