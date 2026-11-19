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
    private static readonly StringComparison _pathComparison =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <inheritdoc />
    public bool AreSameFile(string firstPath, string secondPath)
    {
        string first = ResolveFinalPath(firstPath);
        string second = ResolveFinalPath(secondPath);
        if (string.Equals(first, second, _pathComparison))
        {
            return true;
        }

        return OperatingSystem.IsWindows() && File.Exists(first) && File.Exists(second) && HasSameWindowsFileIdentity(first, second);
    }

    /// <summary>Normalises a path and follows symbolic links to their final target.</summary>
    private static string ResolveFinalPath(string path)
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

    /// <summary>Compares volume serial and file index; fails closed (returns <c>true</c>) when identity cannot be read.</summary>
    private static bool HasSameWindowsFileIdentity(string first, string second)
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

    /// <summary>Retrieves file identity for an open handle.</summary>
    /// <param name="file">Open file handle.</param>
    /// <param name="information">Receives the file information.</param>
    /// <returns><c>true</c> on success.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetFileInformationByHandle(SafeFileHandle file, out ByHandleFileInformation information);

    /// <summary>Win32 <c>BY_HANDLE_FILE_INFORMATION</c>; only the volume serial and file index are used.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        /// <summary>File attribute flags.</summary>
        public uint FileAttributes;
        /// <summary>Creation time (FILETIME).</summary>
        public long CreationTime;
        /// <summary>Last access time (FILETIME).</summary>
        public long LastAccessTime;
        /// <summary>Last write time (FILETIME).</summary>
        public long LastWriteTime;
        /// <summary>Serial number of the volume holding the file.</summary>
        public uint VolumeSerialNumber;
        /// <summary>High 32 bits of the file size.</summary>
        public uint FileSizeHigh;
        /// <summary>Low 32 bits of the file size.</summary>
        public uint FileSizeLow;
        /// <summary>Number of hard links to the file.</summary>
        public uint NumberOfLinks;
        /// <summary>High 32 bits of the volume-unique file index.</summary>
        public uint FileIndexHigh;
        /// <summary>Low 32 bits of the volume-unique file index.</summary>
        public uint FileIndexLow;
    }
}
