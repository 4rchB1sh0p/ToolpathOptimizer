using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// Reads input files read-only, checking their size before reading the content.
/// </summary>
public interface IInputFileReader
{
    /// <summary>Reads a whole file within a size limit.</summary>
    /// <param name="path">File path.</param>
    /// <param name="maxBytes">Largest accepted file size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The bytes, or <c>IO_READ_FAILED</c> / <c>INPUT_LIMIT_EXCEEDED</c>.</returns>
    Task<Result<byte[]>> ReadAsync(string path, long maxBytes, CancellationToken cancellationToken);
}
