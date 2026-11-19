using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Cli.CommandLine;
using ToolpathOptimizer.Cli.Output;

namespace ToolpathOptimizer.Cli.Commands;

/// <summary>
/// <c>validate</c>: checks inputs and constraints; writes no report file.
/// </summary>
public sealed class ValidateCommand
{
    private readonly JobInputLoader _loader;
    private readonly ValidateJobHandler _validateHandler;

    /// <summary>Creates the command.</summary>
    /// <param name="loader">Reads the input files.</param>
    /// <param name="validateHandler">Shared validation handler.</param>
    public ValidateCommand(JobInputLoader loader, ValidateJobHandler validateHandler)
    {
        _loader = loader;
        _validateHandler = validateHandler;
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

        Result<JobValidationSummary> validation = _validateHandler.Handle(input.Value);
        return validation.IsSuccess ? CliResult.Success() : CliResult.Failure(validation.Outcome);
    }
}
