namespace ToolpathOptimizer.Application.Analysis;

/// <summary>
/// A serialised report with its summary.
/// </summary>
public sealed class AnalysisReport
{
    /// <summary>Creates a report.</summary>
    /// <param name="content">Report v1 bytes.</param>
    /// <param name="summary">Key figures.</param>
    public AnalysisReport(byte[] content, AnalysisSummary summary)
    {
        Content = content;
        Summary = summary;
    }

    /// <summary>Gets the report bytes.</summary>
    public byte[] Content { get; }

    /// <summary>Gets the key figures.</summary>
    public AnalysisSummary Summary { get; }
}

