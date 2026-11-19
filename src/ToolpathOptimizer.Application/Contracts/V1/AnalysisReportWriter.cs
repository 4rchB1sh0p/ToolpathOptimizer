using System.Globalization;
using System.Text.Json;
using ToolpathOptimizer.Application.Provenance;
using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Verification;

namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Serialises report v1 deterministically: fixed field order, shortest round-trip numbers, LF newlines.
/// </summary>
public static class AnalysisReportWriter
{
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    /// <summary>Serialises a report.</summary>
    /// <param name="canonicalJob">Canonical job embedded so distances can be recomputed from the report alone.</param>
    /// <param name="provenance">Hashes, tool and algorithm identity, and generation time.</param>
    /// <param name="result">Analysis result to report.</param>
    /// <returns>UTF-8 JSON bytes ending in a newline.</returns>
    public static byte[] Serialize(CanonicalJob canonicalJob, ReportProvenance provenance, AnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(canonicalJob);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(result);
        byte[] json = ContractJson.ToUtf8Bytes(writer => WriteReport(writer, canonicalJob, provenance, result), ContractJson.Indented);
        return [.. json, (byte)'\n'];
    }

    private static void WriteReport(Utf8JsonWriter writer, CanonicalJob canonicalJob, ReportProvenance provenance, AnalysisResult result)
    {
        writer.WriteStartObject();
        WriteHeader(writer, canonicalJob, provenance);
        writer.WritePropertyName("canonical_job");
        canonicalJob.WriteTo(writer);
        WriteComparison(writer, result);
        WriteConstraintChecks(writer, result);
        WriteSearch(writer, result.Statistics);
        ContractJson.WriteCodedMessages(writer, "warnings", result.Warnings, warning => warning.Code, warning => warning.Message);
        writer.WriteString("generated_at_utc", provenance.GeneratedAtUtc.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture));
        writer.WriteEndObject();
    }

    private static void WriteHeader(Utf8JsonWriter writer, CanonicalJob job, ReportProvenance provenance)
    {
        writer.WriteString("schema_version", ContractVersions.ReportSchemaVersion);
        writer.WriteString("report_kind", ContractVersions.ReportKind);
        writer.WriteString("advisory_notice", ContractVersions.AdvisoryNotice);
        WriteOptionalString(writer, "job_id", job.Manifest.JobId);
        writer.WriteString("input_sha256", provenance.InputSha256);
        WriteSourceFiles(writer, provenance.SourceFiles);
        writer.WriteString("tool_name", provenance.Tool.ToolName);
        writer.WriteString("tool_version", provenance.Tool.ToolVersion);
        writer.WriteString("algorithm_name", provenance.AlgorithmName);
        writer.WriteString("algorithm_version", provenance.AlgorithmVersion);
        WriteParameters(writer, job.Manifest.Parameters);
        writer.WriteString("length_unit", ContractVersions.LengthUnit);
    }

    private static void WriteSourceFiles(Utf8JsonWriter writer, IReadOnlyList<SourceFileInfo> files)
    {
        writer.WriteStartArray("source_files");
        foreach (SourceFileInfo file in files)
        {
            writer.WriteStartObject();
            writer.WriteString("role", file.Role);
            writer.WriteString("sha256", file.Sha256);
            writer.WriteNumber("size_bytes", file.SizeBytes);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteParameters(Utf8JsonWriter writer, AlgorithmParameters parameters)
    {
        writer.WriteStartObject("parameters");
        writer.WriteNumber("seed", parameters.Seed);
        writer.WriteNumber("max_passes", parameters.MaxPasses);
        writer.WriteNumber("max_move_evaluations", parameters.MaxMoveEvaluations);
        writer.WriteNumber("min_improvement_mm", parameters.MinImprovementMm);
        writer.WriteEndObject();
    }

    private static void WriteComparison(Utf8JsonWriter writer, AnalysisResult result)
    {
        WriteOrder(writer, "baseline_order", result.BaselineRoute);
        WriteOrder(writer, "candidate_order", result.CandidateRoute);
        writer.WriteString("selected_route", ContractNames.ForSelection(result.Selection));
        writer.WriteString("selection_reason", result.SelectionReason);
        writer.WriteNumber("baseline_distance_mm", result.BaselineCheck.Distance.Millimetres);
        writer.WriteNumber("candidate_distance_mm", result.CandidateCheck.Distance.Millimetres);
        writer.WriteNumber("delta_distance_mm", result.Delta.Millimetres);
        if (result.DeltaPercent is double percent)
        {
            writer.WriteNumber("delta_percent", percent);
        }
        else
        {
            writer.WriteNull("delta_percent");
        }
    }

    private static void WriteConstraintChecks(Utf8JsonWriter writer, AnalysisResult result)
    {
        writer.WriteStartObject("constraint_check");
        WriteConstraintCheck(writer, "baseline", result.BaselineCheck);
        WriteConstraintCheck(writer, "candidate", result.CandidateCheck);
        writer.WriteEndObject();
    }

    private static void WriteConstraintCheck(Utf8JsonWriter writer, string name, FeasibilityReport check)
    {
        writer.WriteStartObject(name);
        writer.WriteBoolean("feasible", check.IsFeasible);
        writer.WriteNumber("violation_count", check.ViolationCount);
        ContractJson.WriteCodedMessages(writer, "violations", check.Violations, violation => violation.Code, violation => violation.Message);
        writer.WriteEndObject();
    }

    private static void WriteSearch(Utf8JsonWriter writer, SearchStatistics statistics)
    {
        writer.WriteStartObject("search");
        writer.WriteNumber("passes", statistics.Passes);
        writer.WriteNumber("move_evaluations", statistics.MoveEvaluations);
        writer.WriteNumber("accepted_moves", statistics.AcceptedMoves);
        writer.WriteString("termination", ContractNames.ForTermination(statistics.Termination));
        writer.WriteEndObject();
    }

    private static void WriteOrder(Utf8JsonWriter writer, string name, Route route)
    {
        writer.WriteStartArray(name);
        foreach (PointId id in route.OrderedIds)
        {
            writer.WriteStringValue(id.Value);
        }

        writer.WriteEndArray();
    }

    private static void WriteOptionalString(Utf8JsonWriter writer, string name, string value)
    {
        if (value.Length > 0)
        {
            writer.WriteString(name, value);
        }
        else
        {
            writer.WriteNull(name);
        }
    }
}
