using System;
using System.IO;
using WinSymlinkFinder.Services.Native;
using Xunit;

namespace WinSymlinkFinder.Tests;

public class ReparsePointHelperTests
{
    [Fact]
    public void CheckTargetExists_ValidExistingFile_ReturnsTrue()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            bool exists = ReparsePointHelper.CheckTargetExists(
                sourcePath: @"C:\dummy\link.txt",
                targetPath: tempFile,
                isRelative: false,
                isDirectory: false);

            Assert.True(exists);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void CheckTargetExists_NonExistentFile_ReturnsFalse()
    {
        string nonExistentPath = Path.Combine(Path.GetTempPath(), $"does_not_exist_{Guid.NewGuid()}.txt");

        bool exists = ReparsePointHelper.CheckTargetExists(
            sourcePath: @"C:\dummy\link.txt",
            targetPath: nonExistentPath,
            isRelative: false,
            isDirectory: false);

        Assert.False(exists);
    }

    [Fact]
    public void CheckTargetExists_RelativePath_ResolvesCorrectly()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"test_rel_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        string realFile = Path.Combine(tempDir, "real.txt");
        File.WriteAllText(realFile, "content");

        string linkSource = Path.Combine(tempDir, "link.txt");

        try
        {
            bool exists = ReparsePointHelper.CheckTargetExists(
                sourcePath: linkSource,
                targetPath: "real.txt",
                isRelative: true,
                isDirectory: false);

            Assert.True(exists);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
