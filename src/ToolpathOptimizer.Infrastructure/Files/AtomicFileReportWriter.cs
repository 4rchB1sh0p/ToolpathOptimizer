using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Infrastructure.Files;

/// <summary>
/// Writes a temp file in the target directory, flushes it to disk, then renames it over the target.
/// Requires a local file system for atomic rename semantics.
/// </summary>
public sealed class AtomicFileReportWriter : IReportWriter
{
    /// <inheritdoc />
    public async Task<OperationOutcome> WriteAsync(string path, ReadOnlyMemory<byte> content, ReportWriteMode mode, CancellationToken cancellationToken)
    {
        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (directory is null || !Directory.Exists(directory))
        {
            return OperationOutcome.Failure(ErrorCodes.IoWriteFailed, $"Output directory for '{Path.GetFileName(fullPath)}' does not exist.");
        }

        if (mode == ReportWriteMode.CreateNew && File.Exists(fullPath))
        {
            return OperationOutcome.Failure(ErrorCodes.IoOutputExists, $"Output '{Path.GetFileName(fullPath)}' exists; pass --overwrite to replace it.");
        }

        string tempPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(tempPath, fullPath, overwrite: mode == ReportWriteMode.Overwrite);
            return OperationOutcome.Success;
        }
        catch (OperationCanceledException)
        {
            return OperationOutcome.Failure(ErrorCodes.Cancelled, "Writing the report was cancelled.");
        }
        catch (IOException) when (mode == ReportWriteMode.CreateNew && File.Exists(fullPath))
        {
            return OperationOutcome.Failure(ErrorCodes.IoOutputExists, $"Output '{Path.GetFileName(fullPath)}' exists; pass --overwrite to replace it.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return OperationOutcome.Failure(ErrorCodes.IoWriteFailed, $"Could not write '{Path.GetFileName(fullPath)}': {ex.GetType().Name}.");
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best effort: the temp name is unique and hidden.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort.
        }
    }
}
