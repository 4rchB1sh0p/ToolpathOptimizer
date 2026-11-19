using Microsoft.Extensions.Options;
using ToolpathOptimizer.Infrastructure.Storage;

namespace ToolpathOptimizer.Infrastructure.IntegrationTests.Storage;

public sealed class LocalArtifactStoreTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly LocalArtifactStore _store;

    public LocalArtifactStoreTests()
    {
        _store = new LocalArtifactStore(Options.Create(new ArtifactStorageOptions { RootPath = _temp.Path }));
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task PutThenGet_RoundTripsWithinTenant()
    {
        await _store.PutAsync("tenant-a", "projects/p1/report.json", new byte[] { 7 }, CancellationToken.None);

        Assert.Equal([7], await _store.GetAsync("tenant-a", "projects/p1/report.json", CancellationToken.None));
    }

    [Fact]
    public async Task Get_IsolatesTenants()
    {
        await _store.PutAsync("tenant-a", "projects/p1/report.json", new byte[] { 7 }, CancellationToken.None);

        Assert.Null(await _store.GetAsync("tenant-b", "projects/p1/report.json", CancellationToken.None));
    }

    [Fact]
    public async Task Put_NeverOverwritesImmutableArtifact()
    {
        await _store.PutAsync("tenant-a", "k/a.json", new byte[] { 1 }, CancellationToken.None);

        await Assert.ThrowsAsync<IOException>(() => _store.PutAsync("tenant-a", "k/a.json", new byte[] { 2 }, CancellationToken.None));
    }

    [Theory]
    [InlineData("", "k/a.json")]
    [InlineData("tenant-a", "../escape.json")]
    [InlineData("tenant-a", "k/..")]
    [InlineData("../t", "k/a.json")]
    public async Task Calls_RejectMissingTenantOrUnsafeKeys(string tenant, string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _store.GetAsync(tenant, key, CancellationToken.None));
    }

    [Fact]
    public async Task DeletePrefix_RemovesOnlyThatProject()
    {
        await _store.PutAsync("tenant-a", "projects/p1/a.json", new byte[] { 1 }, CancellationToken.None);
        await _store.PutAsync("tenant-a", "projects/p2/a.json", new byte[] { 2 }, CancellationToken.None);

        await _store.DeletePrefixAsync("tenant-a", "projects/p1/", CancellationToken.None);

        Assert.Null(await _store.GetAsync("tenant-a", "projects/p1/a.json", CancellationToken.None));
        Assert.NotNull(await _store.GetAsync("tenant-a", "projects/p2/a.json", CancellationToken.None));
    }
}
