using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Cli.CommandLine;
using ToolpathOptimizer.Cli.Output;

namespace ToolpathOptimizer.Cli.Commands;

/// <summary>
/// <c>analyze</c>: validate, optimise, verify and write the report atomically. Never replaces an input file.
/// </summary>
public sealed class AnalyzeCommand
{
    private readonly JobInputLoader _loader;
    private readonly AnalyzeJobHandler _analyzeHandler;
    private readonly IReportWriter _reportWriter;
    private readonly IFileAliasDetector _aliasDetector;

    /// <summary>Creates the command.</summary>
    /// <param name="loader">Reads the input files.</param>
    /// <param name="analyzeHandler">Shared analysis handler (same as the web).</param>
    /// <param name="reportWriter">Atomic report writer.</param>
    /// <param name="aliasDetector">Detects an output path that resolves to an input file.</param>
    public AnalyzeCommand(JobInputLoader loader, AnalyzeJobHandler analyzeHandler, IReportWriter reportWriter, IFileAliasDetector aliasDetector)
    {
        _loader = loader;
        _analyzeHandler = analyzeHandler;
        _reportWriter = reportWriter;
        _aliasDetector = aliasDetector;
    }

    /// <summary>Executes the command.</summary>
    /// <param name="arguments">Parsed arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CLI result.</returns>
    public async Task<CliResult> ExecuteAsync(CliArguments arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        string outputPath = Path.GetFullPath(arguments.OutputPath);
        if (_aliasDetector.AreSameFile(outputPath, arguments.PointsPath) || _aliasDetector.AreSameFile(outputPath, arguments.JobPath))
        {
            return CliResult.Failure(OperationOutcome.Failure(ErrorCodes.ArgsOutputIsInput, "The output path resolves to an input file; inputs are never replaced."));
        }

        Result<JobInput> input = await _loader.LoadAsync(arguments, cancellationToken).ConfigureAwait(false);
        if (!input.IsSuccess)
        {
            return CliResult.Failure(input.Outcome);
        }

        Result<AnalysisReport> report = _analyzeHandler.Handle(input.Value, cancellationToken);
        if (!report.IsSuccess)
        {
            return CliResult.Failure(report.Outcome);
        }

        OperationOutcome written = await _reportWriter.WriteAsync(outputPath, report.Value.Content, arguments.WriteMode, cancellationToken).ConfigureAwait(false);
        return written.IsSuccess ? CliResult.ReportWritten(outputPath) : CliResult.Failure(written);
    }
}
