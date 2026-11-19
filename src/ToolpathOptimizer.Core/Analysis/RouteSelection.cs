namespace ToolpathOptimizer.Core.Analysis;

/// <summary>
/// Which route the analysis recommends.
/// </summary>
public enum RouteSelection
{
    /// <summary>The original input order.</summary>
    Baseline,

    /// <summary>The optimised, independently validated order.</summary>
    Candidate,
}
