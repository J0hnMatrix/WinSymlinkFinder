using System;
using System.IO;
using System.Threading.Tasks;
using WinSymlinkFinder.Models;
using WinSymlinkFinder.Services;
using Xunit;

namespace WinSymlinkFinder.Tests;

public class LinkManagementTests
{
    [Fact]
    public async Task DeleteLinkAsync_SystemProtectedLink_ThrowsInvalidOperationException()
    {
        var backupService = new LinkBackupService();
        var linkService = new LinkManagementService(backupService);

        var systemLink = new LinkItem
        {
            Name = "Application Data",
            FullPath = @"C:\Users\Default\Application Data",
            Directory = @"C:\Users\Default",
            Type = LinkType.Junction,
            Target = @"C:\Users\Default\AppData\Roaming",
            IsTargetValid = true,
            IsSystemLink = true,
            CreationTime = DateTime.Now,
            LastWriteTime = DateTime.Now
        };

        Assert.False(systemLink.CanBeDeleted);
        await Assert.ThrowsAsync<InvalidOperationException>(() => linkService.DeleteLinkAsync(systemLink));
    }

    [Fact]
    public async Task BackupService_AddAndRetrieveBackups_WorksCorrectly()
    {
        var backupService = new LinkBackupService();

        var link = new LinkItem
        {
            Name = "TestLink",
            FullPath = @"C:\Temp\TestLink",
            Directory = @"C:\Temp",
            Type = LinkType.SymbolicLinkDirectory,
            Target = @"C:\TargetFolder",
            IsTargetValid = true,
            IsSystemLink = false,
            CreationTime = DateTime.Now,
            LastWriteTime = DateTime.Now
        };

        await backupService.BackupDeletedLinkAsync(link);

        var backups = await backupService.GetBackupsAsync();
        Assert.NotEmpty(backups);

        var found = backups.Find(b => b.OriginalPath == link.FullPath);
        Assert.NotNull(found);
        Assert.Equal(link.Name, found.Name);
        Assert.Equal(link.Target, found.Target);

        // Nettoyage
        await backupService.RemoveBackupAsync(found.Id);
        var updated = await backupService.GetBackupsAsync();
        Assert.DoesNotContain(updated, b => b.Id == found.Id);
    }

    [Fact]
    public async Task BackupService_ExportToCsv_GeneratesValidCsv()
    {
        var backupService = new LinkBackupService();
        string tempCsv = Path.Combine(Path.GetTempPath(), $"backup_test_{Guid.NewGuid()}.csv");

        try
        {
            await backupService.ExportBackupsToCsvAsync(tempCsv);
            Assert.True(File.Exists(tempCsv));
            string content = await File.ReadAllTextAsync(tempCsv);
            Assert.StartsWith("Id;DateSuppression;Nom;Type;CheminOriginal;Cible;EstRelatif", content);
        }
        finally
        {
            if (File.Exists(tempCsv))
            {
                File.Delete(tempCsv);
            }
        }
    }
}
