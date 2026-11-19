using System.Runtime.InteropServices;
using ToolpathOptimizer.Infrastructure.Files;

namespace ToolpathOptimizer.Infrastructure.IntegrationTests.Files;

public sealed partial class FileAliasDetectorTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly FileAliasDetector _detector = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public async Task AreSameFile_DetectsRelativeAndAbsoluteSpellings()
    {
        await File.WriteAllTextAsync(_temp.File("in.csv"), "x");
        string relative = Path.Combine(_temp.Path, "sub", "..", "in.csv");

        Assert.True(_detector.AreSameFile(relative, _temp.File("in.csv")));
    }

    [Fact]
    public async Task AreSameFile_DistinguishesDifferentFiles()
    {
        await File.WriteAllTextAsync(_temp.File("a.csv"), "x");
        await File.WriteAllTextAsync(_temp.File("b.csv"), "x");

        Assert.False(_detector.AreSameFile(_temp.File("a.csv"), _temp.File("b.csv")));
    }

    [Fact]
    public void AreSameFile_TreatsNonExistingOutputAsDistinct()
    {
        Assert.False(_detector.AreSameFile(_temp.File("new.json"), _temp.File("in.csv")));
    }

    [Fact]
    public async Task AreSameFile_DetectsHardLinkOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // Hard-link detection on Unix is open debt (TD-012).
        }

        await File.WriteAllTextAsync(_temp.File("in.csv"), "x");
        Assert.True(CreateHardLink(_temp.File("alias.json"), _temp.File("in.csv"), IntPtr.Zero));

        Assert.True(_detector.AreSameFile(_temp.File("alias.json"), _temp.File("in.csv")));
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateHardLinkW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateHardLink(string newFileName, string existingFileName, IntPtr securityAttributes);
}
