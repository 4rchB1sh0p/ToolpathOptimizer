using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;

namespace ToolpathOptimizer.Core.UnitTests.Support;

/// <summary>
/// Fluent builder for synthetic jobs in millimetres (mapped to metres like the Application layer).
/// Defaults: start at the origin, open-ended path, no constraints.
/// </summary>
internal sealed class JobBuilder
{
    private readonly List<DrillPoint> _points;
    private readonly List<PrecedenceEdge> _precedence = [];
    private readonly List<string> _lockedGroups = [];
    private Point2 _start = new(0, 0);
    private Point2? _end;

    private JobBuilder(IEnumerable<DrillPoint> points)
    {
        _points = points.ToList();
    }

    /// <summary>Starts a job with points in baseline order.</summary>
    /// <param name="points">Points in baseline order.</param>
    /// <returns>The builder.</returns>
    public static JobBuilder WithPoints(params DrillPoint[] points) => new(points);

    /// <summary>Requires <paramref name="before"/> to be visited earlier than <paramref name="after"/>.</summary>
    /// <param name="before">Point visited first.</param>
    /// <param name="after">Point visited later.</param>
    /// <returns>The builder.</returns>
    public JobBuilder WithPrecedence(string before, string after)
    {
        _precedence.Add(new PrecedenceEdge(new PointId(before), new PointId(after)));
        return this;
    }

    /// <summary>Locks an operation group so it stays contiguous in baseline order.</summary>
    /// <param name="group">Operation group name.</param>
    /// <returns>The builder.</returns>
    public JobBuilder WithLockedGroup(string group)
    {
        _lockedGroups.Add(group);
        return this;
    }

    /// <summary>Sets the fixed start position.</summary>
    /// <param name="xMm">X in millimetres.</param>
    /// <param name="yMm">Y in millimetres.</param>
    /// <returns>The builder.</returns>
    public JobBuilder StartingAt(double xMm, double yMm)
    {
        _start = Point2.FromMillimetres(xMm, yMm);
        return this;
    }

    /// <summary>Sets a fixed end position.</summary>
    /// <param name="xMm">X in millimetres.</param>
    /// <param name="yMm">Y in millimetres.</param>
    /// <returns>The builder.</returns>
    public JobBuilder EndingAt(double xMm, double yMm)
    {
        _end = Point2.FromMillimetres(xMm, yMm);
        return this;
    }

    /// <summary>Builds the job.</summary>
    /// <returns>The job.</returns>
    public DrillJob Build() => new(_start, _end, _points, new ConstraintSet(_precedence, _lockedGroups));
}
