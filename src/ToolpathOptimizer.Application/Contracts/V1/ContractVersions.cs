namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Fixed v1 contract constants.
/// </summary>
public static class ContractVersions
{
    /// <summary>Supported manifest schema version.</summary>
    public const string ManifestSchemaVersion = "1.0";

    /// <summary>Report schema version.</summary>
    public const string ReportSchemaVersion = "1.0";

    /// <summary>CLI envelope schema version.</summary>
    public const string EnvelopeSchemaVersion = "1.0";

    /// <summary>Required points CSV header.</summary>
    public const string PointsCsvHeader = "point_id,x_mm,y_mm,sequence,operation_group";

    /// <summary>Report kind.</summary>
    public const string ReportKind = "advisory_drilling_sequence";

    /// <summary>Supported length unit.</summary>
    public const string LengthUnit = "mm";

    /// <summary>Supported coordinate system.</summary>
    public const string CoordinateSystem = "XY_CARTESIAN";

    /// <summary>Fixed advisory notice embedded in every report (ADR-0003).</summary>
    public const string AdvisoryNotice =
        "Advisory analysis only. This report is not machine-ready NC code, models non-cutting travel distance only, " +
        "makes no collision-safety, machine-safety or cycle-time claim, and must be reviewed by a qualified programmer " +
        "before any sequence is re-entered in CAM.";
}
