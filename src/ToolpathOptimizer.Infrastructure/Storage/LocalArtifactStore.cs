using Microsoft.Extensions.Options;
using ToolpathOptimizer.Application.Ports;
using ToolpathOptimizer.Core.Domain;

namespace ToolpathOptimizer.Infrastructure.Storage;

/// <summary>
/// Options for <see cref="LocalArtifactStore"/>.
/// </summary>
public sealed class ArtifactStorageOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ArtifactStorage";

    /// <summary>Gets or sets the root directory (development only; object storage later).</summary>
    public string RootPath { get; set; } = "App_Data/artifacts";
}

/// <summary>
/// Development artifact store on the local file system. Keys are <c>{tenant}/{key}</c>; every segment must be a
/// safe identifier, so path traversal and cross-tenant access are impossible by construction.
/// </summary>
public sealed class LocalArtifactStore : IArtifactStore
{
    private readonly string _root;

    /// <summary>Creates the store.</summary>
    /// <param name="options">Storage options.</param>
    public LocalArtifactStore(IOptions<ArtifactStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _root = Path.GetFullPath(options.Value.RootPath);
    }

    /// <inheritdoc />
    public async Task PutAsync(string tenantId, string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        string path = PathFor(tenantId, key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
        stream.Flush(flushToDisk: true);
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetAsync(string tenantId, string key, CancellationToken cancellationToken)
    {
        string path = PathFor(tenantId, key);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false) : null;
    }

    /// <inheritdoc />
    public Task DeletePrefixAsync(string tenantId, string prefix, CancellationToken cancellationToken)
    {
        string path = PathFor(tenantId, prefix.TrimEnd('/'));
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
        else if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string PathFor(string tenantId, string key)
    {
        if (!IdentifierRules.IsValid(tenantId))
        {
            throw new ArgumentException("A valid tenant identifier is required for every storage call.", nameof(tenantId));
        }

        string[] segments = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || !segments.All(IsSafeSegment))
        {
            throw new ArgumentException("Storage keys must consist of safe segments.", nameof(key));
        }

        return Path.Combine([_root, tenantId, .. segments]);
    }

    private static bool IsSafeSegment(string segment) =>
        IdentifierRules.IsValid(segment) && !segment.Contains("..", StringComparison.Ordinal);
}
