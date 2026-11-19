using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Cli.CommandLine;
using ToolpathOptimizer.Cli.Output;

namespace ToolpathOptimizer.Cli.Commands;

/// <summary>
/// Reads the two input files through <see cref="IInputFileReader"/> with configured size limits.
/// </summary>
public sealed class JobInputLoader
{
    private readonly IInputFileReader _reader;
    private readonly IOptions<AnalysisLimitsOptions> _limits;

    /// <summary>Creates the loader.</summary>
    /// <param name="reader">File reader port.</param>
    /// <param name="limits">Configured limits.</param>
    public JobInputLoader(IInputFileReader reader, IOptions<AnalysisLimitsOptions> limits)
    {
        _reader = reader;
        _limits = limits;
    }

    /// <summary>Loads both input files.</summary>
    /// <param name="arguments">Parsed arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The job input, or the read failure.</returns>
    public async Task<Result<JobInput>> LoadAsync(CliArguments arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        Result<byte[]> points = await _reader.ReadAsync(arguments.PointsPath, _limits.Value.MaxPointsFileBytes, cancellationToken).ConfigureAwait(false);
        if (!points.IsSuccess)
        {
            return Result<JobInput>.Failure(points.Outcome);
        }

        Result<byte[]> manifest = await _reader.ReadAsync(arguments.JobPath, _limits.Value.MaxManifestFileBytes, cancellationToken).ConfigureAwait(false);
        if (!manifest.IsSuccess)
        {
            return Result<JobInput>.Failure(manifest.Outcome);
        }

        return Result<JobInput>.Success(new JobInput(points.Value, manifest.Value, Path.GetFileName(arguments.PointsPath), Path.GetFileName(arguments.JobPath)));
    }
}

/// <summary>
/// <c>validate</c>: validation only, no report file.
/// </summary>
public sealed class ValidateCommand
{
    private readonly JobInputLoader _loader;
    private readonly ValidateJobHandler _handler;

    /// <summary>Creates the command.</summary>
    /// <param name="loader">Input loader.</param>
    /// <param name="handler">Validation handler.</param>
    public ValidateCommand(JobInputLoader loader, ValidateJobHandler handler)
    {
        _loader = loader;
        _handler = handler;
    }

    /// <summary>Executes the command.</summary>
    /// <param name="arguments">Parsed arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CLI result.</returns>
    public async Task<CliResult> ExecuteAsync(CliArguments arguments, CancellationToken cancellationToken)
    {
        Result<JobInput> input = await _loader.LoadAsync(arguments, cancellationToken).ConfigureAwait(false);
        if (!input.IsSuccess)
        {
            return CliResult.Failure(input.Outcome);
        }

        Result<JobValidationSummary> validation = _handler.Handle(input.Value);
        return validation.IsSuccess ? CliResult.Success(string.Empty) : CliResult.Failure(validation.Outcome);
    }
}

/// <summary>
/// <c>analyze</c>: validate, optimise, verify and write the report atomically.
/// </summary>
public sealed class AnalyzeCommand
{
    private readonly JobInputLoader _loader;
    private readonly AnalyzeJobHandler _handler;
    private readonly IReportWriter _writer;
    private readonly IFileAliasDetector _aliases;

    /// <summary>Creates the command.</summary>
    /// <param name="loader">Input loader.</param>
    /// <param name="handler">Analysis handler.</param>
    /// <param name="writer">Atomic report writer.</param>
    /// <param name="aliases">Path alias detector.</param>
    public AnalyzeCommand(JobInputLoader loader, AnalyzeJobHandler handler, IReportWriter writer, IFileAliasDetector aliases)
    {
        _loader = loader;
        _handler = handler;
        _writer = writer;
        _aliases = aliases;
    }

    /// <summary>Executes the command.</summary>
    /// <param name="arguments">Parsed arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CLI result.</returns>
    public async Task<CliResult> ExecuteAsync(CliArguments arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        string output = Path.GetFullPath(arguments.OutputPath);
        if (_aliases.AreSameFile(output, arguments.PointsPath) || _aliases.AreSameFile(output, arguments.JobPath))
        {
            return CliResult.Failure(OperationOutcome.Failure(ErrorCodes.ArgsOutputIsInput, "The output path resolves to an input file; inputs are never replaced."));
        }

        Result<JobInput> input = await _loader.LoadAsync(arguments, cancellationToken).ConfigureAwait(false);
        if (!input.IsSuccess)
        {
            return CliResult.Failure(input.Outcome);
        }

        Result<AnalysisReport> report = _handler.Handle(input.Value, cancellationToken);
        if (!report.IsSuccess)
        {
            return CliResult.Failure(report.Outcome);
        }

        ReportWriteMode mode = arguments.Overwrite ? ReportWriteMode.Overwrite : ReportWriteMode.CreateNew;
        OperationOutcome written = await _writer.WriteAsync(output, report.Value.Content, mode, cancellationToken).ConfigureAwait(false);
        return written.IsSuccess ? CliResult.Success(output) : CliResult.Failure(written);
    }
}
