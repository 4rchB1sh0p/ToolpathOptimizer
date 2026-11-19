using ToolpathOptimizer.Core.Validation;

namespace ToolpathOptimizer.Core.Analysis;

/// <summary>
/// Either a successful <see cref="AnalysisResult"/> or the issues that prevented one.
/// </summary>
public sealed class AnalysisOutcome
{
    private readonly AnalysisResult? _result;

    private AnalysisOutcome(AnalysisResult? result, IReadOnlyList<ValidationIssue> issues)
    {
        _result = result;
        Issues = issues;
    }

    /// <summary>Gets a value indicating whether a result is available.</summary>
    public bool IsSuccess => _result is not null;

    /// <summary>Gets the failure issues; empty on success.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; }

    /// <summary>Gets the result; only available when <see cref="IsSuccess"/>.</summary>
    public AnalysisResult Result => _result ?? throw new InvalidOperationException("Analysis failed; no result available.");

    internal static AnalysisOutcome Success(AnalysisResult result) => new(result, []);

    internal static AnalysisOutcome Failure(IReadOnlyList<ValidationIssue> issues) => new(null, issues.ToArray());
}
