using Microsoft.Extensions.Logging;

namespace ToolpathOptimizer.Application.Analysis;

/// <summary>
/// Source-generated log messages (event IDs 2000–2999). Data minimisation: counts, hashes, durations and
/// codes only; never coordinates, point IDs or file paths.
/// </summary>
internal static partial class ApplicationLog
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Analysis succeeded: points={PointCount} inputSha256={InputSha256} selected={Selected} durationMs={DurationMs}")]
    public static partial void AnalysisSucceeded(ILogger logger, int pointCount, string inputSha256, string selected, long durationMs);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "Analysis failed: code={Code} durationMs={DurationMs}")]
    public static partial void AnalysisFailed(ILogger logger, string code, long durationMs);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "Analysis defect: code={Code}")]
    public static partial void AnalysisDefect(ILogger logger, string code, Exception exception);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Validation finished: code={Code} points={PointCount}")]
    public static partial void ValidationFinished(ILogger logger, string code, int pointCount);
}
