using ToolpathOptimizer.Application.Projects;
using ToolpathOptimizer.Infrastructure.Persistence;

namespace ToolpathOptimizer.Infrastructure.IntegrationTests.Persistence;

public sealed class InMemoryProjectRepositoryTests
{
    private static readonly DateTimeOffset s_now = new(2026, 11, 19, 0, 0, 0, TimeSpan.Zero);

    private static AnalysisRun Run(Guid projectId, string key) =>
        new(Guid.NewGuid(), projectId, "tenant-a", Guid.NewGuid(), key, RunStatus.Succeeded, string.Empty, null, "u", s_now);

    [Fact]
    public void FindProject_IsTenantScoped()
    {
        var repository = new InMemoryProjectRepository();
        var project = new Project(Guid.NewGuid(), "tenant-a", "P", "u", s_now);
        repository.AddProject(project);

        Assert.NotNull(repository.FindProject("tenant-a", project.Id));
        Assert.Null(repository.FindProject("tenant-b", project.Id));
        Assert.Empty(repository.ListProjects("tenant-b"));
    }

    [Fact]
    public void AddRunIfAbsent_ReturnsExistingForSameKey()
    {
        var repository = new InMemoryProjectRepository();
        Guid projectId = Guid.NewGuid();
        AnalysisRun first = Run(projectId, "k");

        repository.AddRunIfAbsent(first);
        AnalysisRun stored = repository.AddRunIfAbsent(Run(projectId, "k"));

        Assert.Equal(first.Id, stored.Id);
        Assert.Single(repository.ListRuns("tenant-a", projectId));
    }

    [Fact]
    public void AddRunIfAbsent_StoresRunsWithoutKeySeparately()
    {
        var repository = new InMemoryProjectRepository();
        Guid projectId = Guid.NewGuid();

        repository.AddRunIfAbsent(Run(projectId, string.Empty));
        repository.AddRunIfAbsent(Run(projectId, string.Empty));

        Assert.Equal(2, repository.ListRuns("tenant-a", projectId).Count);
    }
}
