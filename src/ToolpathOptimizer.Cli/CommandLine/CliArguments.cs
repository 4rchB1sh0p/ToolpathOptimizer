using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Cli.CommandLine;

/// <summary>
/// A parsed, validated command line.
/// </summary>
public sealed class CliArguments
{
    /// <summary>Creates parsed arguments.</summary>
    /// <param name="command">Command to run.</param>
    /// <param name="pointsPath">Points CSV path; empty for commands without inputs.</param>
    /// <param name="jobPath">Manifest path; empty for commands without inputs.</param>
    /// <param name="outputPath">Report path; empty unless the command writes a report.</param>
    /// <param name="writeMode">Whether an existing report may be replaced (<c>--overwrite</c>).</param>
    public CliArguments(CliCommand command, string pointsPath, string jobPath, string outputPath, ReportWriteMode writeMode)
    {
        Command = command;
        PointsPath = pointsPath;
        JobPath = jobPath;
        OutputPath = outputPath;
        WriteMode = writeMode;
    }

    /// <summary>Gets the command to run.</summary>
    public CliCommand Command { get; }

    /// <summary>Gets the points CSV path.</summary>
    public string PointsPath { get; }

    /// <summary>Gets the manifest path.</summary>
    public string JobPath { get; }

    /// <summary>Gets the report path.</summary>
    public string OutputPath { get; }

    /// <summary>Gets whether an existing report may be replaced.</summary>
    public ReportWriteMode WriteMode { get; }
}
