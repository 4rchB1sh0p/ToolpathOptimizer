using ToolpathOptimizer.Core.Geometry;

namespace ToolpathOptimizer.Core.Domain;

/// <summary>
/// An independent drill location with an optional operation group.
/// </summary>
public sealed class DrillPoint
{
    /// <summary>Creates a drill point.</summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="position">Position in metres.</param>
    /// <param name="operationGroup">Operation group name, or empty when ungrouped.</param>
    public DrillPoint(PointId id, Point2 position, string operationGroup)
    {
        ArgumentNullException.ThrowIfNull(operationGroup);
        if (operationGroup.Length > 0 && !IdentifierRules.IsValid(operationGroup))
        {
            throw new ArgumentException("Invalid operation group.", nameof(operationGroup));
        }

        Id = id;
        Position = position;
        OperationGroup = operationGroup;
    }

    /// <summary>Gets the point identifier.</summary>
    public PointId Id { get; }

    /// <summary>Gets the position in metres.</summary>
    public Point2 Position { get; }

    /// <summary>Gets the operation group name; empty when the point is ungrouped.</summary>
    public string OperationGroup { get; }

    /// <summary>Gets a value indicating whether the point belongs to an operation group.</summary>
    public bool HasOperationGroup => OperationGroup.Length > 0;
}
