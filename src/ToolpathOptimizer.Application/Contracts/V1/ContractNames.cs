using ToolpathOptimizer.Application.Projects;
using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Optimisation;

namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// The single mapping from domain enums to their v1 contract strings.
/// </summary>
public static class ContractNames
{
    /// <summary>Contract name of <see cref="RouteSelection.Candidate"/>.</summary>
    public const string CandidateRoute = "candidate";

    /// <summary>Contract name of <see cref="RouteSelection.Baseline"/>.</summary>
    public const string BaselineRoute = "baseline";

    /// <summary>Maps a route selection to its contract name.</summary>
    /// <param name="selection">Selected route.</param>
    /// <returns><c>candidate</c> or <c>baseline</c>.</returns>
    public static string ForSelection(RouteSelection selection) =>
        selection == RouteSelection.Candidate ? CandidateRoute : BaselineRoute;

    /// <summary>Maps a search termination to its contract name.</summary>
    /// <param name="termination">Termination reason.</param>
    /// <returns><c>converged</c> or <c>budget_exhausted</c>.</returns>
    public static string ForTermination(SearchTermination termination) =>
        termination == SearchTermination.Converged ? "converged" : "budget_exhausted";

    /// <summary>Maps a run status to its API name.</summary>
    /// <param name="status">Run status.</param>
    /// <returns><c>succeeded</c> or <c>failed</c>.</returns>
    public static string ForRunStatus(RunStatus status) =>
        status == RunStatus.Succeeded ? "succeeded" : "failed";
}
