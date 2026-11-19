using System.Security.Cryptography;

namespace ToolpathOptimizer.Application.Provenance;

/// <summary>
/// Raw-file provenance recorded in the report: role, SHA-256 and size of an input file exactly as received.
/// </summary>
public sealed class SourceFileInfo
{
    /// <summary>Creates file provenance.</summary>
    /// <param name="role">File role, <c>points</c> or <c>manifest</c>.</param>
    /// <param name="sha256">Lower-case hexadecimal SHA-256 of the raw bytes.</param>
    /// <param name="sizeBytes">File size in bytes.</param>
    public SourceFileInfo(string role, string sha256, long sizeBytes)
    {
        Role = role;
        Sha256 = sha256;
        SizeBytes = sizeBytes;
    }

    /// <summary>Gets the file role, <c>points</c> or <c>manifest</c>.</summary>
    public string Role { get; }

    /// <summary>Gets the lower-case hexadecimal SHA-256 of the raw bytes.</summary>
    public string Sha256 { get; }

    /// <summary>Gets the file size in bytes.</summary>
    public long SizeBytes { get; }

    /// <summary>Hashes raw file bytes.</summary>
    /// <param name="role">File role.</param>
    /// <param name="content">Raw bytes as received.</param>
    /// <returns>The provenance entry.</returns>
    public static SourceFileInfo Describe(string role, ReadOnlySpan<byte> content) =>
        new(role, Convert.ToHexStringLower(SHA256.HashData(content)), content.Length);
}
