using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Application.UnitTests.Support;

/// <summary>Project services wired with fakes that share one principal, repository and artifact store.</summary>
internal sealed class ProjectServices
{
    /// <summary>Creates the services.</summary>
    public ProjectServices()
    {
        var handlers = new TestHandlers();
        var access = new ProjectAccess(Principal, Principal, Repository);
        var auditor = new ProjectAuditor(AuditLog, access, handlers.Clock);
        var artifacts = new ProjectArtifacts(Artifacts);
        Commands = new ProjectCommandService(access, Repository, artifacts, auditor, handlers.Clock);
        Uploads = new InputUploadService(access, Repository, artifacts, new AllowAllEntitlements(), auditor, handlers.Validate, handlers.Clock);
        Runs = new AnalysisRunService(access, Repository, artifacts, auditor, handlers.Analyze, handlers.Clock);
        Queries = new ProjectQueryService(access, Repository, artifacts);
    }

    /// <summary>Gets the caller identity; change it to act as another user or tenant.</summary>
    public FakePrincipal Principal { get; } = new();

    /// <summary>Gets the shared repository.</summary>
    public FakeProjectRepository Repository { get; } = new();

    /// <summary>Gets the shared artifact store.</summary>
    public FakeArtifactStore Artifacts { get; } = new();

    /// <summary>Gets the recorded audit events.</summary>
    public RecordingAuditLog AuditLog { get; } = new();

    /// <summary>Gets the project command service.</summary>
    public ProjectCommandService Commands { get; }

    /// <summary>Gets the upload service.</summary>
    public InputUploadService Uploads { get; }

    /// <summary>Gets the analysis run service.</summary>
    public AnalysisRunService Runs { get; }

    /// <summary>Gets the query service.</summary>
    public ProjectQueryService Queries { get; }

    /// <summary>Creates a project and uploads the crossing sample as the current user.</summary>
    /// <returns>The project and input.</returns>
    public async Task<(Project Project, InputArtifact Input)> SeedProjectWithInputAsync()
    {
        Project project = Commands.CreateProject("Plate").Value;
        InputArtifact input = (await Uploads.UploadAsync(project.Id, TestInputs.CreateJob(TestInputs.CrossingCsv), CancellationToken.None)).Value;
        return (project, input);
    }
}
