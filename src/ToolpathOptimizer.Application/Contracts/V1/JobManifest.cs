namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// A precedence constraint as written in the manifest.
/// </summary>
public sealed class PrecedenceEntry
{
    /// <summary>Creates an entry.</summary>
    /// <param name="before">Point visited first.</param>
    /// <param name="after">Point visited later.</param>
    public PrecedenceEntry(string before, string after)
    {
        Before = before;
        After = after;
    }

    /// <summary>Gets the point visited first.</summary>
    public string Before { get; }

    /// <summary>Gets the point visited later.</summary>
    public string After { get; }
}

/// <summary>
/// A validated v1 job manifest with effective algorithm parameters (defaults applied).
/// </summary>
public sealed class JobManifest
{
    /// <summary>Creates a manifest.</summary>
    /// <param name="jobId">Job identifier; empty when absent.</param>
    /// <param name="startXMm">Start X in millimetres.</param>
    /// <param name="startYMm">Start Y in millimetres.</param>
    /// <param name="endXMm">Optional end X in millimetres.</param>
    /// <param name="endYMm">Optional end Y in millimetres.</param>
    /// <param name="precedence">Precedence entries in input order.</param>
    /// <param name="lockedGroups">Locked group names in input order.</param>
    /// <param name="parameters">Effective algorithm parameters.</param>
    public JobManifest(
        string jobId,
        double startXMm,
        double startYMm,
        double? endXMm,
        double? endYMm,
        IReadOnlyList<PrecedenceEntry> precedence,
        IReadOnlyList<string> lockedGroups,
        AlgorithmParameters parameters)
    {
        JobId = jobId;
        StartXMm = startXMm;
        StartYMm = startYMm;
        EndXMm = endXMm;
        EndYMm = endYMm;
        Precedence = precedence.ToArray();
        LockedGroups = lockedGroups.ToArray();
        Parameters = parameters;
    }

    /// <summary>Gets the job identifier; empty when absent.</summary>
    public string JobId { get; }

    /// <summary>Gets the start X in millimetres.</summary>
    public double StartXMm { get; }

    /// <summary>Gets the start Y in millimetres.</summary>
    public double StartYMm { get; }

    /// <summary>Gets the optional end X in millimetres.</summary>
    public double? EndXMm { get; }

    /// <summary>Gets the optional end Y in millimetres.</summary>
    public double? EndYMm { get; }

    /// <summary>Gets the precedence entries.</summary>
    public IReadOnlyList<PrecedenceEntry> Precedence { get; }

    /// <summary>Gets the locked group names.</summary>
    public IReadOnlyList<string> LockedGroups { get; }

    /// <summary>Gets the effective algorithm parameters.</summary>
    public AlgorithmParameters Parameters { get; }
}

/// <summary>
/// Effective algorithm parameters recorded in the report (ADR-0008).
/// </summary>
public sealed class AlgorithmParameters
{
    /// <summary>Creates parameters.</summary>
    /// <param name="seed">Reserved seed (always 0).</param>
    /// <param name="maxPasses">Pass budget.</param>
    /// <param name="maxMoveEvaluations">Move-evaluation budget.</param>
    /// <param name="minImprovementMm">Minimum improvement in millimetres.</param>
    public AlgorithmParameters(long seed, long maxPasses, long maxMoveEvaluations, double minImprovementMm)
    {
        Seed = seed;
        MaxPasses = maxPasses;
        MaxMoveEvaluations = maxMoveEvaluations;
        MinImprovementMm = minImprovementMm;
    }

    /// <summary>Gets the reserved seed.</summary>
    public long Seed { get; }

    /// <summary>Gets the pass budget.</summary>
    public long MaxPasses { get; }

    /// <summary>Gets the move-evaluation budget.</summary>
    public long MaxMoveEvaluations { get; }

    /// <summary>Gets the minimum improvement in millimetres.</summary>
    public double MinImprovementMm { get; }
}
