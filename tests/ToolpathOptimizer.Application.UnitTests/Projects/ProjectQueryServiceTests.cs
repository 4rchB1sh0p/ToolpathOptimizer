using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Projects;
using ToolpathOptimizer.Application.UnitTests.Support;

namespace ToolpathOptimizer.Application.UnitTests.Projects;

/// <summary>Tests for <see cref="ProjectQueryService"/>.</summary>
public sealed class ProjectQueryServiceTests
{
    private readonly ProjectServices _services = new();

    [Fact]
    public async Task GetReport_SucceededRun_ReturnsStoredBytes()
    {
        (Project project, InputArtifact input) = await _services.SeedProjectWithInputAsync();
        AnalysisRun run = (await _services.Runs.RunAsync(project.Id, input.Id, string.Empty, CancellationToken.None)).Value;

        Result<byte[]> report = await _services.Queries.GetReportAsync(project.Id, run.Id, CancellationToken.None);

        Assert.True(report.IsSuccess, report.IsSuccess ? string.Empty : report.Outcome.Errors[0].Code);
    }

    [Fact]
    public async Task ListProjects_OtherTenant_IsEmpty()
    {
        await _services.SeedProjectWithInputAsync();
        _services.Principal.TenantId = "tenant-b";

        Assert.Empty(_services.Queries.ListProjects().Value);
    }

    [Fact]
    public async Task ExportProject_Engineer_ReturnsAccessDenied()
    {
        (Project project, _) = await _services.SeedProjectWithInputAsync();

        Assert.Equal(ErrorCodes.AccessDenied, _services.Queries.ExportProject(project.Id).Outcome.Errors[0].Code);
    }

    [Fact]
    public async Task ExportProject_Owner_IncludesInputs()
    {
        (Project project, _) = await _services.SeedProjectWithInputAsync();
        _services.Principal.Role = Role.Owner;

        Assert.Single(_services.Queries.ExportProject(project.Id).Value.Inputs);
    }
}
