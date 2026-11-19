namespace ToolpathOptimizer.Application.Projects;

/// <summary>
/// Tenant roles mapped from the identity provider (D-018).
/// </summary>
public enum Role
{
    /// <summary>Read only.</summary>
    Viewer,

    /// <summary>Read and annotate.</summary>
    Reviewer,

    /// <summary>Create projects, upload inputs and run analyses.</summary>
    Engineer,

    /// <summary>Everything, including delete, export and audit.</summary>
    Owner,
}

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

/// <summary>
/// The role permission matrix from web-and-tenancy.md, enforced server-side.
/// </summary>
public static class RolePermissions
{
    /// <summary>Determines whether a role may perform an action within its own tenant.</summary>
    /// <param name="role">Caller role.</param>
    /// <param name="action">Requested action.</param>
    /// <returns><c>true</c> when permitted.</returns>
    public static bool IsAllowed(Role role, ProjectAction action) => action switch
    {
        ProjectAction.Read => true,
        ProjectAction.CreateProject or ProjectAction.UploadInput or ProjectAction.RunAnalysis => role is Role.Owner or Role.Engineer,
        ProjectAction.Annotate => role is Role.Owner or Role.Engineer or Role.Reviewer,
        ProjectAction.DeleteProject or ProjectAction.ExportProject or ProjectAction.ViewAudit => role is Role.Owner,
        _ => false,
    };
}
