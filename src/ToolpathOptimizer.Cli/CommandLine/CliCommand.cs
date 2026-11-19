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
