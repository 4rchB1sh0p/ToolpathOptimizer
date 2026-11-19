using System.Text.Json;

namespace ToolpathOptimizer.Cli.IntegrationTests.Support;

/// <summary>Captured result of one CLI child process.</summary>
internal sealed class CliRun
{
    /// <summary>Creates a captured run.</summary>
    /// <param name="exitCode">Process exit code.</param>
    /// <param name="stdout">Everything written to stdout.</param>
    /// <param name="stderr">Everything written to stderr.</param>
    public CliRun(int exitCode, string stdout, string stderr)
    {
        ExitCode = exitCode;
        Stdout = stdout;
        Stderr = stderr;
    }

    /// <summary>Gets the process exit code.</summary>
    public int ExitCode { get; }

    /// <summary>Gets everything written to stdout.</summary>
    public string Stdout { get; }

    /// <summary>Gets everything written to stderr.</summary>
    public string Stderr { get; }

    /// <summary>Gets the non-empty stdout lines.</summary>
    public string[] StdoutLines => Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Parses the single stdout line as JSON.</summary>
    /// <returns>The JSON envelope.</returns>
    public JsonElement ReadEnvelope() => JsonDocument.Parse(Assert.Single(StdoutLines)).RootElement.Clone();

    /// <summary>Reads the first error code from the envelope.</summary>
    /// <returns>The code.</returns>
    public string ReadFirstErrorCode() => ReadEnvelope().GetProperty("errors")[0].GetProperty("code").GetString()!;
}
