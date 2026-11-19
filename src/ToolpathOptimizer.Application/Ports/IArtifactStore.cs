using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// Stores immutable artifacts under tenant-scoped keys. Calls without a valid tenant are rejected.
/// </summary>
public interface IArtifactStore
{
    /// <summary>Stores bytes; an existing key is never overwritten.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="key">Relative key of safe, slash-separated segments.</param>
    /// <param name="content">Bytes to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when stored.</returns>
    Task PutAsync(string tenantId, string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);

    /// <summary>Loads bytes.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="key">Relative key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The bytes, or <c>RESOURCE_NOT_FOUND</c> when absent for this tenant.</returns>
    Task<Result<byte[]>> GetAsync(string tenantId, string key, CancellationToken cancellationToken);

    /// <summary>Deletes all artifacts under a key prefix for one tenant.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="prefix">Key prefix.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when deleted.</returns>
    Task DeletePrefixAsync(string tenantId, string prefix, CancellationToken cancellationToken);
}
