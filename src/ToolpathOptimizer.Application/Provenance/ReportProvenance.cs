using ToolpathOptimizer.Core.Optimisation;

namespace ToolpathOptimizer.Application.Provenance;

/// <summary>
/// Provenance block of a report: what was analysed, by which tool and algorithm, and when.
/// </summary>
public sealed class ReportProvenance
{
    /// <summary>Creates provenance.</summary>
    /// <param name="inputSha256">Canonical input hash (independent of row order and formatting).</param>
    /// <param name="sourceFiles">Raw-file hashes and sizes.</param>
    /// <param name="tool">Tool identity.</param>
    /// <param name="generatedAtUtc">Generation time from the injected <see cref="TimeProvider"/>.</param>
    public ReportProvenance(string inputSha256, IReadOnlyList<SourceFileInfo> sourceFiles, ToolInfo tool, DateTimeOffset generatedAtUtc)
    {
        InputSha256 = inputSha256;
        SourceFiles = sourceFiles.ToArray();
        Tool = tool;
        GeneratedAtUtc = generatedAtUtc;
    }

    /// <summary>Gets the canonical input hash.</summary>
    public string InputSha256 { get; }

    /// <summary>Gets the raw-file hashes and sizes.</summary>
    public IReadOnlyList<SourceFileInfo> SourceFiles { get; }

    /// <summary>Gets the tool identity.</summary>
    public ToolInfo Tool { get; }

    /// <summary>Gets the algorithm name from <see cref="RouteOptimiser"/>.</summary>
    public string AlgorithmName => RouteOptimiser.AlgorithmName;

    /// <summary>Gets the algorithm version from <see cref="RouteOptimiser"/>.</summary>
    public string AlgorithmVersion => RouteOptimiser.AlgorithmVersion;

    /// <summary>Gets the generation time (excluded from determinism comparisons).</summary>
    public DateTimeOffset GeneratedAtUtc { get; }
}
