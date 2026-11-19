using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// Persists a report atomically (temp file in the target directory, flush, rename).
/// </summary>
public interface IReportWriter
{
    /// <summary>Writes report bytes to a path.</summary>
    /// <param name="path">Target path; its directory must exist.</param>
    /// <param name="content">Report bytes.</param>
    /// <param name="mode">Whether an existing file may be replaced.</param>
    /// <param name="cancellationToken">A cancelled write leaves no temp or partial file.</param>
    /// <returns>Success, or an I/O or cancellation outcome.</returns>
    Task<OperationOutcome> WriteAsync(string path, ReadOnlyMemory<byte> content, ReportWriteMode mode, CancellationToken cancellationToken);
}
