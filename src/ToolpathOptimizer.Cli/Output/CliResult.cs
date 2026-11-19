using System.Text.Json;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Cli.Output;

/// <summary>
/// The stdout result envelope (<c>schema_version</c>, <c>status</c>, <c>exit_code</c>, <c>report_path</c>, <c>errors[]</c>).
/// </summary>
public sealed class CliResult
{
    private CliResult(int exitCode, string reportPath, IReadOnlyList<OutcomeError> errors)
    {
        ExitCode = exitCode;
        ReportPath = reportPath;
        Errors = errors;
    }

    /// <summary>Gets the process exit code.</summary>
    public int ExitCode { get; }

    /// <summary>Gets the full path of the written report; empty when no report was written.</summary>
    public string ReportPath { get; }

    /// <summary>Gets the errors; empty on success.</summary>
    public IReadOnlyList<OutcomeError> Errors { get; }

    /// <summary>Creates a success result for a command that writes no report.</summary>
    /// <returns>The result.</returns>
    public static CliResult Success() => new(ExitCodes.Success, string.Empty, []);

    /// <summary>Creates a success result for a written report.</summary>
    /// <param name="reportPath">Full path of the written report.</param>
    /// <returns>The result.</returns>
    public static CliResult ReportWritten(string reportPath) => new(ExitCodes.Success, reportPath, []);

    /// <summary>Creates a result from a failure outcome.</summary>
    /// <param name="outcome">Failure outcome.</param>
    /// <returns>The result.</returns>
    public static CliResult Failure(OperationOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return new CliResult(ExitCodes.FromCategory(outcome.Category), string.Empty, outcome.Errors);
    }

    /// <summary>Writes the envelope as one line of JSON.</summary>
    /// <param name="target">Destination, normally stdout.</param>
    public void WriteTo(TextWriter target) => JsonLineWriter.WriteLine(target, WriteEnvelope);

    private void WriteEnvelope(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("schema_version", ContractVersions.EnvelopeSchemaVersion);
        writer.WriteString("status", ExitCode == ExitCodes.Success ? "ok" : "error");
        writer.WriteNumber("exit_code", ExitCode);
        if (ReportPath.Length > 0)
        {
            writer.WriteString("report_path", ReportPath);
        }
        else
        {
            writer.WriteNull("report_path");
        }

        ContractJson.WriteCodedMessages(writer, "errors", Errors, error => error.Code, error => error.Message);
        writer.WriteEndObject();
    }
}
