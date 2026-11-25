using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WinSymlinkFinder.Core.Helpers;
using WinSymlinkFinder.Models;

namespace WinSymlinkFinder.Services;

public interface ILinkBackupService
{
    string BackupDirectory { get; }
    string BackupCsvPath { get; }
    Task BackupDeletedLinkAsync(LinkItem link);
    Task<List<DeletedLinkBackup>> GetBackupsAsync();
    Task RemoveBackupAsync(Guid backupId);
    Task ExportBackupsToCsvAsync(string filePath);
}

public sealed class LinkBackupService : ILinkBackupService
{
    private static readonly Lazy<LinkBackupService> _lazy = new(() => new LinkBackupService());
    public static LinkBackupService Instance => _lazy.Value;

    private readonly string _backupDir;
    private readonly string _backupJsonPath;
    private readonly string _backupCsvPath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public string BackupDirectory => _backupDir;
    public string BackupCsvPath => _backupCsvPath;

    public LinkBackupService()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _backupDir = Path.Combine(appData, "WinSymlinkFinder", "Backups");
            Directory.CreateDirectory(_backupDir);
        }
        catch
        {
            _backupDir = Path.Combine(Path.GetTempPath(), "WinSymlinkFinder", "Backups");
            Directory.CreateDirectory(_backupDir);
        }

        _backupJsonPath = Path.Combine(_backupDir, "deleted_links_history.json");
        _backupCsvPath = Path.Combine(_backupDir, "deleted_links_history.csv");
    }

    public async Task BackupDeletedLinkAsync(LinkItem link)
    {
        var backup = new DeletedLinkBackup
        {
            Name = link.Name,
            OriginalPath = link.FullPath,
            Target = link.Target,
            Type = link.Type,
            IsRelative = link.IsRelative
        };

        await _fileLock.WaitAsync();
        try
        {
            var all = await ReadBackupsInternalAsync();
            all.Insert(0, backup);
            await SaveBackupsInternalAsync(all);
        }
        finally
        {
            _fileLock.Release();
        }

        AppLogService.Instance.LogInfo($"Sauvegarde créée pour le lien supprimé : '{link.FullPath}' -> '{link.Target}'");
    }

    public async Task<List<DeletedLinkBackup>> GetBackupsAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            return await ReadBackupsInternalAsync();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<DeletedLinkBackup>> ReadBackupsInternalAsync()
    {
        if (!File.Exists(_backupJsonPath))
        {
            return [];
        }

        try
        {
            string json = await File.ReadAllTextAsync(_backupJsonPath, Encoding.UTF8);
            var list = JsonSerializer.Deserialize(json, AppBackupJsonContext.Default.ListDeletedLinkBackup);
            return list ?? [];
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogError("Erreur lors de la lecture des sauvegardes de liens", ex);
            return [];
        }
    }

    public async Task RemoveBackupAsync(Guid backupId)
    {
        await _fileLock.WaitAsync();
        try
        {
            var all = await ReadBackupsInternalAsync();
            all.RemoveAll(b => b.Id == backupId);
            await SaveBackupsInternalAsync(all);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task ExportBackupsToCsvAsync(string filePath)
    {
        List<DeletedLinkBackup> all;
        await _fileLock.WaitAsync();
        try
        {
            all = await ReadBackupsInternalAsync();
        }
        finally
        {
            _fileLock.Release();
        }

        string csvContent = GenerateCsvContent(all);
        await File.WriteAllTextAsync(filePath, csvContent, Encoding.UTF8);
        AppLogService.Instance.LogInfo($"Sauvegardes exportées en CSV : '{filePath}' ({all.Count} entrées)");
    }

    private async Task SaveBackupsInternalAsync(List<DeletedLinkBackup> backups)
    {
        try
        {
            // 1. Sauvegarde au format JSON structuré
            string json = JsonSerializer.Serialize(backups, AppBackupJsonContext.Default.ListDeletedLinkBackup);
            await File.WriteAllTextAsync(_backupJsonPath, json, Encoding.UTF8);

            // 2. Synchronisation automatique au format CSV dans le même dossier
            string csvContent = GenerateCsvContent(backups);
            await File.WriteAllTextAsync(_backupCsvPath, csvContent, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogError("Erreur lors de la sauvegarde des fichiers JSON / CSV de sauvegarde", ex);
        }
    }

    private static string GenerateCsvContent(List<DeletedLinkBackup> backups)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id;DateSuppression;Nom;Type;CheminOriginal;Cible;EstRelatif");

        foreach (var b in backups)
        {
            string line = string.Join(";",
                b.Id,
                b.DeletedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                CsvHelper.EscapeCsv(b.Name),
                CsvHelper.EscapeCsv(b.TypeDisplayName),
                CsvHelper.EscapeCsv(b.OriginalPath),
                CsvHelper.EscapeCsv(b.Target),
                b.IsRelative ? "Oui" : "Non"
            );
            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
