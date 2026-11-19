using System.Runtime.InteropServices;
using ToolpathOptimizer.Infrastructure.Files;

namespace ToolpathOptimizer.Infrastructure.IntegrationTests.Files;

/// <summary>Tests for <see cref="FileAliasDetector"/> against the real file system.</summary>
public sealed partial class FileAliasDetectorTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly FileAliasDetector _detector = new();

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task AreSameFile_RelativeAndAbsoluteSpelling_ReturnsTrue()
    {
        await File.WriteAllTextAsync(_temp.PathOf("in.csv"), "x");
        string relativeSpelling = Path.Combine(_temp.Path, "sub", "..", "in.csv");

        Assert.True(_detector.AreSameFile(relativeSpelling, _temp.PathOf("in.csv")), "Equivalent spellings of one path must be detected.");
    }

    [Fact]
    public async Task AreSameFile_DifferentFiles_ReturnsFalse()
    {
        await File.WriteAllTextAsync(_temp.PathOf("a.csv"), "x");
        await File.WriteAllTextAsync(_temp.PathOf("b.csv"), "x");

        Assert.False(_detector.AreSameFile(_temp.PathOf("a.csv"), _temp.PathOf("b.csv")), "Distinct files must not be reported as aliases.");
    }

    [Fact]
    public void AreSameFile_NonExistingOutput_ReturnsFalse()
    {
        Assert.False(_detector.AreSameFile(_temp.PathOf("new.json"), _temp.PathOf("in.csv")), "A new output path cannot alias an input.");
    }

    [Fact]
    public async Task AreSameFile_HardLinkOnWindows_ReturnsTrue()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // Hard-link detection on Unix is open debt (TD-012).
        }

        await File.WriteAllTextAsync(_temp.PathOf("in.csv"), "x");
        Assert.True(CreateHardLink(_temp.PathOf("alias.json"), _temp.PathOf("in.csv"), IntPtr.Zero), "Test setup: creating the hard link failed.");

        Assert.True(_detector.AreSameFile(_temp.PathOf("alias.json"), _temp.PathOf("in.csv")), "A hard link to an input must be detected.");
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateHardLinkW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateHardLink(string newFileName, string existingFileName, IntPtr securityAttributes);
}
