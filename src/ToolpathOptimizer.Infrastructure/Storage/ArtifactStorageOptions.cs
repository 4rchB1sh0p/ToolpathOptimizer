namespace ToolpathOptimizer.Infrastructure.Storage;

/// <summary>
/// Options for <see cref="LocalArtifactStore"/>.
/// </summary>
public sealed class ArtifactStorageOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ArtifactStorage";

    /// <summary>Gets or sets the root directory (development only; object storage later).</summary>
    public string RootPath { get; set; } = "App_Data/artifacts";
}

