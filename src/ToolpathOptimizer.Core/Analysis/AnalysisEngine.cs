using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Units;
using ToolpathOptimizer.Core.Validation;
using ToolpathOptimizer.Core.Verification;

namespace ToolpathOptimizer.Core.Analysis;

/// <summary>
/// Orchestrates validate → evaluate baseline → optimise → verify candidate → select.
/// </summary>
public static class AnalysisEngine
{
    private const double RelativeDistanceTolerance = 1e-9;
    private const double AbsoluteDistanceToleranceMetres = 1e-12;

    /// <summary>Selection reason: the candidate is shorter than the feasible baseline.</summary>
    public const string ReasonCandidateShorter = "candidate_shorter";

    /// <summary>Selection reason: the candidate did not improve by the minimum amount.</summary>
    public const string ReasonNoImprovement = "no_improvement_baseline_retained";

    /// <summary>Selection reason: the baseline violates constraints.</summary>
    public const string ReasonBaselineInfeasible = "baseline_infeasible";

    /// <summary>Runs a full analysis.</summary>
    /// <param name="job">Job to analyse.</param>
    /// <param name="options">Effective parameters.</param>
    /// <param name="cancellationToken">Cancellation token propagated into search.</param>
    /// <returns>The analysis outcome.</returns>
    public static AnalysisOutcome Analyse(DrillJob job, AnalysisOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        ValidationResult validation = JobValidator.Validate(job);
        if (!validation.IsValid)
        {
            return AnalysisOutcome.Failure(validation.Issues);
        }

        Route baseline = job.CreateBaselineRoute();
        FeasibilityReport baselineCheck = FeasibilityValidator.Validate(job, baseline);
        OptimisationResult optimised = RouteOptimiser.Optimise(validation.Model, options.Budget, cancellationToken);
        FeasibilityReport candidateCheck = FeasibilityValidator.Validate(job, optimised.Route);

        if (!candidateCheck.IsFeasible || !DistancesAgree(optimised.RunningDistanceMetres, candidateCheck.Distance.Metres))
        {
            return AnalysisOutcome.Failure([new ValidationIssue(
                AnalysisCodes.OptCandidateRejected,
                "The independent validator rejected the optimiser's candidate route.")]);
        }

        return AnalysisOutcome.Success(BuildResult(job, baseline, baselineCheck, optimised, candidateCheck, options.MinImprovement));
    }

    private static AnalysisResult BuildResult(
        DrillJob job,
        Route baseline,
        FeasibilityReport baselineCheck,
        OptimisationResult optimised,
        FeasibilityReport candidateCheck,
        Length minImprovement)
    {
        var warnings = new List<AnalysisWarning>();
        RouteSelection selection;
        string reason;
        if (!baselineCheck.IsFeasible)
        {
            selection = RouteSelection.Candidate;
            reason = ReasonBaselineInfeasible;
            warnings.Add(new AnalysisWarning(AnalysisCodes.BaselineViolatesConstraints, "The baseline order violates constraints; the validated candidate is selected."));
        }
        else if (candidateCheck.Distance.Metres >= baselineCheck.Distance.Metres - minImprovement.Metres)
        {
            selection = RouteSelection.Baseline;
            reason = ReasonNoImprovement;
            warnings.Add(new AnalysisWarning(AnalysisCodes.NoImprovementBaselineSelected, "The candidate does not shorten travel by the minimum improvement; the baseline is retained."));
        }
        else
        {
            selection = RouteSelection.Candidate;
            reason = ReasonCandidateShorter;
        }

        if (optimised.Statistics.Termination == SearchTermination.BudgetExhausted)
        {
            warnings.Add(new AnalysisWarning(AnalysisCodes.SearchBudgetExhausted, "Local search stopped on a budget before converging."));
        }

        if (baselineCheck.Distance == Length.Zero)
        {
            warnings.Add(new AnalysisWarning(AnalysisCodes.ZeroBaselineDistance, "The baseline distance is zero; no percentage change is reported."));
        }

        return new AnalysisResult(job, baseline, optimised.Route, baselineCheck, candidateCheck, selection, reason, optimised.Statistics, warnings);
    }

    private static bool DistancesAgree(double runningMetres, double validatedMetres) =>
        Math.Abs(runningMetres - validatedMetres) <= (RelativeDistanceTolerance * validatedMetres) + AbsoluteDistanceToleranceMetres;
}
