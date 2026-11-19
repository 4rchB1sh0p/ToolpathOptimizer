using Microsoft.Extensions.Options;

namespace ToolpathOptimizer.Application.Limits;

/// <summary>
/// Validates <see cref="AnalysisLimitsOptions"/> at start-up; invalid options stop the host.
/// </summary>
public sealed class AnalysisLimitsOptionsValidator : IValidateOptions<AnalysisLimitsOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AnalysisLimitsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        Require(options.MaxPointsFileBytes > 0, "MaxPointsFileBytes must be positive.", failures);
        Require(options.MaxManifestFileBytes > 0, "MaxManifestFileBytes must be positive.", failures);
        Require(options.MaxPoints > 0, "MaxPoints must be positive.", failures);
        Require(options.MaxPrecedenceEdges >= 0, "MaxPrecedenceEdges must not be negative.", failures);
        Require(double.IsFinite(options.MaxCoordinateMagnitudeMm) && options.MaxCoordinateMagnitudeMm > 0, "MaxCoordinateMagnitudeMm must be positive and finite.", failures);
        Require(options.MaxPassesCap >= 0 && options.DefaultMaxPasses >= 0 && options.DefaultMaxPasses <= options.MaxPassesCap, "DefaultMaxPasses must be within 0..MaxPassesCap.", failures);
        Require(options.MaxMoveEvaluationsCap >= 0 && options.DefaultMaxMoveEvaluations >= 0 && options.DefaultMaxMoveEvaluations <= options.MaxMoveEvaluationsCap, "DefaultMaxMoveEvaluations must be within 0..MaxMoveEvaluationsCap.", failures);
        Require(double.IsFinite(options.MinImprovementCapMm) && options.MinImprovementCapMm >= 0, "MinImprovementCapMm must be finite and non-negative.", failures);
        Require(double.IsFinite(options.DefaultMinImprovementMm) && options.DefaultMinImprovementMm >= 0 && options.DefaultMinImprovementMm <= options.MinImprovementCapMm, "DefaultMinImprovementMm must be within 0..MinImprovementCapMm.", failures);
        Require(options.Deadline > TimeSpan.Zero, "Deadline must be positive.", failures);
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }
}
