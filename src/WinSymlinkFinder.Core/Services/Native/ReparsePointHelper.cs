using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using WinSymlinkFinder.Models;

namespace WinSymlinkFinder.Services.Native;

/// <summary>
/// Informations extraites d'un point d'analyse NTFS (Reparse Point).
/// </summary>
public sealed class ReparseInfo
{
    public uint ReparseTag { get; init; }
    public LinkType LinkType { get; init; }
    public string TargetPath { get; init; } = string.Empty;
    public string PrintName { get; init; } = string.Empty;
    public bool IsRelative { get; init; }
    public bool IsTargetValid { get; init; }
}

/// <summary>
/// Helper natif Win32 pour l'inspection précise des Reparse Points NTFS (liens symboliques, jonctions).
/// </summary>
public static class ReparsePointHelper
{
    public const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
    public const uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;
    public const uint IO_REPARSE_TAG_APPEXECLINK = 0x8000001B;

    private const uint FSCTL_SET_REPARSE_POINT = 0x000900A4;
    private const uint FSCTL_GET_REPARSE_POINT = 0x000900A8;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint FILE_SHARE_DELETE = 0x00000004;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
    private const int MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16 * 1024;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        byte[]? lpInBuffer,
        uint nInBufferSize,
        byte[]? lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    /// <summary>
    /// Crée une jonction NTFS directement via Win32 (FSCTL_SET_REPARSE_POINT) sans lancer cmd.exe.
    /// </summary>
    public static void CreateJunction(string linkPath, string targetPath)
    {
        string fullTarget = Path.GetFullPath(targetPath);
        if (!Directory.Exists(fullTarget))
        {
            throw new DirectoryNotFoundException($"Le dossier cible '{fullTarget}' est introuvable.");
        }

        // Normaliser : retirer le backslash final sauf pour la racine d'un lecteur (ex: C:\)
        string root = Path.GetPathRoot(fullTarget) ?? string.Empty;
        if (fullTarget.Length > root.Length && fullTarget.EndsWith(Path.DirectorySeparatorChar))
        {
            fullTarget = fullTarget.TrimEnd(Path.DirectorySeparatorChar);
        }

        Directory.CreateDirectory(linkPath);
        bool succeeded = false;
        try
        {
            using var handle = CreateFile(
                linkPath,
                GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                int err = Marshal.GetLastWin32Error();
                throw new IOException($"Impossible d'ouvrir le répertoire pour créer la jonction (code Win32 {err}).");
            }

            string substituteName = @"\??\" + fullTarget;
            string printName = fullTarget;

            int subByteLen = Encoding.Unicode.GetByteCount(substituteName);
            int printByteLen = Encoding.Unicode.GetByteCount(printName);
            int reparseDataLen = 8 + subByteLen + 2 + printByteLen + 2;
            int totalBufferSize = 8 + reparseDataLen;

            byte[] buffer = ArrayPool<byte>.Shared.Rent(totalBufferSize);
            try
            {
                Array.Clear(buffer, 0, totalBufferSize);

                BitConverter.TryWriteBytes(buffer.AsSpan(0, 4), IO_REPARSE_TAG_MOUNT_POINT);
                BitConverter.TryWriteBytes(buffer.AsSpan(4, 2), (ushort)reparseDataLen);
                BitConverter.TryWriteBytes(buffer.AsSpan(8, 2), (ushort)0); // SubstituteNameOffset
                BitConverter.TryWriteBytes(buffer.AsSpan(10, 2), (ushort)subByteLen);
                BitConverter.TryWriteBytes(buffer.AsSpan(12, 2), (ushort)(subByteLen + 2)); // PrintNameOffset
                BitConverter.TryWriteBytes(buffer.AsSpan(14, 2), (ushort)printByteLen);

                Encoding.Unicode.GetBytes(substituteName, buffer.AsSpan(16, subByteLen));
                Encoding.Unicode.GetBytes(printName, buffer.AsSpan(16 + subByteLen + 2, printByteLen));

                if (!DeviceIoControl(handle, FSCTL_SET_REPARSE_POINT, buffer, (uint)totalBufferSize, null, 0, out _, IntPtr.Zero))
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new IOException($"Échec de FSCTL_SET_REPARSE_POINT pour la jonction (code Win32 {err}).");
                }

                succeeded = true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        finally
        {
            if (!succeeded)
            {
                try
                {
                    if (Directory.Exists(linkPath))
                    {
                        Directory.Delete(linkPath, recursive: false);
                    }
                }
                catch
                {
                }
            }
        }
    }

    /// <summary>
    /// Inspecte un élément ayant l'attribut ReparsePoint et extrait ses informations précises.
    /// </summary>
    public static ReparseInfo? GetReparseInfo(string path, bool isDirectory)
    {
        try
        {
            using var handle = CreateFile(
                path,
                0, // GENERIC_READ n'est pas requis pour lire les métadonnées de reparse point
                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                // Fallback vers les API .NET si CreateFile échoue
                return FallbackViaDotNet(path, isDirectory);
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(MAXIMUM_REPARSE_DATA_BUFFER_SIZE);
            try
            {
                if (!DeviceIoControl(handle, FSCTL_GET_REPARSE_POINT, null, 0, buffer, MAXIMUM_REPARSE_DATA_BUFFER_SIZE, out uint bytesReturned, IntPtr.Zero))
                {
                    return FallbackViaDotNet(path, isDirectory);
                }

                if (bytesReturned < 8)
                {
                    return FallbackViaDotNet(path, isDirectory);
                }

                uint reparseTag = BitConverter.ToUInt32(buffer, 0);

                if (reparseTag == IO_REPARSE_TAG_SYMLINK)
                {
                    return ParseSymbolicLink(path, isDirectory, buffer);
                }
                else if (reparseTag == IO_REPARSE_TAG_MOUNT_POINT)
                {
                    return ParseMountPoint(path, buffer);
                }
                else if (reparseTag == IO_REPARSE_TAG_APPEXECLINK)
                {
                    return new ReparseInfo
                    {
                        ReparseTag = reparseTag,
                        LinkType = LinkType.AppExecLink,
                        TargetPath = "Windows App Execution Alias",
                        PrintName = "AppExecLink",
                        IsRelative = false,
                        IsTargetValid = true
                    };
                }
                else
                {
                    // Autre reparse point (WOF, OneDrive Cloud Files, etc.)
                    return new ReparseInfo
                    {
                        ReparseTag = reparseTag,
                        LinkType = LinkType.OtherReparsePoint,
                        TargetPath = $"ReparseTag: 0x{reparseTag:X8}",
                        PrintName = $"0x{reparseTag:X8}",
                        IsRelative = false,
                        IsTargetValid = true
                    };
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch
        {
            return FallbackViaDotNet(path, isDirectory);
        }
    }

    private static ReparseInfo ParseSymbolicLink(string sourcePath, bool isDirectory, byte[] buffer)
    {
        // REPARSE_DATA_BUFFER pour SymbolicLink :
        // Offset 0: ReparseTag (uint, 4)
        // Offset 4: ReparseDataLength (ushort, 2)
        // Offset 6: Reserved (ushort, 2)
        // Offset 8: SubstituteNameOffset (ushort, 2)
        // Offset 10: SubstituteNameLength (ushort, 2)
        // Offset 12: PrintNameOffset (ushort, 2)
        // Offset 14: PrintNameLength (ushort, 2)
        // Offset 16: Flags (uint, 4) - 0x1 = SYMLINK_FLAG_RELATIVE
        // Offset 20: PathBuffer (byte[])

        ushort subNameOffset = BitConverter.ToUInt16(buffer, 8);
        ushort subNameLen = BitConverter.ToUInt16(buffer, 10);
        ushort printNameOffset = BitConverter.ToUInt16(buffer, 12);
        ushort printNameLen = BitConverter.ToUInt16(buffer, 14);
        uint flags = BitConverter.ToUInt32(buffer, 16);

        bool isRelative = (flags & 0x1) == 0x1;

        string subName = string.Empty;
        string printName = string.Empty;

        if (subNameLen > 0 && 20 + subNameOffset + subNameLen <= buffer.Length)
        {
            subName = Encoding.Unicode.GetString(buffer, 20 + subNameOffset, subNameLen);
        }

        if (printNameLen > 0 && 20 + printNameOffset + printNameLen <= buffer.Length)
        {
            printName = Encoding.Unicode.GetString(buffer, 20 + printNameOffset, printNameLen);
        }

        string finalTarget = !string.IsNullOrEmpty(printName) ? printName : NormalizeWin32Path(subName);
        bool isValid = CheckTargetExists(sourcePath, finalTarget, isRelative, isDirectory);

        return new ReparseInfo
        {
            ReparseTag = IO_REPARSE_TAG_SYMLINK,
            LinkType = isDirectory ? LinkType.SymbolicLinkDirectory : LinkType.SymbolicLinkFile,
            TargetPath = finalTarget,
            PrintName = printName,
            IsRelative = isRelative,
            IsTargetValid = isValid
        };
    }

    private static ReparseInfo ParseMountPoint(string sourcePath, byte[] buffer)
    {
        // REPARSE_DATA_BUFFER pour MountPoint (Jonction) :
        // Offset 0: ReparseTag (uint, 4)
        // Offset 4: ReparseDataLength (ushort, 2)
        // Offset 6: Reserved (ushort, 2)
        // Offset 8: SubstituteNameOffset (ushort, 2)
        // Offset 10: SubstituteNameLength (ushort, 2)
        // Offset 12: PrintNameOffset (ushort, 2)
        // Offset 14: PrintNameLength (ushort, 2)
        // Offset 16: PathBuffer (byte[])

        ushort subNameOffset = BitConverter.ToUInt16(buffer, 8);
        ushort subNameLen = BitConverter.ToUInt16(buffer, 10);
        ushort printNameOffset = BitConverter.ToUInt16(buffer, 12);
        ushort printNameLen = BitConverter.ToUInt16(buffer, 14);

        string subName = string.Empty;
        string printName = string.Empty;

        if (subNameLen > 0 && 16 + subNameOffset + subNameLen <= buffer.Length)
        {
            subName = Encoding.Unicode.GetString(buffer, 16 + subNameOffset, subNameLen);
        }

        if (printNameLen > 0 && 16 + printNameOffset + printNameLen <= buffer.Length)
        {
            printName = Encoding.Unicode.GetString(buffer, 16 + printNameOffset, printNameLen);
        }

        string finalTarget = !string.IsNullOrEmpty(printName) ? printName : NormalizeWin32Path(subName);
        bool isValid = CheckTargetExists(sourcePath, finalTarget, false, isDirectory: true);

        return new ReparseInfo
        {
            ReparseTag = IO_REPARSE_TAG_MOUNT_POINT,
            LinkType = LinkType.Junction,
            TargetPath = finalTarget,
            PrintName = printName,
            IsRelative = false,
            IsTargetValid = isValid
        };
    }

    private static ReparseInfo? FallbackViaDotNet(string path, bool isDirectory)
    {
        try
        {
            FileSystemInfo info = isDirectory ? new DirectoryInfo(path) : new FileInfo(path);
            string? target = info.LinkTarget;
            if (string.IsNullOrEmpty(target))
            {
                return null;
            }

            bool isRelative = !Path.IsPathRooted(target);
            bool isValid = CheckTargetExists(path, target, isRelative, isDirectory);

            LinkType linkType = isDirectory ? LinkType.SymbolicLinkDirectory : LinkType.SymbolicLinkFile;

            return new ReparseInfo
            {
                ReparseTag = IO_REPARSE_TAG_SYMLINK,
                LinkType = linkType,
                TargetPath = target,
                PrintName = target,
                IsRelative = isRelative,
                IsTargetValid = isValid
            };
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeWin32Path(string rawPath)
    {
        if (rawPath.StartsWith(@"\??\", StringComparison.Ordinal))
        {
            return rawPath.Substring(4);
        }
        if (rawPath.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            return rawPath.Substring(4);
        }
        return rawPath;
    }

    public static bool CheckTargetExists(string sourcePath, string targetPath, bool isRelative, bool isDirectory)
    {
        try
        {
            string absoluteTarget = targetPath;
            if (isRelative)
            {
                string? parentDir = Path.GetDirectoryName(sourcePath);
                if (!string.IsNullOrEmpty(parentDir))
                {
                    absoluteTarget = Path.GetFullPath(Path.Combine(parentDir, targetPath));
                }
            }
            else
            {
                absoluteTarget = NormalizeWin32Path(targetPath);
            }

            if (isDirectory)
            {
                return Directory.Exists(absoluteTarget) || File.Exists(absoluteTarget);
            }
            else
            {
                return File.Exists(absoluteTarget) || Directory.Exists(absoluteTarget);
            }
        }
        catch
        {
            return false;
        }
    }
}
