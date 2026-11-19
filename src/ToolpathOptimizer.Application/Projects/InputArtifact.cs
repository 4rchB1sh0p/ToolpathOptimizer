namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// An immutable, validated input upload (points CSV + manifest) stored under tenant-scoped keys.
/// </summary>
public sealed class InputArtifact
{
    /// <summary>Creates an input artifact.</summary>
    /// <param name="id">Server-assigned identifier.</param>
    /// <param name="projectId">Project the input belongs to.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="inputSha256">Canonical input hash, so identical uploads can be recognised.</param>
    /// <param name="pointCount">Number of drill points, used for quota checks.</param>
    /// <param name="uploadedBy">User who uploaded the input.</param>
    /// <param name="uploadedAtUtc">Upload time from the injected clock.</param>
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

    /// <summary>Gets the server-assigned identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the project the input belongs to.</summary>
    public Guid ProjectId { get; }

    /// <summary>Gets the owning tenant.</summary>
    public string TenantId { get; }

    /// <summary>Gets the canonical input hash.</summary>
    public string InputSha256 { get; }

    /// <summary>Gets the number of drill points.</summary>
    public int PointCount { get; }

    /// <summary>Gets the user who uploaded the input.</summary>
    public string UploadedBy { get; }

    /// <summary>Gets the upload time.</summary>
    public DateTimeOffset UploadedAtUtc { get; }

    /// <summary>Gets the artifact key of the points CSV.</summary>
    public string PointsKey => $"projects/{ProjectId:N}/inputs/{Id:N}/points.csv";

    /// <summary>Gets the artifact key of the manifest.</summary>
    public string ManifestKey => $"projects/{ProjectId:N}/inputs/{Id:N}/job.json";
}
