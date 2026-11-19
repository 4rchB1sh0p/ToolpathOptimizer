namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// One parsed row of a v1 points CSV (millimetres, as imported).
/// </summary>
public sealed class PointRow
{
    /// <summary>Creates a row.</summary>
    /// <param name="pointId">Point identifier.</param>
    /// <param name="xMm">X in millimetres.</param>
    /// <param name="yMm">Y in millimetres.</param>
    /// <param name="sequence">Baseline sequence number (1-based).</param>
    /// <param name="operationGroup">Operation group; empty when none.</param>
    public PointRow(string pointId, double xMm, double yMm, int sequence, string operationGroup)
    {
        PointId = pointId;
        XMm = xMm;
        YMm = yMm;
        Sequence = sequence;
        OperationGroup = operationGroup;
    }

    /// <summary>Gets the point identifier.</summary>
    public string PointId { get; }

    /// <summary>Gets X in millimetres.</summary>
    public double XMm { get; }

    /// <summary>Gets Y in millimetres.</summary>
    public double YMm { get; }

    /// <summary>Gets the baseline sequence number.</summary>
    public int Sequence { get; }

    /// <summary>Gets the operation group; empty when none.</summary>
    public string OperationGroup { get; }
}
