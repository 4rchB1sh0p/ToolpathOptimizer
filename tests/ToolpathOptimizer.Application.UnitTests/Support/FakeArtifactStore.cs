using ToolpathOptimizer.Application.Outcomes;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Application.UnitTests.Support;

/// <summary>In-memory artifact store keyed by <c>tenant/key</c>.</summary>
internal sealed class FakeArtifactStore : IArtifactStore
{
    private readonly Dictionary<string, byte[]> _items = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task PutAsync(string tenantId, string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        _items.Add($"{tenantId}/{key}", content.ToArray());
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Result<byte[]>> GetAsync(string tenantId, string key, CancellationToken cancellationToken) =>
        Task.FromResult(_items.TryGetValue($"{tenantId}/{key}", out byte[]? content) ? Result<byte[]>.Success(content) : Result<byte[]>.NotFound("Artifact"));

    /// <inheritdoc />
    public Task DeletePrefixAsync(string tenantId, string prefix, CancellationToken cancellationToken)
    {
        foreach (string key in _items.Keys.Where(k => k.StartsWith($"{tenantId}/{prefix}", StringComparison.Ordinal)).ToArray())
        {
            _items.Remove(key);
        }

        return Task.CompletedTask;
    }
}
