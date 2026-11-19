using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Web.Api;

/// <summary>Analysis run as returned by the API. Optional members serialise as JSON <c>null</c>.</summary>
public sealed class RunResponse
{
    /// <summary>Gets the run identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the owning project.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>Gets the analysed input.</summary>
    public Guid InputId { get; init; }

    /// <summary>Gets the status, <c>succeeded</c> or <c>failed</c>.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Gets the failure code of a failed run; JSON <c>null</c> when succeeded.</summary>
    public string? OutcomeCode { get; init; }

    /// <summary>Gets the key figures of a succeeded run; JSON <c>null</c> when failed.</summary>
    public AnalysisSummary? Summary { get; init; }

    /// <summary>Gets the report URL of a succeeded run; JSON <c>null</c> when failed.</summary>
    public string? ReportUrl { get; init; }

    /// <summary>Gets the creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Maps a run.</summary>
    /// <param name="run">Domain run.</param>
    /// <returns>The response.</returns>
    public static RunResponse From(AnalysisRun run)
    {
        bool isSucceeded = run.Status == RunStatus.Succeeded;
        return new RunResponse
        {
            Id = run.Id,
            ProjectId = run.ProjectId,
            InputId = run.InputId,
            Status = ContractNames.ForRunStatus(run.Status),
            OutcomeCode = isSucceeded ? null : run.OutcomeCode,
            Summary = run.HasSummary ? run.Summary : null,
            ReportUrl = isSucceeded ? $"/api/v1/projects/{run.ProjectId}/analyses/{run.Id}/report" : null,
            CreatedAtUtc = run.Origin.CreatedAtUtc,
        };
    }
}
