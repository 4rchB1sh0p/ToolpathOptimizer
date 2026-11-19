using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Units;

namespace ToolpathOptimizer.Core.UnitTests.Support;

/// <summary>Optional features of a generated random job.</summary>
[Flags]
internal enum RandomJobFeatures
{
    /// <summary>Points and precedence only.</summary>
    None = 0,

    /// <summary>Points 1 and 2 form the locked group <c>LOCK</c>.</summary>
    LockedGroup = 1,

    /// <summary>The path ends at a fixed position.</summary>
    FixedEnd = 2,
}

/// <summary>Shared test data: points, routes, default options and seeded random jobs.</summary>
internal static class TestData
{
    /// <summary>Gets generous budgets with the default 0.001 mm minimum improvement.</summary>
    public static AnalysisOptions DefaultOptions { get; } = new(new SearchBudget(1000, 50_000_000), Length.FromMillimetres(0.001));

    /// <summary>Creates an ungrouped point.</summary>
    /// <param name="id">Point identifier.</param>
    /// <param name="xMm">X in millimetres.</param>
    /// <param name="yMm">Y in millimetres.</param>
    /// <returns>The point.</returns>
    public static DrillPoint PointAt(string id, double xMm, double yMm) => PointInGroup(id, xMm, yMm, string.Empty);

    /// <summary>Creates a point in an operation group.</summary>
    /// <param name="id">Point identifier.</param>
    /// <param name="xMm">X in millimetres.</param>
    /// <param name="yMm">Y in millimetres.</param>
    /// <param name="group">Operation group name.</param>
    /// <returns>The point.</returns>
    public static DrillPoint PointInGroup(string id, double xMm, double yMm, string group) =>
        new(new PointId(id), Point2.FromMillimetres(xMm, yMm), group);

    /// <summary>Creates a route from identifiers.</summary>
    /// <param name="ids">Point identifiers in visit order.</param>
    /// <returns>The route.</returns>
    public static Route RouteOf(params string[] ids) => new(ids.Select(id => new PointId(id)).ToArray());

    /// <summary>Lists a route's identifiers.</summary>
    /// <param name="route">Route.</param>
    /// <returns>Identifier texts in visit order.</returns>
    public static string[] IdsOf(Route route) => route.OrderedIds.Select(id => id.Value).ToArray();

    /// <summary>
    /// Creates a seeded random job on a 0.5 mm grid. Precedence edges always point from a lower to a higher
    /// baseline index, so the baseline is feasible and the constraint graph acyclic.
    /// </summary>
    /// <param name="seed">Random seed.</param>
    /// <param name="pointCount">Number of points.</param>
    /// <param name="precedenceEdgeCount">Number of random precedence edges.</param>
    /// <param name="features">Optional locked group and fixed end.</param>
    /// <returns>The job.</returns>
    public static DrillJob CreateRandomJob(int seed, int pointCount, int precedenceEdgeCount, RandomJobFeatures features)
    {
        var generator = new Random(seed);
        bool hasLockedGroup = features.HasFlag(RandomJobFeatures.LockedGroup);
        DrillPoint[] points = CreateRandomPoints(generator, pointCount, hasLockedGroup);
        JobBuilder builder = JobBuilder.WithPoints(points).StartingAt(-10, -10);
        for (int i = 0; i < precedenceEdgeCount; i++)
        {
            int before = generator.Next(0, pointCount - 1);
            int after = generator.Next(before + 1, pointCount);
            builder.WithPrecedence(points[before].Id.Value, points[after].Id.Value);
        }

        if (hasLockedGroup)
        {
            builder.WithLockedGroup("LOCK");
        }

        if (features.HasFlag(RandomJobFeatures.FixedEnd))
        {
            builder.EndingAt(210, 210);
        }

        return builder.Build();
    }

    /// <summary>Applies a coordinate transform to every position, keeping IDs, groups and constraints.</summary>
    /// <param name="job">Original job.</param>
    /// <param name="transform">Position transform in metres.</param>
    /// <returns>The transformed job.</returns>
    public static DrillJob TransformPositions(DrillJob job, Func<Point2, Point2> transform) => new(
        transform(job.Start),
        job.End is Point2 end ? transform(end) : null,
        job.Points.Select(p => new DrillPoint(p.Id, transform(p.Position), p.OperationGroup)).ToArray(),
        job.Constraints);

    private static DrillPoint[] CreateRandomPoints(Random generator, int pointCount, bool hasLockedGroup)
    {
        var usedCells = new HashSet<(int, int)>();
        var points = new List<DrillPoint>(pointCount);
        while (points.Count < pointCount)
        {
            (int cellX, int cellY) = (generator.Next(0, 400), generator.Next(0, 400));
            if (!usedCells.Add((cellX, cellY)))
            {
                continue;
            }

            string group = hasLockedGroup && points.Count is >= 1 and <= 2 ? "LOCK" : string.Empty;
            points.Add(PointInGroup($"N{points.Count:D3}", cellX * 0.5, cellY * 0.5, group));
        }

        return points.ToArray();
    }
}
