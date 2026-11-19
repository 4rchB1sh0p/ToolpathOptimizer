namespace ToolpathOptimizer.Application.Ports;

/// <summary>
/// Detects whether two paths refer to the same file (full path, links and file identity).
/// </summary>
public interface IFileAliasDetector
{
    /// <summary>Determines whether two paths alias the same file.</summary>
    /// <param name="firstPath">First path.</param>
    /// <param name="secondPath">Second path.</param>
    /// <returns><c>true</c> when both resolve to the same file, or when that cannot be ruled out.</returns>
    bool AreSameFile(string firstPath, string secondPath);
}
