using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Writes a CSV summary of the selected route from a v1 report. Text cells beginning with
/// <c>=</c>, <c>+</c>, <c>-</c> or <c>@</c> are prefixed with an apostrophe to block formula injection.
/// </summary>
public static class ReportCsvSummaryWriter
{
    /// <summary>Creates the CSV summary.</summary>
    /// <param name="reportJson">Report v1 bytes.</param>
    /// <returns>UTF-8 CSV bytes with LF line endings.</returns>
    public static byte[] Write(ReadOnlyMemory<byte> reportJson)
    {
        using JsonDocument report = JsonDocument.Parse(reportJson);
        JsonElement root = report.RootElement;
        bool isCandidateSelected = root.GetProperty("selected_route").GetString() == ContractNames.CandidateRoute;
        JsonElement order = root.GetProperty(isCandidateSelected ? "candidate_order" : "baseline_order");
        Dictionary<string, JsonElement> points = root.GetProperty("canonical_job").GetProperty("points").EnumerateArray()
            .ToDictionary(p => p.GetProperty("point_id").GetString()!, StringComparer.Ordinal);

        var csv = new StringBuilder("position,point_id,x_mm,y_mm,operation_group\n");
        int position = 1;
        foreach (JsonElement idElement in order.EnumerateArray())
        {
            string id = idElement.GetString()!;
            JsonElement point = points[id];
            csv.Append(position++.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(EscapeText(id)).Append(',')
                .Append(point.GetProperty("x_mm").GetRawText()).Append(',')
                .Append(point.GetProperty("y_mm").GetRawText()).Append(',')
                .Append(EscapeText(point.GetProperty("operation_group").GetString()!)).Append('\n');
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>Neutralises spreadsheet formula prefixes in a text cell.</summary>
    /// <param name="value">Cell text.</param>
    /// <returns>The escaped text.</returns>
    public static string EscapeText(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Length > 0 && value[0] is '=' or '+' or '-' or '@' ? "'" + value : value;
    }
}
