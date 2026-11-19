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
    // Relaxed escaping keeps apostrophes and '+' readable; reports are served as application/json, never embedded in HTML.
    private static readonly JsonWriterOptions s_options = new()
    {
        Indented = true,
        NewLine = "\n",
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Serialises a report.</summary>
    /// <param name="canonicalJob">Canonical job.</param>
    /// <param name="provenance">Provenance.</param>
    /// <param name="result">Analysis result.</param>
    /// <returns>UTF-8 JSON bytes.</returns>
    public static byte[] Serialize(CanonicalJob canonicalJob, ReportProvenance provenance, AnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(canonicalJob);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(result);
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, s_options))
        {
            writer.WriteStartObject();
            WriteHeader(writer, canonicalJob, provenance);
            writer.WritePropertyName("canonical_job");
            canonicalJob.WriteTo(writer);
            WriteComparison(writer, result);
            WriteChecks(writer, result);
            WriteSearch(writer, result.Statistics);
            writer.WriteStartArray("warnings");
            foreach (AnalysisWarning warning in result.Warnings)
            {
                writer.WriteStartObject();
                writer.WriteString("code", warning.Code);
                writer.WriteString("message", warning.Message);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteString("generated_at_utc", provenance.GeneratedAtUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
            writer.WriteEndObject();
        }

        buffer.WriteByte((byte)'\n');
        return buffer.ToArray();
    }

    private static void WriteHeader(Utf8JsonWriter writer, CanonicalJob job, ReportProvenance provenance)
    {
        writer.WriteString("schema_version", ContractVersions.ReportSchemaVersion);
        writer.WriteString("report_kind", ContractVersions.ReportKind);
        writer.WriteString("advisory_notice", ContractVersions.AdvisoryNotice);
        if (job.Manifest.JobId.Length > 0)
        {
            writer.WriteString("job_id", job.Manifest.JobId);
        }
        else
        {
            writer.WriteNull("job_id");
        }

        writer.WriteString("input_sha256", provenance.InputSha256);
        writer.WriteStartArray("source_files");
        foreach (SourceFileInfo file in provenance.SourceFiles)
        {
            writer.WriteStartObject();
            writer.WriteString("role", file.Role);
            writer.WriteString("sha256", file.Sha256);
            writer.WriteNumber("size_bytes", file.SizeBytes);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteString("tool_name", provenance.Tool.ToolName);
        writer.WriteString("tool_version", provenance.Tool.ToolVersion);
        writer.WriteString("algorithm_name", provenance.AlgorithmName);
        writer.WriteString("algorithm_version", provenance.AlgorithmVersion);
        AlgorithmParameters parameters = job.Manifest.Parameters;
        writer.WriteStartObject("parameters");
        writer.WriteNumber("seed", parameters.Seed);
        writer.WriteNumber("max_passes", parameters.MaxPasses);
        writer.WriteNumber("max_move_evaluations", parameters.MaxMoveEvaluations);
        writer.WriteNumber("min_improvement_mm", parameters.MinImprovementMm);
        writer.WriteEndObject();
        writer.WriteString("length_unit", ContractVersions.LengthUnit);
    }

    private static void WriteComparison(Utf8JsonWriter writer, AnalysisResult result)
    {
        WriteOrder(writer, "baseline_order", result.BaselineRoute);
        WriteOrder(writer, "candidate_order", result.CandidateRoute);
        writer.WriteString("selected_route", result.Selection == RouteSelection.Candidate ? "candidate" : "baseline");
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

    private static void WriteChecks(Utf8JsonWriter writer, AnalysisResult result)
    {
        writer.WriteStartObject("constraint_check");
        WriteCheck(writer, "baseline", result.BaselineCheck);
        WriteCheck(writer, "candidate", result.CandidateCheck);
        writer.WriteEndObject();
    }

    private static void WriteCheck(Utf8JsonWriter writer, string name, FeasibilityReport check)
    {
        writer.WriteStartObject(name);
        writer.WriteBoolean("feasible", check.IsFeasible);
        writer.WriteNumber("violation_count", check.ViolationCount);
        writer.WriteStartArray("violations");
        foreach (FeasibilityViolation violation in check.Violations)
        {
            writer.WriteStartObject();
            writer.WriteString("code", violation.Code);
            writer.WriteString("message", violation.Message);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteSearch(Utf8JsonWriter writer, SearchStatistics statistics)
    {
        writer.WriteStartObject("search");
        writer.WriteNumber("passes", statistics.Passes);
        writer.WriteNumber("move_evaluations", statistics.MoveEvaluations);
        writer.WriteNumber("accepted_moves", statistics.AcceptedMoves);
        writer.WriteString("termination", statistics.Termination == SearchTermination.Converged ? "converged" : "budget_exhausted");
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
}
