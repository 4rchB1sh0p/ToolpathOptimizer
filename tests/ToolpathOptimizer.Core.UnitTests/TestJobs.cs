using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Units;

namespace ToolpathOptimizer.Core.UnitTests;

/// <summary>Builders for synthetic jobs (millimetre inputs, mapped to metres like the Application layer).</summary>
internal static class TestJobs
{
    public static AnalysisOptions DefaultOptions { get; } = new(new SearchBudget(1000, 50_000_000), Length.FromMillimetres(0.001));

    public static DrillPoint P(string id, double xMm, double yMm, string group = "") =>
        new(new PointId(id), Point2.FromMillimetres(xMm, yMm), group);

    public static DrillJob Job(
        IReadOnlyList<DrillPoint> points,
        (string Before, string After)[]? precedence = null,
        string[]? lockedGroups = null,
        (double X, double Y)? startMm = null,
        (double X, double Y)? endMm = null)
    {
        (double sx, double sy) = startMm ?? (0, 0);
        Point2? end = endMm is (double ex, double ey) ? Point2.FromMillimetres(ex, ey) : null;
        PrecedenceEdge[] edges = (precedence ?? []).Select(e => new PrecedenceEdge(new PointId(e.Before), new PointId(e.After))).ToArray();
        return new DrillJob(Point2.FromMillimetres(sx, sy), end, points, new ConstraintSet(edges, lockedGroups ?? []));
    }

    public static Route Route(params string[] ids) => new(ids.Select(id => new PointId(id)).ToArray());

    public static string[] Ids(Route route) => route.OrderedIds.Select(id => id.Value).ToArray();

    /// <summary>Random points on a 0.5 mm grid with optional DAG precedence (edges from lower to higher index) and a locked group.</summary>
    public static DrillJob Random(int seed, int count, int edgeCount = 0, bool withLockedGroup = false, bool withEnd = false)
    {
        var random = new Random(seed);
        var used = new HashSet<(int, int)>();
        var points = new List<DrillPoint>();
        while (points.Count < count)
        {
            (int gx, int gy) = (random.Next(0, 400), random.Next(0, 400));
            if (!used.Add((gx, gy)))
            {
                continue;
            }

            string group = withLockedGroup && points.Count is >= 1 and <= 2 ? "LOCK" : string.Empty;
            points.Add(P($"N{points.Count:D3}", gx * 0.5, gy * 0.5, group));
        }

        var edges = new List<(string, string)>();
        for (int k = 0; k < edgeCount; k++)
        {
            int a = random.Next(0, count - 1);
            int b = random.Next(a + 1, count);
            edges.Add((points[a].Id.Value, points[b].Id.Value));
        }

        return Job(points, [.. edges], withLockedGroup ? ["LOCK"] : null, (-10, -10), withEnd ? (210, 210) : null);
    }

    /// <summary>Applies a coordinate transform to every position, keeping IDs, groups and constraints.</summary>
    public static DrillJob Transform(DrillJob job, Func<Point2, Point2> transform) => new(
        transform(job.Start),
        job.End is Point2 end ? transform(end) : null,
        job.Points.Select(p => new DrillPoint(p.Id, transform(p.Position), p.OperationGroup)).ToArray(),
        job.Constraints);
}
