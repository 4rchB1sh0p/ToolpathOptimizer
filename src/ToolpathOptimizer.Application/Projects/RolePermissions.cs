namespace ToolpathOptimizer.Application.Projects;

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
