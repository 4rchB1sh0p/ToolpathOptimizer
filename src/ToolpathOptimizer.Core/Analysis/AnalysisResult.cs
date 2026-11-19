using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Units;
using ToolpathOptimizer.Core.Verification;

namespace ToolpathOptimizer.Core.Analysis;

/// <summary>
/// Which route the analysis recommends.
/// </summary>
public enum RouteSelection
{
    /// <summary>The original input order.</summary>
    Baseline,

    /// <summary>The optimised, validated order.</summary>
    Candidate,
}

/// <summary>
/// A non-fatal analysis warning.
/// </summary>
public sealed class AnalysisWarning
{
    /// <summary>Creates a warning.</summary>
    /// <param name="code">Stable code from <see cref="AnalysisCodes"/>.</param>
    /// <param name="message">Explanation.</param>
    public AnalysisWarning(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>Gets the stable code.</summary>
    public string Code { get; }

    /// <summary>Gets the explanation.</summary>
    public string Message { get; }
}

/// <summary>
/// Immutable comparison of baseline and candidate routes with the selection decision.
/// </summary>
public sealed class AnalysisResult
{
    /// <summary>Creates a result.</summary>
    /// <param name="job">Analysed job.</param>
    /// <param name="baselineRoute">Baseline route.</param>
    /// <param name="candidateRoute">Candidate route.</param>
    /// <param name="baselineCheck">Validator report for the baseline.</param>
    /// <param name="candidateCheck">Validator report for the candidate.</param>
    /// <param name="selection">Selected route.</param>
    /// <param name="selectionReason">Stable reason token for the selection.</param>
    /// <param name="statistics">Search statistics.</param>
    /// <param name="warnings">Warnings in deterministic order.</param>
    public AnalysisResult(
        DrillJob job,
        Route baselineRoute,
        Route candidateRoute,
        FeasibilityReport baselineCheck,
        FeasibilityReport candidateCheck,
        RouteSelection selection,
        string selectionReason,
        SearchStatistics statistics,
        IReadOnlyList<AnalysisWarning> warnings)
    {
        Job = job;
        BaselineRoute = baselineRoute;
        CandidateRoute = candidateRoute;
        BaselineCheck = baselineCheck;
        CandidateCheck = candidateCheck;
        Selection = selection;
        SelectionReason = selectionReason;
        Statistics = statistics;
        Warnings = warnings.ToArray();
    }

    /// <summary>Gets the analysed job.</summary>
    public DrillJob Job { get; }

    /// <summary>Gets the baseline route.</summary>
    public Route BaselineRoute { get; }

    /// <summary>Gets the candidate route.</summary>
    public Route CandidateRoute { get; }

    /// <summary>Gets the baseline validator report.</summary>
    public FeasibilityReport BaselineCheck { get; }

    /// <summary>Gets the candidate validator report.</summary>
    public FeasibilityReport CandidateCheck { get; }

    /// <summary>Gets the selected route kind.</summary>
    public RouteSelection Selection { get; }

    /// <summary>Gets the stable selection reason token.</summary>
    public string SelectionReason { get; }

    /// <summary>Gets the search statistics.</summary>
    public SearchStatistics Statistics { get; }

    /// <summary>Gets the warnings.</summary>
    public IReadOnlyList<AnalysisWarning> Warnings { get; }

    /// <summary>Gets candidate minus baseline distance (negative means shorter).</summary>
    public Length Delta => CandidateCheck.Distance - BaselineCheck.Distance;

    /// <summary>Gets the delta as a percentage of the baseline, or <c>null</c> when the baseline distance is zero.</summary>
    public double? DeltaPercent => BaselineCheck.Distance == Length.Zero
        ? null
        : Delta.Metres / BaselineCheck.Distance.Metres * 100.0;

    /// <summary>Gets the selected route.</summary>
    public Route SelectedRoute => Selection == RouteSelection.Candidate ? CandidateRoute : BaselineRoute;
}
