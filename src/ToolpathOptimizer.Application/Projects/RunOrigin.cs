namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// What a run was started for and by whom; shared by successful and failed runs.
/// </summary>
public sealed class RunOrigin
{
    /// <summary>Creates a run origin.</summary>
    /// <param name="input">Input that was analysed; supplies project and tenant.</param>
    /// <param name="idempotencyKey">Client idempotency key; empty when none was sent.</param>
    /// <param name="initiatedBy">User who started the run.</param>
    /// <param name="createdAtUtc">Start time from the injected clock.</param>
    public RunOrigin(InputArtifact input, string idempotencyKey, string initiatedBy, DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(input);
        ProjectId = input.ProjectId;
        TenantId = input.TenantId;
        InputId = input.Id;
        IdempotencyKey = idempotencyKey;
        InitiatedBy = initiatedBy;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Gets the project the run belongs to.</summary>
    public Guid ProjectId { get; }

    /// <summary>Gets the owning tenant.</summary>
    public string TenantId { get; }

    /// <summary>Gets the analysed input.</summary>
    public Guid InputId { get; }

    /// <summary>Gets the client idempotency key; empty when none was sent.</summary>
    public string IdempotencyKey { get; }

    /// <summary>Gets the user who started the run.</summary>
    public string InitiatedBy { get; }

    /// <summary>Gets the start time.</summary>
    public DateTimeOffset CreatedAtUtc { get; }
}
