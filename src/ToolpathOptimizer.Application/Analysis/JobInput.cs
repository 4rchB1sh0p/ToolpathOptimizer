namespace ToolpathOptimizer.Application.Analysis;

/// <summary>
/// Raw job inputs: points CSV and manifest bytes with file labels used in messages.
/// </summary>
public sealed class JobInput
{
    /// <summary>Creates job input.</summary>
    /// <param name="pointsCsv">Points CSV bytes.</param>
    /// <param name="manifestJson">Manifest JSON bytes.</param>
    /// <param name="pointsLabel">File label for the CSV (file name only, never a full path).</param>
    /// <param name="manifestLabel">File label for the manifest.</param>
    public JobInput(byte[] pointsCsv, byte[] manifestJson, string pointsLabel = "points.csv", string manifestLabel = "job.json")
    {
        PointsCsv = pointsCsv;
        ManifestJson = manifestJson;
        PointsLabel = pointsLabel;
        ManifestLabel = manifestLabel;
    }

    /// <summary>Gets the points CSV bytes.</summary>
    public byte[] PointsCsv { get; }

    /// <summary>Gets the manifest bytes.</summary>
    public byte[] ManifestJson { get; }

    /// <summary>Gets the CSV file label.</summary>
    public string PointsLabel { get; }

    /// <summary>Gets the manifest file label.</summary>
    public string ManifestLabel { get; }
}
