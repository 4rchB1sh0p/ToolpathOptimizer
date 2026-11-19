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

