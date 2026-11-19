namespace ToolpathOptimizer.Core.Analysis;

/// <summary>
/// A non-fatal analysis warning reported alongside a successful result.
/// </summary>
public sealed class AnalysisWarning
{
    /// <summary>Creates a warning.</summary>
    /// <param name="code">Stable code from <see cref="AnalysisCodes"/>.</param>
    /// <param name="message">Explanation for the reader of the report.</param>
    public AnalysisWarning(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>Gets the stable code from <see cref="AnalysisCodes"/>.</summary>
    public string Code { get; }

    /// <summary>Gets the explanation for the reader of the report.</summary>
    public string Message { get; }
}
