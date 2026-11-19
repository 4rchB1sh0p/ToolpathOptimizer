namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// A tenant-owned container for immutable inputs and analysis runs.
/// </summary>
public sealed class Project
{
    /// <summary>Creates a project.</summary>
    /// <param name="id">Server-assigned identifier.</param>
    /// <param name="tenantId">Owning tenant, from the caller's credential.</param>
    /// <param name="name">Display name (1–100 printable characters).</param>
    /// <param name="ownerId">User who created the project.</param>
    /// <param name="createdAtUtc">Creation time from the injected clock.</param>
    public Project(Guid id, string tenantId, string name, string ownerId, DateTimeOffset createdAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        OwnerId = ownerId;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Gets the server-assigned identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the owning tenant; every lookup filters on it.</summary>
    public string TenantId { get; }

    /// <summary>Gets the display name shown in the UI.</summary>
    public string Name { get; }

    /// <summary>Gets the user who created the project.</summary>
    public string OwnerId { get; }

    /// <summary>Gets the creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; }
}
