using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// Whether an existing report file may be replaced.
/// </summary>
public enum ReportWriteMode
{
    /// <summary>Fail with <c>IO_OUTPUT_EXISTS</c> when the file exists.</summary>
    CreateNew,

    /// <summary>Atomically replace an existing file.</summary>
    Overwrite,
}

/// <summary>
/// Persists a report atomically (temp file in the target directory, flush, rename).
/// </summary>
public interface IReportWriter
{
    /// <summary>Writes report bytes to a path.</summary>
    /// <param name="path">Target path; its directory must exist.</param>
    /// <param name="content">Report bytes.</param>
    /// <param name="mode">Overwrite policy.</param>
    /// <param name="cancellationToken">Cancellation; a cancelled write leaves no temp or partial file.</param>
    /// <returns>Success, or an I/O or cancellation outcome.</returns>
    Task<OperationOutcome> WriteAsync(string path, ReadOnlyMemory<byte> content, ReportWriteMode mode, CancellationToken cancellationToken);
}

/// <summary>
/// Reads input files read-only, checking size before reading the content.
/// </summary>
public interface IInputFileReader
{
    /// <summary>Reads a whole file within a size limit.</summary>
    /// <param name="path">File path.</param>
    /// <param name="maxBytes">Maximum allowed size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The bytes, or <c>IO_READ_FAILED</c> / <c>INPUT_LIMIT_EXCEEDED</c>.</returns>
    Task<Result<byte[]>> ReadAsync(string path, long maxBytes, CancellationToken cancellationToken);
}

/// <summary>
/// Detects whether two paths refer to the same file (full path, links and file identity).
/// </summary>
public interface IFileAliasDetector
{
    /// <summary>Determines whether two paths alias the same file.</summary>
    /// <param name="firstPath">First path.</param>
    /// <param name="secondPath">Second path.</param>
    /// <returns><c>true</c> when both resolve to the same file.</returns>
    bool AreSameFile(string firstPath, string secondPath);
}

/// <summary>
/// Stores immutable artifacts under tenant-scoped keys.
/// </summary>
public interface IArtifactStore
{
    /// <summary>Stores bytes; an existing key is never overwritten.</summary>
    /// <param name="tenantId">Owning tenant (required).</param>
    /// <param name="key">Relative key of safe segments.</param>
    /// <param name="content">Bytes to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when stored.</returns>
    Task PutAsync(string tenantId, string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);

    /// <summary>Loads bytes.</summary>
    /// <param name="tenantId">Owning tenant (required).</param>
    /// <param name="key">Relative key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The bytes, or <c>null</c> when absent for this tenant.</returns>
    Task<byte[]?> GetAsync(string tenantId, string key, CancellationToken cancellationToken);

    /// <summary>Deletes all artifacts under a key prefix for one tenant.</summary>
    /// <param name="tenantId">Owning tenant (required).</param>
    /// <param name="prefix">Key prefix.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when deleted.</returns>
    Task DeletePrefixAsync(string tenantId, string prefix, CancellationToken cancellationToken);
}
