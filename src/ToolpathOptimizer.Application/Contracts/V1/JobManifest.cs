namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// A validated v1 job manifest in millimetres, with effective algorithm parameters.
/// </summary>
public sealed class JobManifest
{
    /// <summary>Creates a manifest.</summary>
    /// <param name="jobId">Optional job identifier; empty when the manifest has none.</param>
    /// <param name="startXMm">Fixed start X in millimetres.</param>
    /// <param name="startYMm">Fixed start Y in millimetres.</param>
    /// <param name="end">Optional fixed end position in millimetres; absent when the path is open-ended.</param>
    /// <param name="precedence">Precedence entries in input order.</param>
    /// <param name="lockedGroups">Locked operation group names in input order.</param>
    /// <param name="parameters">Effective algorithm parameters.</param>
    public JobManifest(
        string jobId,
        double startXMm,
        double startYMm,
        (double XMm, double YMm)? end,
        IReadOnlyList<PrecedenceEntry> precedence,
        IReadOnlyList<string> lockedGroups,
        AlgorithmParameters parameters)
    {
        JobId = jobId;
        StartXMm = startXMm;
        StartYMm = startYMm;
        EndXMm = end?.XMm;
        EndYMm = end?.YMm;
        Precedence = precedence.ToArray();
        LockedGroups = lockedGroups.ToArray();
        Parameters = parameters;
    }

    /// <summary>Gets the optional job identifier; empty when the manifest has none.</summary>
    public string JobId { get; }

    /// <summary>Gets the fixed start X in millimetres.</summary>
    public double StartXMm { get; }

    /// <summary>Gets the fixed start Y in millimetres.</summary>
    public double StartYMm { get; }

    /// <summary>Gets the optional fixed end X in millimetres (absent for open-ended paths).</summary>
    public double? EndXMm { get; }

    /// <summary>Gets the optional fixed end Y in millimetres (absent for open-ended paths).</summary>
    public double? EndYMm { get; }

    /// <summary>Gets the precedence entries in input order.</summary>
    public IReadOnlyList<PrecedenceEntry> Precedence { get; }

    /// <summary>Gets the locked operation group names in input order.</summary>
    public IReadOnlyList<string> LockedGroups { get; }

    /// <summary>Gets the effective algorithm parameters.</summary>
    public AlgorithmParameters Parameters { get; }
}
