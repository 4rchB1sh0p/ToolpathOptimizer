namespace ToolpathOptimizer.Web;

/// <summary>
/// HTTP request and rate limits.
/// </summary>
public sealed class WebLimitsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "WebLimits";

    /// <summary>Default maximum request body size in bytes.</summary>
    public const long DefaultMaxRequestBytes = 6 * 1024 * 1024;

    /// <summary>Gets or sets the maximum request body size in bytes (multipart uploads included).</summary>
    public long MaxRequestBytes { get; set; } = DefaultMaxRequestBytes;

    /// <summary>Gets or sets the number of API requests allowed per user per window.</summary>
    public int PermitLimit { get; set; } = 120;

    /// <summary>Gets or sets the rate-limit window.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
