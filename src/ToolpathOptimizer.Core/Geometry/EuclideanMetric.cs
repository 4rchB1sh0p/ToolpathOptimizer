namespace ToolpathOptimizer.Core.Geometry;

/// <summary>
/// Euclidean distance in metres. Uses plain multiply/add and <see cref="Math.Sqrt(double)"/>
/// (no FMA or SIMD) so results are reproducible across platforms.
/// </summary>
public static class EuclideanMetric
{
    /// <summary>Computes the straight-line distance between two points.</summary>
    /// <param name="from">Start point.</param>
    /// <param name="to">End point.</param>
    /// <returns>Distance in metres.</returns>
    public static double Distance(Point2 from, Point2 to) => Math.Sqrt(SquaredDistance(from, to));

    /// <summary>Computes the squared straight-line distance between two points.</summary>
    /// <param name="from">Start point.</param>
    /// <param name="to">End point.</param>
    /// <returns>Squared distance in square metres.</returns>
    public static double SquaredDistance(Point2 from, Point2 to)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        return (dx * dx) + (dy * dy);
    }
}
