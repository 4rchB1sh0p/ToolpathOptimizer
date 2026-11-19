namespace ToolpathOptimizer.Cli.IntegrationTests.Support;

/// <summary>A unique temporary output directory, deleted on dispose.</summary>
internal sealed class OutputDirectory : IDisposable
{
    /// <summary>Creates the directory under the system temp path.</summary>
    public OutputDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tpo-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Gets the directory path.</summary>
    public string Path { get; }

    /// <summary>Gets the files currently in the directory.</summary>
    public string[] Files => Directory.GetFiles(Path);

    /// <summary>Builds the path of a file inside the directory.</summary>
    /// <param name="name">File name.</param>
    /// <returns>The full path.</returns>
    public string PathOf(string name) => System.IO.Path.Combine(Path, name);

    /// <summary>Writes a text file and returns its path.</summary>
    /// <param name="name">File name.</param>
    /// <param name="content">File content.</param>
    /// <returns>The full path.</returns>
    public string WriteFile(string name, string content)
    {
        string path = PathOf(name);
        File.WriteAllText(path, content);
        return path;
    }

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
