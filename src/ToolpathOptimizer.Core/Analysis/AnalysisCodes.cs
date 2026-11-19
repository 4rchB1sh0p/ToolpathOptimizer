namespace ToolpathOptimizer.Core.Analysis;

/// <summary>
/// Stable error and warning codes produced by <see cref="AnalysisEngine"/>.
/// </summary>
public static class AnalysisCodes
{
    /// <summary>The independent validator rejected the candidate (optimiser defect).</summary>
    public const string OptCandidateRejected = "OPT_CANDIDATE_REJECTED";

    /// <summary>Warning: the baseline order violates constraints; the validated candidate was selected.</summary>
    public const string BaselineViolatesConstraints = "BASELINE_VIOLATES_CONSTRAINTS";

    /// <summary>Warning: the candidate did not improve enough; the baseline was selected.</summary>
    public const string NoImprovementBaselineSelected = "NO_IMPROVEMENT_BASELINE_SELECTED";

    /// <summary>Warning: local search stopped on a budget rather than converging.</summary>
    public const string SearchBudgetExhausted = "SEARCH_BUDGET_EXHAUSTED";

    /// <summary>Warning: the baseline distance is zero, so no percentage is reported.</summary>
    public const string ZeroBaselineDistance = "ZERO_BASELINE_DISTANCE";
}
