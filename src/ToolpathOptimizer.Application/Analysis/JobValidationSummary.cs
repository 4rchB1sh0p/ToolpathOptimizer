namespace ToolpathOptimizer.Application.Analysis;

/// <summary>
/// Counts describing a valid job.
/// </summary>
public sealed class JobValidationSummary
{
    /// <summary>Creates a summary.</summary>
    /// <param name="inputSha256">Canonical input hash.</param>
    /// <param name="pointCount">Number of points.</param>
    /// <param name="precedenceEdgeCount">Number of precedence edges.</param>
    /// <param name="lockedGroupCount">Number of locked groups.</param>
    public JobValidationSummary(string inputSha256, int pointCount, int precedenceEdgeCount, int lockedGroupCount)
    {
        InputSha256 = inputSha256;
        PointCount = pointCount;
        PrecedenceEdgeCount = precedenceEdgeCount;
        LockedGroupCount = lockedGroupCount;
    }

    /// <summary>Gets the canonical input hash.</summary>
    public string InputSha256 { get; }

    /// <summary>Gets the number of points.</summary>
    public int PointCount { get; }

    /// <summary>Gets the number of precedence edges.</summary>
    public int PrecedenceEdgeCount { get; }

    /// <summary>Gets the number of locked groups.</summary>
    public int LockedGroupCount { get; }
}

