using System.Text;
using ToolpathOptimizer.Application.Analysis;

namespace ToolpathOptimizer.Application.UnitTests.Support;

/// <summary>Synthetic CSV and manifest inputs used across Application tests.</summary>
internal static class TestInputs
{
    /// <summary>The four-hole square from sample-input.md; its baseline is already optimal.</summary>
    public const string SquareCsv = "point_id,x_mm,y_mm,sequence,operation_group\nH01,0,0,1,A\nH02,100,0,2,A\nH03,100,100,3,A\nH04,0,100,4,A\n";

    /// <summary>A crossing baseline that 2-opt shortens.</summary>
    public const string CrossingCsv = "point_id,x_mm,y_mm,sequence,operation_group\nA,0,10,1,\nD,10,20,2,\nC,0,20,3,\nB,10,10,4,\n";

    /// <summary>The smallest valid manifest: start at the origin, default budgets.</summary>
    public const string MinimalManifest =
        """{"schema_version":"1.0","coordinate_system":"XY_CARTESIAN","units":"mm","start_x_mm":0,"start_y_mm":0,"algorithm":{"name":"nearest-neighbor-2opt"}}""";

    /// <summary>Encodes text as UTF-8.</summary>
    /// <param name="text">Text.</param>
    /// <returns>UTF-8 bytes.</returns>
    public static byte[] ToUtf8(string text) => Encoding.UTF8.GetBytes(text);

    /// <summary>Creates job input with the minimal manifest.</summary>
    /// <param name="csv">Points CSV text.</param>
    /// <returns>The job input.</returns>
    public static JobInput CreateJob(string csv) => CreateJob(csv, MinimalManifest);

    /// <summary>Creates job input.</summary>
    /// <param name="csv">Points CSV text.</param>
    /// <param name="manifest">Manifest JSON text.</param>
    /// <returns>The job input.</returns>
    public static JobInput CreateJob(string csv, string manifest) => new(ToUtf8(csv), ToUtf8(manifest));

    /// <summary>Creates the minimal manifest with extra root fields appended.</summary>
    /// <param name="extraFields">JSON fragment starting with a comma, e.g. <c>,"job_id":"J1"</c>.</param>
    /// <returns>Manifest JSON text.</returns>
    public static string ManifestWith(string extraFields) => MinimalManifest[..^1] + extraFields + "}";
}
