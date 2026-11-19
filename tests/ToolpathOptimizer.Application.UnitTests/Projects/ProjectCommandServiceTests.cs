using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Projects;
using ToolpathOptimizer.Application.UnitTests.Support;

namespace ToolpathOptimizer.Application.UnitTests.Projects;

/// <summary>Tests for <see cref="ProjectCommandService"/>.</summary>
public sealed class ProjectCommandServiceTests
{
    private readonly ProjectServices _services = new();

    [Fact]
    public void CreateProject_Engineer_RecordsAuditEvent()
    {
        _services.Commands.CreateProject("Plate");

        Assert.Contains(_services.AuditLog.Events, auditEvent => auditEvent.Action == "project.created");
    }

    [Fact]
    public void CreateProject_Unauthenticated_ReturnsUnauthenticated()
    {
        _services.Principal.IsAuthenticated = false;

        Assert.Equal(ErrorCodes.Unauthenticated, _services.Commands.CreateProject("x").Outcome.Errors[0].Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad\u0001name")]
    public void CreateProject_InvalidName_ReturnsInvalidParameter(string name)
    {
        Assert.Equal(ErrorCodes.InputInvalidParameter, _services.Commands.CreateProject(name).Outcome.Errors[0].Code);
    }

    [Fact]
    public async Task DeleteProject_Engineer_IsForbidden()
    {
        (Project project, _) = await _services.SeedProjectWithInputAsync();

        OperationOutcome outcome = await _services.Commands.DeleteProjectAsync(project.Id, CancellationToken.None);

        Assert.Equal(OutcomeCategory.Forbidden, outcome.Category);
    }

    [Fact]
    public async Task DeleteProject_Owner_RemovesProject()
    {
        (Project project, _) = await _services.SeedProjectWithInputAsync();
        _services.Principal.Role = Role.Owner;

        await _services.Commands.DeleteProjectAsync(project.Id, CancellationToken.None);

        Assert.False(_services.Repository.FindProject(_services.Principal.TenantId, project.Id).IsSuccess, "The project should no longer be found.");
    }

    [Fact]
    public async Task DeleteProject_Owner_RemovesStoredArtifacts()
    {
        (Project project, InputArtifact input) = await _services.SeedProjectWithInputAsync();
        _services.Principal.Role = Role.Owner;

        await _services.Commands.DeleteProjectAsync(project.Id, CancellationToken.None);

        Assert.False((await _services.Artifacts.GetAsync(input.TenantId, input.PointsKey, CancellationToken.None)).IsSuccess, "Stored inputs should be deleted.");
    }
}
