using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Projects;
using ToolpathOptimizer.Application.UnitTests.Support;

namespace ToolpathOptimizer.Application.UnitTests.Projects;

/// <summary>Tests for <see cref="InputUploadService"/>.</summary>
public sealed class InputUploadServiceTests
{
    private readonly ProjectServices _services = new();

    [Fact]
    public async Task Upload_ValidInput_StoresArtifactBytes()
    {
        (_, InputArtifact input) = await _services.SeedProjectWithInputAsync();

        Assert.True((await _services.Artifacts.GetAsync(input.TenantId, input.ManifestKey, CancellationToken.None)).IsSuccess, "The manifest should be stored.");
    }

    [Fact]
    public async Task Upload_InvalidInput_ReturnsInputCategory()
    {
        Project project = _services.Commands.CreateProject("Plate").Value;

        Result<InputArtifact> result = await _services.Uploads.UploadAsync(project.Id, TestInputs.CreateJob("bad"), CancellationToken.None);

        Assert.Equal(OutcomeCategory.Input, result.Outcome.Category);
    }

    [Fact]
    public async Task Upload_InvalidInput_StoresNothing()
    {
        Project project = _services.Commands.CreateProject("Plate").Value;

        await _services.Uploads.UploadAsync(project.Id, TestInputs.CreateJob("bad"), CancellationToken.None);

        Assert.Empty(_services.Repository.ListInputs(_services.Principal.TenantId, project.Id));
    }

    [Theory]
    [InlineData(Role.Viewer)]
    [InlineData(Role.Reviewer)]
    public async Task Upload_ReadOnlyRole_ReturnsAccessDenied(Role role)
    {
        (Project project, _) = await _services.SeedProjectWithInputAsync();
        _services.Principal.Role = role;

        Result<InputArtifact> result = await _services.Uploads.UploadAsync(project.Id, TestInputs.CreateJob(TestInputs.SquareCsv), CancellationToken.None);

        Assert.Equal(ErrorCodes.AccessDenied, result.Outcome.Errors[0].Code);
    }
}
