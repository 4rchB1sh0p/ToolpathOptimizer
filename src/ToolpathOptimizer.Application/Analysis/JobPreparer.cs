using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Provenance;
using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Application.Analysis;

/// <summary>
/// Parses both inputs strictly, maps millimetres to metres and computes provenance hashes.
/// </summary>
public sealed class JobPreparer
{
    private readonly IOptions<AnalysisLimitsOptions> _limits;

    /// <summary>Creates the preparer.</summary>
    /// <param name="limits">Configured limits.</param>
    public JobPreparer(IOptions<AnalysisLimitsOptions> limits)
    {
        _limits = limits;
    }

    /// <summary>Prepares raw inputs.</summary>
    /// <param name="input">Raw inputs.</param>
    /// <returns>The prepared job, or the first input violation.</returns>
    public Result<PreparedJob> Prepare(JobInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        AnalysisLimitsOptions limits = _limits.Value;
        Result<IReadOnlyList<PointRow>> rows = PointsCsvReader.Read(input.PointsCsv, input.PointsLabel, limits);
        if (!rows.IsSuccess)
        {
            return rows.FailAs<PreparedJob>();
        }

        Result<JobManifest> manifest = JobManifestReader.Read(input.ManifestJson, input.ManifestLabel, limits);
        if (!manifest.IsSuccess)
        {
            return manifest.FailAs<PreparedJob>();
        }

        var canonical = CanonicalJob.Create(rows.Value, manifest.Value);
        SourceFileInfo[] sources =
        [
            SourceFileInfo.Describe("points", input.PointsCsv),
            SourceFileInfo.Describe("manifest", input.ManifestJson),
        ];
        return Result<PreparedJob>.Success(new PreparedJob(canonical, canonical.ToDrillJob(), canonical.ComputeSha256(), sources));
    }
}
