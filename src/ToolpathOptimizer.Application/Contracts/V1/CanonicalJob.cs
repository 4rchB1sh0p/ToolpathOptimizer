using System.Security.Cryptography;
using System.Text.Json;
using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;

namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// The job exactly as hashed and embedded in the report (D-017): points in baseline order with imported
/// millimetre values, start/end and constraints in input order.
/// </summary>
public sealed class CanonicalJob
{
    private CanonicalJob(IReadOnlyList<PointRow> points, JobManifest manifest)
    {
        Points = points;
        Manifest = manifest;
    }

    /// <summary>Gets the points in baseline (sequence) order.</summary>
    public IReadOnlyList<PointRow> Points { get; }

    /// <summary>Gets the manifest providing start, end and constraints.</summary>
    public JobManifest Manifest { get; }

    /// <summary>Creates the canonical job from parsed inputs.</summary>
    /// <param name="rows">Parsed CSV rows (any order).</param>
    /// <param name="manifest">Parsed manifest.</param>
    /// <returns>The canonical job.</returns>
    public static CanonicalJob Create(IReadOnlyList<PointRow> rows, JobManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(manifest);
        return new CanonicalJob(rows.OrderBy(r => r.Sequence).ToArray(), manifest);
    }

    /// <summary>Maps the canonical job to a Core job in metres.</summary>
    /// <returns>The SI job.</returns>
    public DrillJob ToDrillJob()
    {
        DrillPoint[] points = Points
            .Select(r => new DrillPoint(new PointId(r.PointId), Point2.FromMillimetres(r.XMm, r.YMm), r.OperationGroup))
            .ToArray();
        Point2? end = Manifest.EndXMm is double ex && Manifest.EndYMm is double ey ? Point2.FromMillimetres(ex, ey) : null;
        PrecedenceEdge[] edges = Manifest.Precedence.Select(e => new PrecedenceEdge(new PointId(e.Before), new PointId(e.After))).ToArray();
        return new DrillJob(Point2.FromMillimetres(Manifest.StartXMm, Manifest.StartYMm), end, points, new ConstraintSet(edges, Manifest.LockedGroups));
    }

    /// <summary>Computes the canonical input hash: SHA-256 of the compact canonical JSON.</summary>
    /// <returns>Lower-case hexadecimal hash.</returns>
    public string ComputeSha256() =>
        Convert.ToHexStringLower(SHA256.HashData(ContractJson.ToUtf8Bytes(WriteTo, ContractJson.Canonical)));

    /// <summary>Writes the canonical job as a JSON object.</summary>
    /// <param name="writer">Target writer.</param>
    public void WriteTo(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WriteStartArray("points");
        foreach (PointRow point in Points)
        {
            writer.WriteStartObject();
            writer.WriteString("point_id", point.PointId);
            writer.WriteNumber("x_mm", point.XMm);
            writer.WriteNumber("y_mm", point.YMm);
            writer.WriteString("operation_group", point.OperationGroup);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteNumber("start_x_mm", Manifest.StartXMm);
        writer.WriteNumber("start_y_mm", Manifest.StartYMm);
        WriteOptionalNumber(writer, "end_x_mm", Manifest.EndXMm);
        WriteOptionalNumber(writer, "end_y_mm", Manifest.EndYMm);
        writer.WriteStartObject("constraints");
        writer.WriteStartArray("precedence");
        foreach (PrecedenceEntry edge in Manifest.Precedence)
        {
            writer.WriteStartObject();
            writer.WriteString("before", edge.Before);
            writer.WriteString("after", edge.After);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("locked_groups");
        foreach (string group in Manifest.LockedGroups)
        {
            writer.WriteStringValue(group);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteOptionalNumber(Utf8JsonWriter writer, string name, double? value)
    {
        if (value is double number)
        {
            writer.WriteNumber(name, number);
        }
        else
        {
            writer.WriteNull(name);
        }
    }
}
