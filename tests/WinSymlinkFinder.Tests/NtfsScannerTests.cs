using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WinSymlinkFinder.Models;
using WinSymlinkFinder.Services;
using Xunit;

namespace WinSymlinkFinder.Tests;

public class NtfsScannerTests
{
    [Fact]
    public async Task ScanDirectoryAsync_EmptyFolder_ReturnsEmptyList()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"scanner_empty_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var scanner = new NtfsLinkScanner();
            var links = new List<LinkItem>();

            await foreach (var link in scanner.ScanDirectoryAsync(tempDir, null, CancellationToken.None))
            {
                links.Add(link);
            }

            Assert.Empty(links);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir);
            }
        }
    }

    [Fact]
    public async Task ScanDirectoryAsync_NonExistentFolder_ExitsGracefully()
    {
        string nonExistent = Path.Combine(Path.GetTempPath(), $"no_such_dir_{Guid.NewGuid()}");
        var scanner = new NtfsLinkScanner();
        var links = new List<LinkItem>();

        await foreach (var link in scanner.ScanDirectoryAsync(nonExistent, null, CancellationToken.None))
        {
            links.Add(link);
        }

        Assert.Empty(links);
    }

    [Fact]
    public async Task ScanDirectoryAsync_NormalFilesAndFolders_DoesNotFlagAsLinks()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"scanner_normal_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        string subDir = Path.Combine(tempDir, "subfolder");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(tempDir, "file1.txt"), "hello");
        File.WriteAllText(Path.Combine(subDir, "file2.txt"), "world");

        try
        {
            var scanner = new NtfsLinkScanner();
            var links = new List<LinkItem>();

            await foreach (var link in scanner.ScanDirectoryAsync(tempDir, null, CancellationToken.None))
            {
                links.Add(link);
            }

            Assert.Empty(links);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ScanDirectoryAsync_CancellationRequested_TerminatesEarly()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"scanner_cancel_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var scanner = new NtfsLinkScanner();
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in scanner.ScanDirectoryAsync(tempDir, null, cts.Token))
                {
                }
            });
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir);
            }
        }
    }
}
