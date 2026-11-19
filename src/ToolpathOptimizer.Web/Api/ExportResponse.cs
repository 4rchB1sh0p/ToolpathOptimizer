using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Web.Api;

/// <summary>Project export as returned by <c>GET /api/v1/projects/{projectId}/export</c>.</summary>
public sealed class ExportResponse
{
    /// <summary>Gets the exported project.</summary>
    public ProjectResponse Project { get; init; } = new();

    /// <summary>Gets the project's inputs in upload order.</summary>
    public IReadOnlyList<InputResponse> Inputs { get; init; } = [];

    /// <summary>Gets the project's runs in creation order.</summary>
    public IReadOnlyList<RunResponse> Runs { get; init; } = [];

    /// <summary>Maps an export.</summary>
    /// <param name="export">Domain export.</param>
    /// <returns>The response.</returns>
    public static ExportResponse From(ProjectExport export) => new()
    {
        Project = ProjectResponse.From(export.Project),
        Inputs = export.Inputs.Select(InputResponse.From).ToArray(),
        Runs = export.Runs.Select(RunResponse.From).ToArray(),
    };
}
