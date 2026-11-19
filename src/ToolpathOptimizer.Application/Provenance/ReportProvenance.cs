using System.Reflection;
using System.Security.Cryptography;
using ToolpathOptimizer.Core.Optimisation;

namespace ToolpathOptimizer.Application.Provenance;

/// <summary>
/// Raw-file provenance: role, SHA-256 and size.
/// </summary>
public sealed class SourceFileInfo
{
    /// <summary>Creates file provenance.</summary>
    /// <param name="role">File role, e.g. <c>points</c> or <c>manifest</c>.</param>
    /// <param name="sha256">Lower-case hexadecimal SHA-256 of the raw bytes.</param>
    /// <param name="sizeBytes">File size in bytes.</param>
    public SourceFileInfo(string role, string sha256, long sizeBytes)
    {
        Role = role;
        Sha256 = sha256;
        SizeBytes = sizeBytes;
    }

    /// <summary>Gets the file role.</summary>
    public string Role { get; }

    /// <summary>Gets the raw-bytes SHA-256.</summary>
    public string Sha256 { get; }

    /// <summary>Gets the size in bytes.</summary>
    public long SizeBytes { get; }

    /// <summary>Describes raw file bytes.</summary>
    /// <param name="role">File role.</param>
    /// <param name="content">Raw bytes.</param>
    /// <returns>The provenance entry.</returns>
    public static SourceFileInfo Describe(string role, ReadOnlySpan<byte> content) =>
        new(role, Convert.ToHexStringLower(SHA256.HashData(content)), content.Length);
}

/// <summary>
/// Identifies the tool that produced a report.
/// </summary>
public sealed class ToolInfo
{
    /// <summary>Creates tool information.</summary>
    /// <param name="toolName">Tool name.</param>
    /// <param name="toolVersion">Tool version.</param>
    public ToolInfo(string toolName, string toolVersion)
    {
        ToolName = toolName;
        ToolVersion = toolVersion;
    }

    /// <summary>Gets the tool name.</summary>
    public string ToolName { get; }

    /// <summary>Gets the tool version (excluded from determinism comparisons).</summary>
    public string ToolVersion { get; }

    /// <summary>Creates tool information from this assembly's informational version.</summary>
    /// <returns>The tool information.</returns>
    public static ToolInfo FromAssembly()
    {
        string version = typeof(ToolInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        return new ToolInfo("toolpath-optimize", version);
    }
}

/// <summary>
/// Provenance block of a report.
/// </summary>
public sealed class ReportProvenance
{
    /// <summary>Creates provenance.</summary>
    /// <param name="inputSha256">Canonical input hash.</param>
    /// <param name="sourceFiles">Raw-file provenance.</param>
    /// <param name="tool">Tool identity.</param>
    /// <param name="generatedAtUtc">Generation time from <see cref="TimeProvider"/>.</param>
    public ReportProvenance(string inputSha256, IReadOnlyList<SourceFileInfo> sourceFiles, ToolInfo tool, DateTimeOffset generatedAtUtc)
    {
        InputSha256 = inputSha256;
        SourceFiles = sourceFiles.ToArray();
        Tool = tool;
        GeneratedAtUtc = generatedAtUtc;
    }

    /// <summary>Gets the canonical input hash.</summary>
    public string InputSha256 { get; }

    /// <summary>Gets the raw-file provenance.</summary>
    public IReadOnlyList<SourceFileInfo> SourceFiles { get; }

    /// <summary>Gets the tool identity.</summary>
    public ToolInfo Tool { get; }

    /// <summary>Gets the algorithm name.</summary>
    public string AlgorithmName => RouteOptimiser.AlgorithmName;

    /// <summary>Gets the algorithm version.</summary>
    public string AlgorithmVersion => RouteOptimiser.AlgorithmVersion;

    /// <summary>Gets the generation time (excluded from determinism comparisons).</summary>
    public DateTimeOffset GeneratedAtUtc { get; }
}

/// <summary>
/// Creates <see cref="ReportProvenance"/> using the injected clock.
/// </summary>
public sealed class ProvenanceFactory
{
    private readonly TimeProvider _timeProvider;
    private readonly ToolInfo _tool;

    /// <summary>Creates the factory.</summary>
    /// <param name="timeProvider">Clock.</param>
    /// <param name="tool">Tool identity.</param>
    public ProvenanceFactory(TimeProvider timeProvider, ToolInfo tool)
    {
        _timeProvider = timeProvider;
        _tool = tool;
    }

    /// <summary>Creates provenance stamped with the current UTC time.</summary>
    /// <param name="inputSha256">Canonical input hash.</param>
    /// <param name="sourceFiles">Raw-file provenance.</param>
    /// <returns>The provenance.</returns>
    public ReportProvenance Create(string inputSha256, IReadOnlyList<SourceFileInfo> sourceFiles) =>
        new(inputSha256, sourceFiles, _tool, _timeProvider.GetUtcNow());
}
