namespace ToolpathOptimizer.Web.Api;

/// <summary>Body of <c>POST /api/v1/projects</c>. Any tenant field in the body is ignored.</summary>
public sealed class CreateProjectRequest
{
    /// <summary>Gets or sets the display name (1–100 printable characters).</summary>
    public string Name { get; set; } = string.Empty;
}
