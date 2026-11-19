using System.Text.Json;
using ToolpathOptimizer.Application.Limits;
using ToolpathOptimizer.Application.Outcomes;

namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Strict reader for the v1 job manifest (ADR-0010): rejects unknown or duplicate properties, comments,
/// trailing commas, depth &gt; 16, wrong types and unsupported versions, units or constraints.
/// </summary>
public static class JobManifestReader
{
    private const int MaxDepth = 16;

    /// <summary>Parses and validates manifest bytes.</summary>
    /// <param name="content">Raw file bytes (an optional UTF-8 BOM is ignored).</param>
    /// <param name="fileLabel">File name used in messages.</param>
    /// <param name="limits">Configured limits and budget defaults.</param>
    /// <returns>The manifest, or the first violation.</returns>
    public static Result<JobManifest> Read(ReadOnlyMemory<byte> content, string fileLabel, AnalysisLimitsOptions limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (content.Length > limits.MaxManifestFileBytes)
        {
            return Result<JobManifest>.Failure(ErrorCodes.InputLimitExceeded, $"{fileLabel}: file exceeds {limits.MaxManifestFileBytes} bytes.");
        }

        Result<JsonDocument> document = ParseDocument(ByteOrderMark.TrimUtf8(content), fileLabel);
        if (!document.IsSuccess)
        {
            return document.FailAs<JobManifest>();
        }

        using JsonDocument parsed = document.Value;
        return new ManifestWalker(fileLabel, limits).ReadRoot(parsed.RootElement);
    }

    private static Result<JsonDocument> ParseDocument(ReadOnlyMemory<byte> content, string fileLabel)
    {
        var options = new JsonDocumentOptions { MaxDepth = MaxDepth, CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false };
        try
        {
            return Result<JsonDocument>.Success(JsonDocument.Parse(content, options));
        }
        catch (JsonException ex)
        {
            string position = ex.LineNumber is long line ? $" near line {line + 1}" : string.Empty;
            return Result<JsonDocument>.Failure(ErrorCodes.InputMalformedJson, $"{fileLabel}: invalid JSON{position} (comments, trailing commas and depth > {MaxDepth} are rejected).");
        }
    }
}
