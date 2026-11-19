using ToolpathOptimizer.Application.Analysis;
using ToolpathOptimizer.Application.Projects;
using ToolpathOptimizer.Infrastructure.Persistence;

namespace ToolpathOptimizer.Infrastructure.IntegrationTests.Persistence;

/// <summary>Tests for <see cref="InMemoryProjectRepository"/>.</summary>
public sealed class InMemoryProjectRepositoryTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 19, 0, 0, 0, TimeSpan.Zero);
    private static readonly AnalysisSummary _summary = new("hash", 1, "baseline", 0, 0, null, []);
    private readonly InMemoryProjectRepository _repository = new();
    private readonly InputArtifact _input = new(Guid.NewGuid(), Guid.NewGuid(), "tenant-a", "hash", 1, "u", _now);

    private AnalysisRun CreateRun(string idempotencyKey) =>
        AnalysisRun.Succeeded(Guid.NewGuid(), new RunOrigin(_input, idempotencyKey, "u", _now), _summary);

    [Fact]
    public void FindProject_SameTenant_ReturnsProject()
    {
        var project = new Project(Guid.NewGuid(), "tenant-a", "P", "u", _now);
        _repository.AddProject(project);

        Assert.Equal(project.Id, _repository.FindProject("tenant-a", project.Id).Value.Id);
    }

    [Fact]
    public void FindProject_OtherTenant_ReturnsNotFound()
    {
        var project = new Project(Guid.NewGuid(), "tenant-a", "P", "u", _now);
        _repository.AddProject(project);

        Assert.False(_repository.FindProject("tenant-b", project.Id).IsSuccess, "Another tenant must not see the project.");
    }

    [Fact]
    public void AddRunIfAbsent_SameKeyTwice_ReturnsFirstRun()
    {
        AnalysisRun first = CreateRun("k");
        _repository.AddRunIfAbsent(first);

        Assert.Equal(first.Id, _repository.AddRunIfAbsent(CreateRun("k")).Id);
    }

    [Fact]
    public void AddRunIfAbsent_SameKeyTwice_StoresOneRun()
    {
        _repository.AddRunIfAbsent(CreateRun("k"));
        _repository.AddRunIfAbsent(CreateRun("k"));

        Assert.Single(_repository.ListRuns("tenant-a", _input.ProjectId));
    }

    [Fact]
    public void AddRunIfAbsent_NoKey_StoresEveryRun()
    {
        _repository.AddRunIfAbsent(CreateRun(string.Empty));
        _repository.AddRunIfAbsent(CreateRun(string.Empty));

        Assert.Equal(2, _repository.ListRuns("tenant-a", _input.ProjectId).Count);
    }

    [Fact]
    public void FindRunByIdempotencyKey_EmptyKey_ReturnsNotFound()
    {
        _repository.AddRunIfAbsent(CreateRun(string.Empty));

        Assert.False(_repository.FindRunByIdempotencyKey("tenant-a", _input.ProjectId, string.Empty).IsSuccess, "An empty key must never match.");
    }
}
