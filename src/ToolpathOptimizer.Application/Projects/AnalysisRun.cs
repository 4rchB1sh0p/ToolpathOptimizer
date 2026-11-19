using ToolpathOptimizer.Application.Analysis;

namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// An immutable analysis run record. Create it with <see cref="Succeeded"/> or <see cref="Failed"/>.
/// </summary>
public sealed class AnalysisRun
{
    private readonly AnalysisSummary? _summary;

    private AnalysisRun(Guid id, RunOrigin origin, AnalysisSummary summary)
    {
        Id = id;
        Origin = origin;
        Status = RunStatus.Succeeded;
        OutcomeCode = string.Empty;
        _summary = summary;
    }

    private AnalysisRun(Guid id, RunOrigin origin, string outcomeCode)
    {
        Id = id;
        Origin = origin;
        Status = RunStatus.Failed;
        OutcomeCode = outcomeCode;
    }

    /// <summary>Gets the server-assigned identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets what the run was started for and by whom.</summary>
    public RunOrigin Origin { get; }

    /// <summary>Gets the project the run belongs to.</summary>
    public Guid ProjectId => Origin.ProjectId;

    /// <summary>Gets the owning tenant.</summary>
    public string TenantId => Origin.TenantId;

    /// <summary>Gets the analysed input.</summary>
    public Guid InputId => Origin.InputId;

    /// <summary>Gets the client idempotency key; empty when none was sent.</summary>
    public string IdempotencyKey => Origin.IdempotencyKey;

    /// <summary>Gets the terminal status.</summary>
    public RunStatus Status { get; }

    /// <summary>Gets the error code of a failed run; empty when succeeded.</summary>
    public string OutcomeCode { get; }

    /// <summary>Gets a value indicating whether a summary is available (succeeded runs only).</summary>
    public bool HasSummary => _summary is not null;

    /// <summary>Gets the summary of a succeeded run; throws for failed runs (check <see cref="HasSummary"/>).</summary>
    public AnalysisSummary Summary => _summary ?? throw new InvalidOperationException("A failed run has no summary.");

    /// <summary>Gets the artifact key of the report.</summary>
    public string ReportKey => $"projects/{ProjectId:N}/runs/{Id:N}/report.json";

    /// <summary>Creates a succeeded run.</summary>
    /// <param name="id">Server-assigned identifier.</param>
    /// <param name="origin">What the run was started for and by whom.</param>
    /// <param name="summary">Key figures of the produced report.</param>
    /// <returns>The run.</returns>
    public static AnalysisRun Succeeded(Guid id, RunOrigin origin, AnalysisSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new AnalysisRun(id, origin, summary);
    }

    /// <summary>Creates a failed run.</summary>
    /// <param name="id">Server-assigned identifier.</param>
    /// <param name="origin">What the run was started for and by whom.</param>
    /// <param name="outcomeCode">Stable code of the failure.</param>
    /// <returns>The run.</returns>
    public static AnalysisRun Failed(Guid id, RunOrigin origin, string outcomeCode) =>
        new(id, origin, outcomeCode);
}
