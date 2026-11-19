namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Actions subject to the role permission matrix.
/// </summary>
public enum ProjectAction
{
    /// <summary>Read projects, runs and reports.</summary>
    Read,

    /// <summary>Create a project.</summary>
    CreateProject,

    /// <summary>Upload an input.</summary>
    UploadInput,

    /// <summary>Run an analysis.</summary>
    RunAnalysis,

    /// <summary>Annotate or mark a run reviewed (advisory).</summary>
    Annotate,

    /// <summary>Delete a project.</summary>
    DeleteProject,

    /// <summary>Export a project.</summary>
    ExportProject,

    /// <summary>View audit events.</summary>
    ViewAudit,
}
