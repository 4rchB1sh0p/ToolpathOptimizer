namespace ToolpathOptimizer.Cli.CommandLine;

/// <summary>
/// Supported commands.
/// </summary>
public enum CliCommand
{
    /// <summary>Print usage.</summary>
    Help,

    /// <summary>Print versions as JSON.</summary>
    Version,

    /// <summary>Validate inputs only.</summary>
    Validate,

    /// <summary>Validate, optimise, verify and write a report.</summary>
    Analyze,
}

/// <summary>
/// Parsed command line.
/// </summary>
public sealed class CliArguments
{
    /// <summary>Creates parsed arguments.</summary>
    /// <param name="command">Command.</param>
    /// <param name="pointsPath">Points CSV path; empty when not applicable.</param>
    /// <param name="jobPath">Manifest path; empty when not applicable.</param>
    /// <param name="outputPath">Report path; empty when not applicable.</param>
    /// <param name="overwrite">Whether an existing report may be replaced.</param>
    public CliArguments(CliCommand command, string pointsPath, string jobPath, string outputPath, bool overwrite)
    {
        Command = command;
        PointsPath = pointsPath;
        JobPath = jobPath;
        OutputPath = outputPath;
        Overwrite = overwrite;
    }

    /// <summary>Gets the command.</summary>
    public CliCommand Command { get; }

    /// <summary>Gets the points CSV path.</summary>
    public string PointsPath { get; }

    /// <summary>Gets the manifest path.</summary>
    public string JobPath { get; }

    /// <summary>Gets the report path.</summary>
    public string OutputPath { get; }

    /// <summary>Gets a value indicating whether an existing report may be replaced.</summary>
    public bool Overwrite { get; }
}
