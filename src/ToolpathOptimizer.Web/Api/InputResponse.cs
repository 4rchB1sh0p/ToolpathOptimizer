using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Web.Api;

/// <summary>Uploaded input as returned by the API.</summary>
public sealed class InputResponse
{
    /// <summary>Gets the identifier to pass when running an analysis.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the owning project.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>Gets the canonical input hash (identical for re-uploads of the same job).</summary>
    public string InputSha256 { get; init; } = string.Empty;

    /// <summary>Gets the number of drill points.</summary>
    public int PointCount { get; init; }

    /// <summary>Gets the upload time.</summary>
    public DateTimeOffset UploadedAtUtc { get; init; }

    /// <summary>Maps an input artifact.</summary>
    /// <param name="input">Domain input.</param>
    /// <returns>The response.</returns>
    public static InputResponse From(InputArtifact input) => new()
    {
        Id = input.Id,
        ProjectId = input.ProjectId,
        InputSha256 = input.InputSha256,
        PointCount = input.PointCount,
        UploadedAtUtc = input.UploadedAtUtc,
    };
}
