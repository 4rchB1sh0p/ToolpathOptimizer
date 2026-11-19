using System.Text;
using System.Text.Json;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.UnitTests.Support;

namespace ToolpathOptimizer.Application.UnitTests.Contracts.V1;

/// <summary>Tests for <see cref="AnalysisReportWriter"/>: report v1 shape and byte-level determinism.</summary>
public sealed class AnalysisReportWriterTests
{
    private static readonly string[] _v1FieldOrder =
    [
        "schema_version", "report_kind", "advisory_notice", "job_id", "input_sha256", "source_files", "tool_name", "tool_version",
        "algorithm_name", "algorithm_version", "parameters", "length_unit", "canonical_job", "baseline_order", "candidate_order",
        "selected_route", "selection_reason", "baseline_distance_mm", "candidate_distance_mm", "delta_distance_mm", "delta_percent",
        "constraint_check", "search", "warnings", "generated_at_utc",
    ];

    private static byte[] CreateCrossingReport() =>
        new TestHandlers().Analyze.Handle(TestInputs.CreateJob(TestInputs.CrossingCsv), CancellationToken.None).Value.Content;

    [Fact]
    public void Serialize_AnyResult_WritesEveryV1FieldInOrder()
    {
        using JsonDocument document = JsonDocument.Parse(CreateCrossingReport());

        Assert.Equal(_v1FieldOrder, document.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
    }

    [Fact]
    public void Serialize_AnyResult_UsesLfNewlinesOnly()
    {
        Assert.DoesNotContain('\r', Encoding.UTF8.GetString(CreateCrossingReport()));
    }

    [Fact]
    public void Serialize_AnyResult_EndsWithNewline()
    {
        Assert.EndsWith("}\n", Encoding.UTF8.GetString(CreateCrossingReport()), StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_AnyResult_AllowsRecomputingCandidateDistance()
    {
        using JsonDocument document = JsonDocument.Parse(CreateCrossingReport());
        JsonElement root = document.RootElement;
        JsonElement job = root.GetProperty("canonical_job");
        Dictionary<string, (double X, double Y)> positions = job.GetProperty("points").EnumerateArray()
            .ToDictionary(point => point.GetProperty("point_id").GetString()!, point => (point.GetProperty("x_mm").GetDouble(), point.GetProperty("y_mm").GetDouble()));

        (double X, double Y) current = (job.GetProperty("start_x_mm").GetDouble(), job.GetProperty("start_y_mm").GetDouble());
        double total = 0;
        foreach (JsonElement id in root.GetProperty("candidate_order").EnumerateArray())
        {
            (double X, double Y) next = positions[id.GetString()!];
            total += Math.Sqrt(Math.Pow(next.X - current.X, 2) + Math.Pow(next.Y - current.Y, 2));
            current = next;
        }

        Assert.Equal(root.GetProperty("candidate_distance_mm").GetDouble(), total, 9);
    }

    [Fact]
    public void Serialize_AnyResult_ContainsAdvisoryNotice()
    {
        using JsonDocument document = JsonDocument.Parse(CreateCrossingReport());

        Assert.Equal(ContractVersions.AdvisoryNotice, document.RootElement.GetProperty("advisory_notice").GetString());
    }
}
