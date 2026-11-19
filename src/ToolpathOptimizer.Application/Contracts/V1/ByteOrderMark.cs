namespace ToolpathOptimizer.Application.Contracts.V1;

/// <summary>
/// Removes an optional UTF-8 byte order mark from input files (both v1 input formats allow one).
/// </summary>
internal static class ByteOrderMark
{
    private static ReadOnlySpan<byte> Utf8Signature => [0xEF, 0xBB, 0xBF];

    /// <summary>Returns the content without a leading UTF-8 byte order mark.</summary>
    /// <param name="content">Raw file bytes.</param>
    /// <returns>The content after the mark, or the content unchanged when there is none.</returns>
    public static ReadOnlyMemory<byte> TrimUtf8(ReadOnlyMemory<byte> content) =>
        content.Span.StartsWith(Utf8Signature) ? content[Utf8Signature.Length..] : content;
}
