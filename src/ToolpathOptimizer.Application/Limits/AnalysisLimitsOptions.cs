using Microsoft.Extensions.Options;

namespace ToolpathOptimizer.Application.Limits;

/// <summary>
/// Parsing limits, search budget defaults/caps and the wall-clock safety deadline.
/// Defaults are proposed and unbenchmarked (TD-009).
/// </summary>
public sealed class AnalysisLimitsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "AnalysisLimits";

    /// <summary>Gets or sets the maximum points CSV size in bytes.</summary>
    public long MaxPointsFileBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>Gets or sets the maximum manifest size in bytes.</summary>
    public long MaxManifestFileBytes { get; set; } = 1024 * 1024;

    /// <summary>Gets or sets the maximum number of points.</summary>
    public int MaxPoints { get; set; } = 5000;

    /// <summary>Gets or sets the maximum number of precedence edges.</summary>
    public int MaxPrecedenceEdges { get; set; } = 20000;

    /// <summary>Gets or sets the maximum absolute coordinate in millimetres.</summary>
    public double MaxCoordinateMagnitudeMm { get; set; } = 10000;

    /// <summary>Gets or sets the default pass budget when the manifest omits one.</summary>
    public long DefaultMaxPasses { get; set; } = 100;

    /// <summary>Gets or sets the highest pass budget a manifest may request.</summary>
    public long MaxPassesCap { get; set; } = 10000;

    /// <summary>Gets or sets the default move-evaluation budget.</summary>
    public long DefaultMaxMoveEvaluations { get; set; } = 200_000_000;

    /// <summary>Gets or sets the highest move-evaluation budget a manifest may request.</summary>
    public long MaxMoveEvaluationsCap { get; set; } = 2_000_000_000;

    /// <summary>Gets or sets the default minimum improvement in millimetres.</summary>
    public double DefaultMinImprovementMm { get; set; } = 0.001;

    /// <summary>Gets or sets the highest minimum improvement a manifest may request, in millimetres.</summary>
    public double MinImprovementCapMm { get; set; } = 1000;

    /// <summary>Gets or sets the wall-clock safety deadline; reaching it fails the run (ADR-0008).</summary>
    public TimeSpan Deadline { get; set; } = TimeSpan.FromSeconds(60);
}

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
