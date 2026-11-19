using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Application.UnitTests.Projects;

/// <summary>Tests for <see cref="RolePermissions"/> against the matrix in web-and-tenancy.md.</summary>
public sealed class RolePermissionsTests
{
    /// <summary>Gets every (role, action) pair with its expected permission, one row per cell of the matrix.</summary>
    public static TheoryData<Role, ProjectAction, bool> Matrix { get; } = CreateMatrix();

    [Theory]
    [MemberData(nameof(Matrix))]
    public void IsAllowed_RoleAndAction_MatchesMatrixCell(Role role, ProjectAction action, bool isExpectedAllowed)
    {
        Assert.Equal(isExpectedAllowed, RolePermissions.IsAllowed(role, action));
    }

    private static TheoryData<Role, ProjectAction, bool> CreateMatrix()
    {
        // Columns: Owner, Engineer, Reviewer, Viewer.
        var allowedByAction = new Dictionary<ProjectAction, bool[]>
        {
            [ProjectAction.Read] = [true, true, true, true],
            [ProjectAction.CreateProject] = [true, true, false, false],
            [ProjectAction.UploadInput] = [true, true, false, false],
            [ProjectAction.RunAnalysis] = [true, true, false, false],
            [ProjectAction.Annotate] = [true, true, true, false],
            [ProjectAction.DeleteProject] = [true, false, false, false],
            [ProjectAction.ExportProject] = [true, false, false, false],
            [ProjectAction.ViewAudit] = [true, false, false, false],
        };
        Role[] columns = [Role.Owner, Role.Engineer, Role.Reviewer, Role.Viewer];
        var matrix = new TheoryData<Role, ProjectAction, bool>();
        foreach ((ProjectAction action, bool[] allowed) in allowedByAction)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                matrix.Add(columns[i], action, allowed[i]);
            }
        }

        return matrix;
    }
}
