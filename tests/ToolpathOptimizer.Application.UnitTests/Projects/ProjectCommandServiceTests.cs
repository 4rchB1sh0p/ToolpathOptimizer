using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Application.UnitTests.Projects;

public sealed class ProjectCommandServiceTests
{
    private readonly FakePrincipal _principal = new();
    private readonly FakeProjectRepository _repository = new();
    private readonly FakeArtifactStore _artifacts = new();
    private readonly RecordingAuditLog _audit = new();
    private readonly Handlers _handlers = new();

    private ProjectCommandService Commands()
    {
        var access = new ProjectAccess(_principal, _principal, _repository);
        return new ProjectCommandService(access, _repository, _artifacts, new AllowAllEntitlements(), _audit, _handlers.Validate, _handlers.Analyze, _handlers.Clock);
    }

    private ProjectQueryService Queries() => new(new ProjectAccess(_principal, _principal, _repository), _repository, _artifacts);

    private async Task<(Project Project, InputArtifact Input)> SeedAsync()
    {
        Project project = Commands().CreateProject("Plate").Value;
        InputArtifact input = (await Commands().UploadInputAsync(project.Id, Inputs.Job(Inputs.CrossingCsv), CancellationToken.None)).Value;
        return (project, input);
    }

    [Fact]
    public async Task RunAnalysis_StoresReportReadableByQuery()
    {
        (Project project, InputArtifact input) = await SeedAsync();

        Result<AnalysisRun> run = await Commands().RunAnalysisAsync(project.Id, input.Id, "key-1", CancellationToken.None);
        Result<byte[]> report = await Queries().GetReportAsync(project.Id, run.Value.Id, CancellationToken.None);

        Assert.Equal(RunStatus.Succeeded, run.Value.Status);
        Assert.True(report.IsSuccess);
        Assert.Contains(_audit.Events, e => e.Action == "analysis.run");
    }

    [Fact]
    public async Task RunAnalysis_RepeatedIdempotencyKeyReturnsSameRun()
    {
        (Project project, InputArtifact input) = await SeedAsync();

        AnalysisRun first = (await Commands().RunAnalysisAsync(project.Id, input.Id, "same", CancellationToken.None)).Value;
        AnalysisRun second = (await Commands().RunAnalysisAsync(project.Id, input.Id, "same", CancellationToken.None)).Value;

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task RunAnalysis_KeyReusedForOtherInputConflicts()
    {
        (Project project, InputArtifact input) = await SeedAsync();
        InputArtifact other = (await Commands().UploadInputAsync(project.Id, Inputs.Job(Inputs.SquareCsv), CancellationToken.None)).Value;
        await Commands().RunAnalysisAsync(project.Id, input.Id, "same", CancellationToken.None);

        Result<AnalysisRun> conflict = await Commands().RunAnalysisAsync(project.Id, other.Id, "same", CancellationToken.None);

        Assert.Equal(ErrorCodes.IdempotencyConflict, conflict.Outcome.Errors[0].Code);
    }

    [Fact]
    public async Task UploadInput_RejectsInvalidInputWithoutStoring()
    {
        Project project = Commands().CreateProject("Plate").Value;

        Result<InputArtifact> result = await Commands().UploadInputAsync(project.Id, Inputs.Job("bad"), CancellationToken.None);

        Assert.Equal(OutcomeCategory.Input, result.Outcome.Category);
        Assert.Empty(_repository.ListInputs(_principal.TenantId, project.Id));
    }

    [Fact]
    public async Task OtherTenant_SeesNotFound()
    {
        (Project project, InputArtifact input) = await SeedAsync();
        _principal.TenantId = "tenant-b";

        Result<AnalysisRun> run = await Commands().RunAnalysisAsync(project.Id, input.Id, string.Empty, CancellationToken.None);

        Assert.Equal(ErrorCodes.ResourceNotFound, run.Outcome.Errors[0].Code);
        Assert.Empty(Queries().ListProjects().Value);
    }

    [Theory]
    [InlineData(Role.Viewer)]
    [InlineData(Role.Reviewer)]
    public async Task ReadOnlyRoles_CannotUploadOrRun(Role role)
    {
        (Project project, InputArtifact input) = await SeedAsync();
        _principal.Role = role;

        Result<InputArtifact> upload = await Commands().UploadInputAsync(project.Id, Inputs.Job(Inputs.SquareCsv), CancellationToken.None);
        Result<AnalysisRun> run = await Commands().RunAnalysisAsync(project.Id, input.Id, string.Empty, CancellationToken.None);

        Assert.Equal(ErrorCodes.AccessDenied, upload.Outcome.Errors[0].Code);
        Assert.Equal(ErrorCodes.AccessDenied, run.Outcome.Errors[0].Code);
    }

    [Fact]
    public async Task DeleteProject_RequiresOwnerAndRemovesEverything()
    {
        (Project project, _) = await SeedAsync();

        OperationOutcome denied = await Commands().DeleteProjectAsync(project.Id, CancellationToken.None);
        _principal.Role = Role.Owner;
        OperationOutcome deleted = await Commands().DeleteProjectAsync(project.Id, CancellationToken.None);

        Assert.Equal(OutcomeCategory.Forbidden, denied.Category);
        Assert.True(deleted.IsSuccess);
        Assert.Null(_repository.FindProject(_principal.TenantId, project.Id));
    }

    [Fact]
    public void CreateProject_RequiresAuthentication()
    {
        _principal.IsAuthenticated = false;

        Assert.Equal(ErrorCodes.Unauthenticated, Commands().CreateProject("x").Outcome.Errors[0].Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad\u0001name")]
    public void CreateProject_RejectsInvalidName(string name)
    {
        Assert.Equal(ErrorCodes.InputInvalidParameter, Commands().CreateProject(name).Outcome.Errors[0].Code);
    }
}
