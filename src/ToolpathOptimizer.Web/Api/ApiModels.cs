using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Web.Api;

/// <summary>Request body for creating a project.</summary>
public sealed class CreateProjectRequest
{
    /// <summary>Gets or sets the project name.</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>Request body for running an analysis.</summary>
public sealed class RunAnalysisRequest
{
    /// <summary>Gets or sets the input to analyse.</summary>
    public Guid InputId { get; set; }
}

/// <summary>Project response.</summary>
public sealed class ProjectResponse
{
    /// <summary>Gets the identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Maps a project.</summary>
    /// <param name="project">Project.</param>
    /// <returns>The response.</returns>
    public static ProjectResponse From(Project project) => new() { Id = project.Id, Name = project.Name, CreatedAtUtc = project.CreatedAtUtc };
}

/// <summary>Input artifact response.</summary>
public sealed class InputResponse
{
    /// <summary>Gets the identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the project.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>Gets the canonical input hash.</summary>
    public string InputSha256 { get; init; } = string.Empty;

    /// <summary>Gets the number of points.</summary>
    public int PointCount { get; init; }

    /// <summary>Gets the upload time.</summary>
    public DateTimeOffset UploadedAtUtc { get; init; }

    /// <summary>Maps an input.</summary>
    /// <param name="input">Input.</param>
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

/// <summary>Analysis run response.</summary>
public sealed class RunResponse
{
    /// <summary>Gets the identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the project.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>Gets the analysed input.</summary>
    public Guid InputId { get; init; }

    /// <summary>Gets the status (<c>succeeded</c> or <c>failed</c>).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Gets the failure code, or null.</summary>
    public string? OutcomeCode { get; init; }

    /// <summary>Gets the summary, or null.</summary>
    public AnalysisSummary? Summary { get; init; }

    /// <summary>Gets the report URL, or null.</summary>
    public string? ReportUrl { get; init; }

    /// <summary>Gets the creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Maps a run.</summary>
    /// <param name="run">Run.</param>
    /// <returns>The response.</returns>
    public static RunResponse From(AnalysisRun run) => new()
    {
        Id = run.Id,
        ProjectId = run.ProjectId,
        InputId = run.InputId,
        Status = run.Status == RunStatus.Succeeded ? "succeeded" : "failed",
        OutcomeCode = run.OutcomeCode.Length == 0 ? null : run.OutcomeCode,
        Summary = run.Summary,
        ReportUrl = run.Status == RunStatus.Succeeded ? $"/api/v1/projects/{run.ProjectId}/analyses/{run.Id}/report" : null,
        CreatedAtUtc = run.CreatedAtUtc,
    };
}

/// <summary>Project export response.</summary>
public sealed class ExportResponse
{
    /// <summary>Gets the project.</summary>
    public ProjectResponse Project { get; init; } = new();

    /// <summary>Gets the inputs.</summary>
    public IReadOnlyList<InputResponse> Inputs { get; init; } = [];

    /// <summary>Gets the runs.</summary>
    public IReadOnlyList<RunResponse> Runs { get; init; } = [];
}
