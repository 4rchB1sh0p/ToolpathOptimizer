using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Infrastructure.Storage;

namespace ToolpathOptimizer.Infrastructure.IntegrationTests.Storage;

/// <summary>Tests for <see cref="LocalArtifactStore"/> against the real file system.</summary>
public sealed class LocalArtifactStoreTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly LocalArtifactStore _store;

    /// <summary>Creates a store rooted in a fresh temporary directory.</summary>
    public LocalArtifactStoreTests()
    {
        _store = new LocalArtifactStore(Options.Create(new ArtifactStorageOptions { RootPath = _temp.Path }));
    }

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task GetAsync_StoredKeySameTenant_ReturnsBytes()
    {
        await _store.PutAsync("tenant-a", "projects/p1/report.json", new byte[] { 7 }, CancellationToken.None);

        Result<byte[]> stored = await _store.GetAsync("tenant-a", "projects/p1/report.json", CancellationToken.None);

        Assert.Equal([7], stored.Value);
    }

    [Fact]
    public async Task GetAsync_StoredKeyOtherTenant_ReturnsNotFound()
    {
        await _store.PutAsync("tenant-a", "projects/p1/report.json", new byte[] { 7 }, CancellationToken.None);

        Result<byte[]> stored = await _store.GetAsync("tenant-b", "projects/p1/report.json", CancellationToken.None);

        Assert.Equal(ErrorCodes.ResourceNotFound, stored.Outcome.Errors[0].Code);
    }

    [Fact]
    public async Task PutAsync_ExistingKey_Throws()
    {
        await _store.PutAsync("tenant-a", "k/a.json", new byte[] { 1 }, CancellationToken.None);

        await Assert.ThrowsAsync<IOException>(() => _store.PutAsync("tenant-a", "k/a.json", new byte[] { 2 }, CancellationToken.None));
    }

    [Theory]
    [InlineData("", "k/a.json")]
    [InlineData("tenant-a", "../escape.json")]
    [InlineData("tenant-a", "k/..")]
    [InlineData("../t", "k/a.json")]
    public async Task GetAsync_MissingTenantOrUnsafeKey_Throws(string tenantId, string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _store.GetAsync(tenantId, key, CancellationToken.None));
    }

    [Fact]
    public async Task DeletePrefixAsync_OneProject_RemovesItsArtifacts()
    {
        await _store.PutAsync("tenant-a", "projects/p1/a.json", new byte[] { 1 }, CancellationToken.None);

        await _store.DeletePrefixAsync("tenant-a", "projects/p1/", CancellationToken.None);

        Assert.False((await _store.GetAsync("tenant-a", "projects/p1/a.json", CancellationToken.None)).IsSuccess, "The deleted artifact must not be found.");
    }

    [Fact]
    public async Task DeletePrefixAsync_OneProject_KeepsOtherProjects()
    {
        await _store.PutAsync("tenant-a", "projects/p1/a.json", new byte[] { 1 }, CancellationToken.None);
        await _store.PutAsync("tenant-a", "projects/p2/a.json", new byte[] { 2 }, CancellationToken.None);

        await _store.DeletePrefixAsync("tenant-a", "projects/p1/", CancellationToken.None);

        Assert.True((await _store.GetAsync("tenant-a", "projects/p2/a.json", CancellationToken.None)).IsSuccess, "Other projects must be untouched.");
    }
}
