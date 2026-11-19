using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Web.Api;

/// <summary>Project as returned by the API (tenant and owner are not exposed).</summary>
public sealed class ProjectResponse
{
    /// <summary>Gets the identifier used in project routes.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Maps a project.</summary>
    /// <param name="project">Domain project.</param>
    /// <returns>The response.</returns>
    public static ProjectResponse From(Project project) => new() { Id = project.Id, Name = project.Name, CreatedAtUtc = project.CreatedAtUtc };
}
