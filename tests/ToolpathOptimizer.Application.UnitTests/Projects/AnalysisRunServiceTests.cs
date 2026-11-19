using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Projects;
using ToolpathOptimizer.Application.UnitTests.Support;

namespace ToolpathOptimizer.Application.UnitTests.Projects;

/// <summary>Tests for <see cref="AnalysisRunService"/>.</summary>
public sealed class AnalysisRunServiceTests
{
    private readonly ProjectServices _services = new();

    [Fact]
    public async Task Run_ValidInput_Succeeds()
    {
        (Project project, InputArtifact input) = await _services.SeedProjectWithInputAsync();

        Result<AnalysisRun> run = await _services.Runs.RunAsync(project.Id, input.Id, "key-1", CancellationToken.None);

        Assert.Equal(RunStatus.Succeeded, run.Value.Status);
    }

    [Fact]
    public async Task Run_ValidInput_RecordsAuditEvent()
    {
        (Project project, InputArtifact input) = await _services.SeedProjectWithInputAsync();

        await _services.Runs.RunAsync(project.Id, input.Id, string.Empty, CancellationToken.None);

        Assert.Contains(_services.AuditLog.Events, auditEvent => auditEvent.Action == "analysis.run");
    }

    [Fact]
    public async Task Run_RepeatedIdempotencyKey_ReturnsSameRun()
    {
        (Project project, InputArtifact input) = await _services.SeedProjectWithInputAsync();

        AnalysisRun first = (await _services.Runs.RunAsync(project.Id, input.Id, "same", CancellationToken.None)).Value;
        AnalysisRun second = (await _services.Runs.RunAsync(project.Id, input.Id, "same", CancellationToken.None)).Value;

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task Run_KeyReusedForOtherInput_ReturnsConflict()
    {
        (Project project, InputArtifact input) = await _services.SeedProjectWithInputAsync();
        InputArtifact other = (await _services.Uploads.UploadAsync(project.Id, TestInputs.CreateJob(TestInputs.SquareCsv), CancellationToken.None)).Value;
        await _services.Runs.RunAsync(project.Id, input.Id, "same", CancellationToken.None);

        Result<AnalysisRun> conflict = await _services.Runs.RunAsync(project.Id, other.Id, "same", CancellationToken.None);

        Assert.Equal(ErrorCodes.IdempotencyConflict, conflict.Outcome.Errors[0].Code);
    }

    [Fact]
    public async Task Run_OtherTenant_ReturnsNotFound()
    {
        (Project project, InputArtifact input) = await _services.SeedProjectWithInputAsync();
        _services.Principal.TenantId = "tenant-b";

        Result<AnalysisRun> run = await _services.Runs.RunAsync(project.Id, input.Id, string.Empty, CancellationToken.None);

        Assert.Equal(ErrorCodes.ResourceNotFound, run.Outcome.Errors[0].Code);
    }

    [Theory]
    [InlineData(Role.Viewer)]
    [InlineData(Role.Reviewer)]
    public async Task Run_ReadOnlyRole_ReturnsAccessDenied(Role role)
    {
        (Project project, InputArtifact input) = await _services.SeedProjectWithInputAsync();
        _services.Principal.Role = role;

        Result<AnalysisRun> run = await _services.Runs.RunAsync(project.Id, input.Id, string.Empty, CancellationToken.None);

        Assert.Equal(ErrorCodes.AccessDenied, run.Outcome.Errors[0].Code);
    }
}
