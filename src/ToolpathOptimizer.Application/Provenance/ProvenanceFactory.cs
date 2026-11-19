namespace ToolpathOptimizer.Application.Provenance;

/// <summary>
/// Creates <see cref="ReportProvenance"/> stamped by the injected clock, so tests control <c>generated_at_utc</c>.
/// </summary>
public sealed class ProvenanceFactory
{
    private readonly TimeProvider _timeProvider;
    private readonly ToolInfo _tool;

    /// <summary>Creates the factory.</summary>
    /// <param name="timeProvider">Clock used for the generation timestamp.</param>
    /// <param name="tool">Tool identity written into every report.</param>
    public ProvenanceFactory(TimeProvider timeProvider, ToolInfo tool)
    {
        _timeProvider = timeProvider;
        _tool = tool;
    }

    /// <summary>Creates provenance stamped with the current UTC time.</summary>
    /// <param name="inputSha256">Canonical input hash.</param>
    /// <param name="sourceFiles">Raw-file hashes and sizes.</param>
    /// <returns>The provenance.</returns>
    public ReportProvenance Create(string inputSha256, IReadOnlyList<SourceFileInfo> sourceFiles) =>
        new(inputSha256, sourceFiles, _tool, _timeProvider.GetUtcNow());
}
