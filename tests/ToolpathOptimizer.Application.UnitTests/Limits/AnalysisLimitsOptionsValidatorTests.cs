using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Limits;

namespace ToolpathOptimizer.Application.UnitTests.Limits;

/// <summary>Tests for <see cref="AnalysisLimitsOptionsValidator"/>.</summary>
public sealed class AnalysisLimitsOptionsValidatorTests
{
    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        Assert.True(new AnalysisLimitsOptionsValidator().Validate(Options.DefaultName, new AnalysisLimitsOptions()).Succeeded, "Default options must be valid.");
    }

    [Fact]
    public void Validate_ZeroDeadline_Fails()
    {
        Assert.True(new AnalysisLimitsOptionsValidator().Validate(Options.DefaultName, new AnalysisLimitsOptions { Deadline = TimeSpan.Zero }).Failed, "A zero deadline must be rejected.");
    }

    [Fact]
    public void Validate_DefaultPassesAboveCap_Fails()
    {
        var options = new AnalysisLimitsOptions { DefaultMaxPasses = 10, MaxPassesCap = 5 };

        Assert.True(new AnalysisLimitsOptionsValidator().Validate(Options.DefaultName, options).Failed, "A default above its cap must be rejected.");
    }
}
