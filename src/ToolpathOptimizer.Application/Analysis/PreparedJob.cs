using ToolpathOptimizer.Application.Contracts.V1;
using ToolpathOptimizer.Application.Provenance;
using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Application.Analysis;

/// <summary>
/// Parsed, mapped and hashed inputs ready for Core.
/// </summary>
public sealed class PreparedJob
{
    /// <summary>Creates a prepared job.</summary>
    /// <param name="canonicalJob">Canonical job.</param>
    /// <param name="drillJob">SI job.</param>
    /// <param name="inputSha256">Canonical input hash.</param>
    /// <param name="sourceFiles">Raw-file provenance.</param>
    public PreparedJob(CanonicalJob canonicalJob, DrillJob drillJob, string inputSha256, IReadOnlyList<SourceFileInfo> sourceFiles)
    {
        CanonicalJob = canonicalJob;
        DrillJob = drillJob;
        InputSha256 = inputSha256;
        SourceFiles = sourceFiles.ToArray();
    }

    /// <summary>Gets the canonical job.</summary>
    public CanonicalJob CanonicalJob { get; }

    /// <summary>Gets the SI job.</summary>
    public DrillJob DrillJob { get; }

    /// <summary>Gets the canonical input hash.</summary>
    public string InputSha256 { get; }

    /// <summary>Gets the raw-file provenance.</summary>
    public IReadOnlyList<SourceFileInfo> SourceFiles { get; }
}

