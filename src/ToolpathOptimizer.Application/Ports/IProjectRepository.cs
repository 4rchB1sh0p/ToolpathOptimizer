using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Projects;

namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// Tenant-scoped persistence of projects, input artifacts and analysis runs. Every method filters by tenant;
/// lookups return <c>RESOURCE_NOT_FOUND</c> for resources of other tenants.
/// </summary>
public interface IProjectRepository
{
    /// <summary>Adds a project.</summary>
    /// <param name="project">Project to add; its tenant scopes all later lookups.</param>
    void AddProject(Project project);

    /// <summary>Finds a project.</summary>
    /// <param name="tenantId">Caller's tenant.</param>
    /// <param name="projectId">Project to find.</param>
    /// <returns>The project, or <c>RESOURCE_NOT_FOUND</c>.</returns>
    Result<Project> FindProject(string tenantId, Guid projectId);

    /// <summary>Lists a tenant's projects ordered by creation time.</summary>
    /// <param name="tenantId">Caller's tenant.</param>
    /// <returns>The projects.</returns>
    IReadOnlyList<Project> ListProjects(string tenantId);

    /// <summary>Deletes a project with its inputs and runs.</summary>
    /// <param name="tenantId">Caller's tenant.</param>
    /// <param name="projectId">Project to delete.</param>
    void DeleteProject(string tenantId, Guid projectId);

    /// <summary>Adds an input artifact.</summary>
    /// <param name="input">Validated input to add.</param>
    void AddInput(InputArtifact input);

    /// <summary>Finds an input artifact.</summary>
    /// <param name="tenantId">Caller's tenant.</param>
    /// <param name="projectId">Owning project.</param>
    /// <param name="inputId">Input to find.</param>
    /// <returns>The input, or <c>RESOURCE_NOT_FOUND</c>.</returns>
    Result<InputArtifact> FindInput(string tenantId, Guid projectId, Guid inputId);

    /// <summary>Lists a project's inputs in upload order.</summary>
    /// <param name="tenantId">Caller's tenant.</param>
    /// <param name="projectId">Owning project.</param>
    /// <returns>The inputs.</returns>
    IReadOnlyList<InputArtifact> ListInputs(string tenantId, Guid projectId);

    /// <summary>
    /// Adds a run unless one with the same idempotency key exists, atomically. Deliberately both a command and a
    /// query (CQS exception, D-030): a separate check-then-add would race.
    /// </summary>
    /// <param name="run">Run to add.</param>
    /// <returns>The stored run: the existing one when the key was already used.</returns>
    AnalysisRun AddRunIfAbsent(AnalysisRun run);

    /// <summary>Finds a run by idempotency key.</summary>
    /// <param name="tenantId">Caller's tenant.</param>
    /// <param name="projectId">Owning project.</param>
    /// <param name="idempotencyKey">Non-empty idempotency key.</param>
    /// <returns>The run, or <c>RESOURCE_NOT_FOUND</c>.</returns>
    Result<AnalysisRun> FindRunByIdempotencyKey(string tenantId, Guid projectId, string idempotencyKey);

    /// <summary>Finds a run.</summary>
    /// <param name="tenantId">Caller's tenant.</param>
    /// <param name="projectId">Owning project.</param>
    /// <param name="runId">Run to find.</param>
    /// <returns>The run, or <c>RESOURCE_NOT_FOUND</c>.</returns>
    Result<AnalysisRun> FindRun(string tenantId, Guid projectId, Guid runId);

    /// <summary>Lists a project's runs in creation order.</summary>
    /// <param name="tenantId">Caller's tenant.</param>
    /// <param name="projectId">Owning project.</param>
    /// <returns>The runs.</returns>
    IReadOnlyList<AnalysisRun> ListRuns(string tenantId, Guid projectId);
}
