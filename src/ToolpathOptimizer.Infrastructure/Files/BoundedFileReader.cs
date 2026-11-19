using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Infrastructure.Files;

/// <summary>
/// Opens input files read-only and checks their size before reading the content.
/// </summary>
public sealed class BoundedFileReader : IInputFileReader
{
    /// <inheritdoc />
    public async Task<Result<byte[]>> ReadAsync(string path, long maxBytes, CancellationToken cancellationToken)
    {
        string name = Path.GetFileName(path);
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (stream.Length > maxBytes)
            {
                return Result<byte[]>.Failure(ErrorCodes.InputLimitExceeded, $"{name}: file exceeds {maxBytes} bytes.");
            }

            byte[] buffer = new byte[stream.Length];
            await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return Result<byte[]>.Success(buffer);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result<byte[]>.Failure(ErrorCodes.IoReadFailed, $"Could not read '{name}': {ex.GetType().Name}.");
        }
    }
}
