using BenchmarkDotNet.Attributes;
using ToolpathOptimizer.Core.Analysis;
using ToolpathOptimizer.Core.Domain;
using ToolpathOptimizer.Core.Geometry;
using ToolpathOptimizer.Core.Optimisation;
using ToolpathOptimizer.Core.Units;

namespace ToolpathOptimizer.Core.Benchmarks;

/// <summary>
/// End-to-end Core analysis on synthetic, uniformly scattered points (seeded, reproducible).
/// Used to set the unbenchmarked limits in TD-009.
/// </summary>
[MemoryDiagnoser]
public class AnalysisEngineBenchmarks
{
    private DrillJob _job = null!;
    private AnalysisOptions _options = null!;

    /// <summary>Gets or sets the number of points.</summary>
    [Params(100, 500, 2000)]
    public int PointCount { get; set; }

    /// <summary>Builds the synthetic job.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(20261119);
        var used = new HashSet<(int, int)>();
        var points = new List<DrillPoint>(PointCount);
        while (points.Count < PointCount)
        {
            (int x, int y) = (random.Next(0, 20000), random.Next(0, 20000));
            if (used.Add((x, y)))
            {
                points.Add(new DrillPoint(new PointId($"P{points.Count:D5}"), Point2.FromMillimetres(x * 0.05, y * 0.05), string.Empty));
            }
        }

        _job = new DrillJob(new Point2(0, 0), null, points, ConstraintSet.Empty);
        _options = new AnalysisOptions(new SearchBudget(100, 200_000_000), Length.FromMillimetres(0.001));
    }

    /// <summary>Validate, optimise, verify and select.</summary>
    /// <returns>The analysis outcome.</returns>
    [Benchmark]
    public AnalysisOutcome Analyse() => AnalysisEngine.Analyse(_job, _options, CancellationToken.None);
}
