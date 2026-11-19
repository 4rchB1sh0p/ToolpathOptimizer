using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Application.UnitTests.Projects;

public sealed class RolePermissionsTests
{
    // Matrix from web-and-tenancy.md: Owner, Engineer, Reviewer, Viewer.
    [Theory]
    [InlineData(ProjectAction.Read, true, true, true, true)]
    [InlineData(ProjectAction.CreateProject, true, true, false, false)]
    [InlineData(ProjectAction.UploadInput, true, true, false, false)]
    [InlineData(ProjectAction.RunAnalysis, true, true, false, false)]
    [InlineData(ProjectAction.Annotate, true, true, true, false)]
    [InlineData(ProjectAction.DeleteProject, true, false, false, false)]
    [InlineData(ProjectAction.ExportProject, true, false, false, false)]
    [InlineData(ProjectAction.ViewAudit, true, false, false, false)]
    public void IsAllowed_MatchesMatrix(ProjectAction action, bool owner, bool engineer, bool reviewer, bool viewer)
    {
        Assert.Equal(owner, RolePermissions.IsAllowed(Role.Owner, action));
        Assert.Equal(engineer, RolePermissions.IsAllowed(Role.Engineer, action));
        Assert.Equal(reviewer, RolePermissions.IsAllowed(Role.Reviewer, action));
        Assert.Equal(viewer, RolePermissions.IsAllowed(Role.Viewer, action));
    }
}
