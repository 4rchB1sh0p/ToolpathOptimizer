namespace ToolpathOptimizer.Core.Domain;

/// <summary>
/// An ordered visit sequence of point identifiers (the start and end positions are implicit).
/// </summary>
public sealed class Route
{
    /// <summary>Creates a route.</summary>
    /// <param name="orderedIds">Point identifiers in visit order.</param>
    public Route(IReadOnlyList<PointId> orderedIds)
    {
        ArgumentNullException.ThrowIfNull(orderedIds);
        OrderedIds = orderedIds.ToArray();
    }

    /// <summary>Gets the point identifiers in visit order.</summary>
    public IReadOnlyList<PointId> OrderedIds { get; }
}
