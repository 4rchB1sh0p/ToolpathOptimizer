using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Cli.CommandLine;

namespace ToolpathOptimizer.Cli.Commands;

/// <summary>
/// Reads the two input files through <see cref="IInputFileReader"/> with the configured size limits.
/// </summary>
public sealed class JobInputLoader
{
    private readonly IInputFileReader _reader;
    private readonly IOptions<AnalysisLimitsOptions> _limits;

    /// <summary>Creates the loader.</summary>
    /// <param name="reader">Read-only, size-checked file reader.</param>
    /// <param name="limits">Supplies the maximum file sizes.</param>
    public JobInputLoader(IInputFileReader reader, IOptions<AnalysisLimitsOptions> limits)
    {
        _reader = reader;
        _limits = limits;
    }

    /// <summary>Loads both input files; messages name files by file name only, never full paths.</summary>
    /// <param name="arguments">Parsed arguments with the input paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The job input, or the read failure.</returns>
    public async Task<Result<JobInput>> LoadAsync(CliArguments arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        Result<byte[]> points = await _reader.ReadAsync(arguments.PointsPath, _limits.Value.MaxPointsFileBytes, cancellationToken).ConfigureAwait(false);
        if (!points.IsSuccess)
        {
            return points.FailAs<JobInput>();
        }

        Result<byte[]> manifest = await _reader.ReadAsync(arguments.JobPath, _limits.Value.MaxManifestFileBytes, cancellationToken).ConfigureAwait(false);
        if (!manifest.IsSuccess)
        {
            return manifest.FailAs<JobInput>();
        }

        return Result<JobInput>.Success(new JobInput(points.Value, manifest.Value, Path.GetFileName(arguments.PointsPath), Path.GetFileName(arguments.JobPath)));
    }
}
