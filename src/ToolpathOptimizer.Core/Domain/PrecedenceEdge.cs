namespace ToolpathOptimizer.Core.Domain;

/// <summary>
/// Requires <see cref="Before"/> to be visited earlier than <see cref="After"/> (not necessarily adjacent).
/// </summary>
public sealed class PrecedenceEdge
{
    /// <summary>Creates a precedence edge.</summary>
    /// <param name="before">Point that must be visited first.</param>
    /// <param name="after">Point that must be visited later.</param>
    public PrecedenceEdge(PointId before, PointId after)
    {
        Before = before;
        After = after;
    }

    /// <summary>Gets the point that must be visited first.</summary>
    public PointId Before { get; }

    /// <summary>Gets the point that must be visited later.</summary>
    public PointId After { get; }
}
