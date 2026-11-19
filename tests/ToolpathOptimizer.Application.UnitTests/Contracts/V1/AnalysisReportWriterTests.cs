using System.Text;
using System.Text.Json;
using ToolpathOptimizer.Application.Contracts.V1;

namespace ToolpathOptimizer.Application.UnitTests.Contracts.V1;

public sealed class AnalysisReportWriterTests
{
    private static readonly string[] s_requiredFields =
    [
        "schema_version", "report_kind", "advisory_notice", "job_id", "input_sha256", "source_files", "tool_name", "tool_version",
        "algorithm_name", "algorithm_version", "parameters", "length_unit", "canonical_job", "baseline_order", "candidate_order",
        "selected_route", "selection_reason", "baseline_distance_mm", "candidate_distance_mm", "delta_distance_mm", "delta_percent",
        "constraint_check", "search", "warnings", "generated_at_utc",
    ];

    private static byte[] Report() => new Handlers().Analyze.Handle(Inputs.Job(Inputs.CrossingCsv), CancellationToken.None).Value.Content;

    [Fact]
    public void Serialize_WritesEveryV1FieldInOrder()
    {
        using JsonDocument document = JsonDocument.Parse(Report());

        Assert.Equal(s_requiredFields, document.RootElement.EnumerateObject().Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Serialize_UsesLfNewlinesOnly()
    {
        string text = Encoding.UTF8.GetString(Report());

        Assert.DoesNotContain('\r', text);
        Assert.EndsWith("}\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_IsSelfContainedSoDistancesCanBeRecomputed()
    {
        using JsonDocument document = JsonDocument.Parse(Report());
        JsonElement root = document.RootElement;
        JsonElement job = root.GetProperty("canonical_job");
        Dictionary<string, (double X, double Y)> points = job.GetProperty("points").EnumerateArray()
            .ToDictionary(p => p.GetProperty("point_id").GetString()!, p => (p.GetProperty("x_mm").GetDouble(), p.GetProperty("y_mm").GetDouble()));

        (double X, double Y) current = (job.GetProperty("start_x_mm").GetDouble(), job.GetProperty("start_y_mm").GetDouble());
        double total = 0;
        foreach (JsonElement id in root.GetProperty("candidate_order").EnumerateArray())
        {
            (double X, double Y) next = points[id.GetString()!];
            total += Math.Sqrt(Math.Pow(next.X - current.X, 2) + Math.Pow(next.Y - current.Y, 2));
            current = next;
        }

        Assert.Equal(root.GetProperty("candidate_distance_mm").GetDouble(), total, 9);
    }

    [Fact]
    public void Serialize_ContainsAdvisoryNotice()
    {
        using JsonDocument document = JsonDocument.Parse(Report());

        Assert.Equal(ContractVersions.AdvisoryNotice, document.RootElement.GetProperty("advisory_notice").GetString());
    }
}
