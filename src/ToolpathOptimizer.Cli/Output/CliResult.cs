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

    /// <summary>Gets the written report path; empty when none.</summary>
    public string ReportPath { get; }

    /// <summary>Gets the errors.</summary>
    public IReadOnlyList<OutcomeError> Errors { get; }

    /// <summary>Creates a success result.</summary>
    /// <param name="reportPath">Written report path, or empty.</param>
    /// <returns>The result.</returns>
    public static CliResult Success(string reportPath) => new(ExitCodes.Success, reportPath, []);

    /// <summary>Creates a result from a failure outcome.</summary>
    /// <param name="outcome">Failure outcome.</param>
    /// <returns>The result.</returns>
    public static CliResult Failure(OperationOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return new CliResult(ExitCodes.For(outcome.Category), string.Empty, outcome.Errors);
    }

    /// <summary>Writes the envelope as one line of JSON.</summary>
    /// <param name="writer">Target (stdout).</param>
    public void WriteTo(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            json.WriteStartObject();
            json.WriteString("schema_version", ContractVersions.EnvelopeSchemaVersion);
            json.WriteString("status", ExitCode == ExitCodes.Success ? "ok" : "error");
            json.WriteNumber("exit_code", ExitCode);
            if (ReportPath.Length > 0)
            {
                json.WriteString("report_path", ReportPath);
            }
            else
            {
                json.WriteNull("report_path");
            }

            json.WriteStartArray("errors");
            foreach (OutcomeError error in Errors)
            {
                json.WriteStartObject();
                json.WriteString("code", error.Code);
                json.WriteString("message", error.Message);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        writer.Write(System.Text.Encoding.UTF8.GetString(buffer.ToArray()));
        writer.Write('\n');
        writer.Flush();
    }
}

/// <summary>
/// Stable exit codes (ADR-0011).
/// </summary>
public static class ExitCodes
{
    /// <summary>Success.</summary>
    public const int Success = 0;

    /// <summary>Invalid arguments or configuration.</summary>
    public const int Arguments = 2;

    /// <summary>Invalid input.</summary>
    public const int Input = 3;

    /// <summary>Infeasible constraints.</summary>
    public const int Infeasible = 4;

    /// <summary>Processing failure.</summary>
    public const int Processing = 5;

    /// <summary>Cancelled.</summary>
    public const int Cancelled = 6;

    /// <summary>I/O failure.</summary>
    public const int Io = 7;

    /// <summary>Resource budget exceeded.</summary>
    public const int Budget = 8;

    /// <summary>Maps an outcome category to an exit code.</summary>
    /// <param name="category">Outcome category.</param>
    /// <returns>The exit code.</returns>
    public static int For(OutcomeCategory category) => category switch
    {
        OutcomeCategory.Success => Success,
        OutcomeCategory.Arguments => Arguments,
        OutcomeCategory.Input => Input,
        OutcomeCategory.Infeasible => Infeasible,
        OutcomeCategory.Cancelled => Cancelled,
        OutcomeCategory.Io => Io,
        OutcomeCategory.Budget => Budget,
        _ => Processing,
    };
}
