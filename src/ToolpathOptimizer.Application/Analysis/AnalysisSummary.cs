namespace ToolpathOptimizer.Application.Analysis;

/// <summary>
/// Key figures of a successful analysis, used by hosts without re-parsing the report.
/// </summary>
public sealed class AnalysisSummary
{
    /// <summary>Creates a summary.</summary>
    /// <param name="inputSha256">Canonical input hash.</param>
    /// <param name="pointCount">Number of points.</param>
    /// <param name="selectedRoute"><c>candidate</c> or <c>baseline</c>.</param>
    /// <param name="baselineDistanceMm">Baseline distance in millimetres.</param>
    /// <param name="candidateDistanceMm">Candidate distance in millimetres.</param>
    /// <param name="deltaPercent">Delta percentage, or <c>null</c>.</param>
    /// <param name="warningCodes">Warning codes.</param>
    public AnalysisSummary(string inputSha256, int pointCount, string selectedRoute, double baselineDistanceMm, double candidateDistanceMm, double? deltaPercent, IReadOnlyList<string> warningCodes)
    {
        InputSha256 = inputSha256;
        PointCount = pointCount;
        SelectedRoute = selectedRoute;
        BaselineDistanceMm = baselineDistanceMm;
        CandidateDistanceMm = candidateDistanceMm;
        DeltaPercent = deltaPercent;
        WarningCodes = warningCodes.ToArray();
    }

    /// <summary>Gets the canonical input hash.</summary>
    public string InputSha256 { get; }

    /// <summary>Gets the number of points.</summary>
    public int PointCount { get; }

    /// <summary>Gets the selected route kind.</summary>
    public string SelectedRoute { get; }

    /// <summary>Gets the baseline distance in millimetres.</summary>
    public double BaselineDistanceMm { get; }

    /// <summary>Gets the candidate distance in millimetres.</summary>
    public double CandidateDistanceMm { get; }

    /// <summary>Gets candidate minus baseline in millimetres.</summary>
    public double DeltaDistanceMm => CandidateDistanceMm - BaselineDistanceMm;

    /// <summary>Gets the delta percentage, or <c>null</c> when the baseline distance is zero.</summary>
    public double? DeltaPercent { get; }

    /// <summary>Gets the warning codes.</summary>
    public IReadOnlyList<string> WarningCodes { get; }
}

