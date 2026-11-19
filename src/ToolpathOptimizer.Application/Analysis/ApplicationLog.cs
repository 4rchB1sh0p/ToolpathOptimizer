using Microsoft.Extensions.Logging;

namespace ToolpathOptimizer.Application.Analysis;

/// <summary>
/// Source-generated log messages (event IDs 2000–2999). Data minimisation: counts, hashes, durations and
/// codes only; never coordinates, point IDs or file paths.
/// </summary>
internal static partial class ApplicationLog
{
    /// <summary>Logs a successful analysis.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="pointCount">Number of points analysed.</param>
    /// <param name="inputSha256">Canonical input hash.</param>
    /// <param name="selected">Selected route name.</param>
    /// <param name="durationMs">Elapsed milliseconds.</param>
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Analysis succeeded: points={PointCount} inputSha256={InputSha256} selected={Selected} durationMs={DurationMs}")]
    public static partial void AnalysisSucceeded(ILogger logger, int pointCount, string inputSha256, string selected, long durationMs);

    /// <summary>Logs an expected analysis failure by code.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="code">First error code.</param>
    /// <param name="durationMs">Elapsed milliseconds.</param>
    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "Analysis failed: code={Code} durationMs={DurationMs}")]
    public static partial void AnalysisFailed(ILogger logger, string code, long durationMs);

    /// <summary>Logs an unexpected exception (a defect) during analysis.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="code">Code returned to the caller.</param>
    /// <param name="exception">The unexpected exception.</param>
    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "Analysis defect: code={Code}")]
    public static partial void AnalysisDefect(ILogger logger, string code, Exception exception);

    /// <summary>Logs the result of a validation-only request.</summary>
    /// <param name="logger">Target logger.</param>
    /// <param name="code">First error code, or <c>OK</c>.</param>
    /// <param name="pointCount">Number of points parsed (zero when parsing failed).</param>
    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Validation finished: code={Code} points={PointCount}")]
    public static partial void ValidationFinished(ILogger logger, string code, int pointCount);
}
