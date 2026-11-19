namespace ToolpathOptimizer.Infrastructure.IntegrationTests;

/// <summary>A unique temporary directory per test, deleted on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
    /// <summary>Creates the directory under the system temp path.</summary>
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tpo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Gets the directory path.</summary>
    public string Path { get; }

    /// <summary>Builds the path of a file inside the directory.</summary>
    /// <param name="name">File name.</param>
    /// <returns>The full path.</returns>
    public string PathOf(string name) => System.IO.Path.Combine(Path, name);

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup; the directory name is unique.
        }
    }
}
