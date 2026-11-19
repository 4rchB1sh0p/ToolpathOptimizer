using ToolpathOptimizer.Core.Geometry;

namespace ToolpathOptimizer.Core.Domain;

/// <summary>
/// A drilling job in SI units: fixed start, optional fixed end, points in baseline order and constraints.
/// </summary>
public sealed class DrillJob
{
    private readonly Dictionary<PointId, int> _indexById;

    /// <summary>Creates a job. Throws on programmer errors such as duplicate identifiers.</summary>
    /// <param name="start">Fixed start position.</param>
    /// <param name="end">Optional fixed end position.</param>
    /// <param name="points">Points in baseline order; at least one, unique identifiers.</param>
    /// <param name="constraints">Constraints; references are checked by the job validator.</param>
    public DrillJob(Point2 start, Point2? end, IReadOnlyList<DrillPoint> points, ConstraintSet constraints)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(constraints);
        if (points.Count == 0)
        {
            throw new ArgumentException("A job needs at least one point.", nameof(points));
        }

        _indexById = new Dictionary<PointId, int>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            if (!_indexById.TryAdd(points[i].Id, i))
            {
                throw new ArgumentException("Duplicate point identifier.", nameof(points));
            }
        }

        Start = start;
        End = end;
        Points = points.ToArray();
        Constraints = constraints;
    }

    /// <summary>Gets the fixed start position (not a drill point).</summary>
    public Point2 Start { get; }

    /// <summary>Gets the optional fixed end position.</summary>
    public Point2? End { get; }

    /// <summary>Gets the points in baseline order.</summary>
    public IReadOnlyList<DrillPoint> Points { get; }

    /// <summary>Gets the constraints.</summary>
    public ConstraintSet Constraints { get; }

    /// <summary>Creates the baseline route (points in their input sequence).</summary>
    /// <returns>The baseline route.</returns>
    public Route CreateBaselineRoute() => new(Points.Select(p => p.Id).ToArray());

    /// <summary>Looks up the baseline index of a point.</summary>
    /// <param name="id">Point identifier.</param>
    /// <param name="index">Baseline index when found.</param>
    /// <returns><c>true</c> when the point exists.</returns>
    public bool TryGetIndex(PointId id, out int index) => _indexById.TryGetValue(id, out index);
}
