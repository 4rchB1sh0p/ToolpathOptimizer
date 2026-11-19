using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using ToolpathOptimizer.Application.Ports;

namespace ToolpathOptimizer.Infrastructure.Files;

/// <summary>
/// Detects path aliases by full-path normalisation, symbolic-link resolution and (on Windows) file identity
/// (volume serial + file index), which also catches hard links. On Unix only path and symlink checks are
/// performed; hard-link detection there is open debt (TD-012).
/// </summary>
public sealed partial class FileAliasDetector : IFileAliasDetector
{
    private static readonly StringComparison s_pathComparison =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <inheritdoc />
    public bool AreSameFile(string firstPath, string secondPath)
    {
        string first = Resolve(firstPath);
        string second = Resolve(secondPath);
        if (string.Equals(first, second, s_pathComparison))
        {
            return true;
        }

        return OperatingSystem.IsWindows() && File.Exists(first) && File.Exists(second) && SameWindowsFileIdentity(first, second);
    }

    private static string Resolve(string path)
    {
        string full = Path.GetFullPath(path);
        try
        {
            FileSystemInfo? target = new FileInfo(full).ResolveLinkTarget(returnFinalTarget: true);
            return target is null ? full : Path.GetFullPath(target.FullName);
        }
        catch (IOException)
        {
            return full;
        }
    }

    private static bool SameWindowsFileIdentity(string first, string second)
    {
        try
        {
            using SafeFileHandle a = File.OpenHandle(first, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using SafeFileHandle b = File.OpenHandle(second, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return GetFileInformationByHandle(a, out ByHandleFileInformation infoA)
                && GetFileInformationByHandle(b, out ByHandleFileInformation infoB)
                && infoA.VolumeSerialNumber == infoB.VolumeSerialNumber
                && infoA.FileIndexHigh == infoB.FileIndexHigh
                && infoA.FileIndexLow == infoB.FileIndexLow;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Fail closed: if identity cannot be established, treat as an alias so inputs are never replaced.
            return true;
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetFileInformationByHandle(SafeFileHandle file, out ByHandleFileInformation information);

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }
}
