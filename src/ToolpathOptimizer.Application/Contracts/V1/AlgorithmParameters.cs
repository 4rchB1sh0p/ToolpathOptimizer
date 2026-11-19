namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Effective algorithm parameters (manifest values with configured defaults applied), recorded in the report (ADR-0008).
/// </summary>
public sealed class AlgorithmParameters
{
    /// <summary>The only accepted seed value; reserved for future randomised algorithms.</summary>
    public const long ReservedSeed = 0;

    /// <summary>Creates parameters.</summary>
    /// <param name="seed">Reserved seed, always <see cref="ReservedSeed"/>.</param>
    /// <param name="maxPasses">2-opt pass budget; zero means construction only.</param>
    /// <param name="maxMoveEvaluations">2-opt move-evaluation budget.</param>
    /// <param name="minImprovementMm">Improvement required to prefer the candidate over a feasible baseline.</param>
    public AlgorithmParameters(long seed, long maxPasses, long maxMoveEvaluations, double minImprovementMm)
    {
        Seed = seed;
        MaxPasses = maxPasses;
        MaxMoveEvaluations = maxMoveEvaluations;
        MinImprovementMm = minImprovementMm;
    }

    /// <summary>Gets the reserved seed.</summary>
    public long Seed { get; }

    /// <summary>Gets the 2-opt pass budget; zero means construction only.</summary>
    public long MaxPasses { get; }

    /// <summary>Gets the 2-opt move-evaluation budget.</summary>
    public long MaxMoveEvaluations { get; }

    /// <summary>Gets the improvement in millimetres required to prefer the candidate over a feasible baseline.</summary>
    public double MinImprovementMm { get; }
}
