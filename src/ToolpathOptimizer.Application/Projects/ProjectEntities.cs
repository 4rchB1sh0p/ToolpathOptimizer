using ToolpathOptimizer.Application.Analysis;

namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// A tenant-owned project grouping inputs and analysis runs.
/// </summary>
public sealed class Project
{
    /// <summary>Creates a project.</summary>
    /// <param name="id">Project identifier.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="name">Display name.</param>
    /// <param name="ownerId">Creating user.</param>
    /// <param name="createdAtUtc">Creation time.</param>
    public Project(Guid id, string tenantId, string name, string ownerId, DateTimeOffset createdAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        OwnerId = ownerId;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Gets the identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the owning tenant.</summary>
    public string TenantId { get; }

    /// <summary>Gets the display name.</summary>
    public string Name { get; }

    /// <summary>Gets the creating user.</summary>
    public string OwnerId { get; }

    /// <summary>Gets the creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; }
}

/// <summary>
/// An immutable, validated input upload (points CSV + manifest).
/// </summary>
public sealed class InputArtifact
{
    /// <summary>Creates an input artifact.</summary>
    /// <param name="id">Input identifier.</param>
    /// <param name="projectId">Project.</param>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="inputSha256">Canonical input hash.</param>
    /// <param name="pointCount">Number of points.</param>
    /// <param name="uploadedBy">Uploading user.</param>
    /// <param name="uploadedAtUtc">Upload time.</param>
    public InputArtifact(Guid id, Guid projectId, string tenantId, string inputSha256, int pointCount, string uploadedBy, DateTimeOffset uploadedAtUtc)
    {
        Id = id;
        ProjectId = projectId;
        TenantId = tenantId;
        InputSha256 = inputSha256;
        PointCount = pointCount;
        UploadedBy = uploadedBy;
        UploadedAtUtc = uploadedAtUtc;
    }

    /// <summary>Gets the identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the project.</summary>
    public Guid ProjectId { get; }

    /// <summary>Gets the tenant.</summary>
    public string TenantId { get; }

    /// <summary>Gets the canonical input hash.</summary>
    public string InputSha256 { get; }

    /// <summary>Gets the number of points.</summary>
    public int PointCount { get; }

    /// <summary>Gets the uploading user.</summary>
    public string UploadedBy { get; }

    /// <summary>Gets the upload time.</summary>
    public DateTimeOffset UploadedAtUtc { get; }

    /// <summary>Gets the artifact key of the points CSV.</summary>
    public string PointsKey => $"projects/{ProjectId:N}/inputs/{Id:N}/points.csv";

    /// <summary>Gets the artifact key of the manifest.</summary>
    public string ManifestKey => $"projects/{ProjectId:N}/inputs/{Id:N}/job.json";
}

/// <summary>
/// Terminal state of an analysis run (runs are synchronous in the MVP).
/// </summary>
public enum RunStatus
{
    /// <summary>A report was produced.</summary>
    Succeeded,

    /// <summary>The run failed; see the outcome code.</summary>
    Failed,
}

/// <summary>
/// An immutable analysis run record.
/// </summary>
public sealed class AnalysisRun
{
    /// <summary>Creates a run record.</summary>
    /// <param name="id">Run identifier.</param>
    /// <param name="projectId">Project.</param>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="inputId">Analysed input.</param>
    /// <param name="idempotencyKey">Idempotency key; empty when none.</param>
    /// <param name="status">Terminal status.</param>
    /// <param name="outcomeCode">Error code when failed; empty when succeeded.</param>
    /// <param name="summary">Summary when succeeded.</param>
    /// <param name="initiatedBy">Initiating user.</param>
    /// <param name="createdAtUtc">Creation time.</param>
    public AnalysisRun(Guid id, Guid projectId, string tenantId, Guid inputId, string idempotencyKey, RunStatus status, string outcomeCode, AnalysisSummary? summary, string initiatedBy, DateTimeOffset createdAtUtc)
    {
        Id = id;
        ProjectId = projectId;
        TenantId = tenantId;
        InputId = inputId;
        IdempotencyKey = idempotencyKey;
        Status = status;
        OutcomeCode = outcomeCode;
        Summary = summary;
        InitiatedBy = initiatedBy;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Gets the identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the project.</summary>
    public Guid ProjectId { get; }

    /// <summary>Gets the tenant.</summary>
    public string TenantId { get; }

    /// <summary>Gets the analysed input.</summary>
    public Guid InputId { get; }

    /// <summary>Gets the idempotency key; empty when none.</summary>
    public string IdempotencyKey { get; }

    /// <summary>Gets the terminal status.</summary>
    public RunStatus Status { get; }

    /// <summary>Gets the error code when failed.</summary>
    public string OutcomeCode { get; }

    /// <summary>Gets the summary when succeeded; otherwise <c>null</c>.</summary>
    public AnalysisSummary? Summary { get; }

    /// <summary>Gets the initiating user.</summary>
    public string InitiatedBy { get; }

    /// <summary>Gets the creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Gets the artifact key of the report.</summary>
    public string ReportKey => $"projects/{ProjectId:N}/runs/{Id:N}/report.json";
}
