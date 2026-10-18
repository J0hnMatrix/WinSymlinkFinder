using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinSymlinkFinder.Models;
using WinSymlinkFinder.Services;

namespace WinSymlinkFinder.ViewModels;

public sealed partial class BackupsViewModel : ObservableObject
{
    private readonly ILinkBackupService _backupService;
    private readonly ILinkManagementService _linkService;

    public ObservableCollection<DeletedLinkBackup> Backups { get; } = [];

    [ObservableProperty]
    private DeletedLinkBackup? _selectedBackup;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusOpen;

    [ObservableProperty]
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

    public Visibility EmptyStateVisibility => Backups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public BackupsViewModel() : this(LinkBackupService.Instance, new LinkManagementService())
    {
    }

    public BackupsViewModel(ILinkBackupService backupService, ILinkManagementService linkService)
    {
        _backupService = backupService;
        _linkService = linkService;
    }

    [RelayCommand]
    public async Task LoadBackupsAsync()
    {
        IsLoading = true;
        try
        {
            Backups.Clear();
            var list = await _backupService.GetBackupsAsync();
            foreach (var item in list)
            {
                Backups.Add(item);
            }
            OnPropertyChanged(nameof(EmptyStateVisibility));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RestoreBackupAsync(DeletedLinkBackup? backup)
    {
        backup ??= SelectedBackup;
        if (backup == null) return;

        try
        {
            await _linkService.RestoreLinkAsync(backup);
            Backups.Remove(backup);
            OnPropertyChanged(nameof(EmptyStateVisibility));

            StatusSeverity = InfoBarSeverity.Success;
            StatusMessage = $"Le lien '{backup.OriginalPath}' a été restauré avec succès !";
            IsStatusOpen = true;
        }
        catch (Exception ex)
        {
            StatusSeverity = InfoBarSeverity.Error;
            StatusMessage = $"Impossible de restaurer le lien : {ex.Message}";
            IsStatusOpen = true;
            AppLogService.Instance.LogError($"Erreur lors de la restauration du lien '{backup.OriginalPath}'", ex);
        }
    }

    [RelayCommand]
    public async Task RemoveBackupAsync(DeletedLinkBackup? backup)
    {
        backup ??= SelectedBackup;
        if (backup == null) return;

        await _backupService.RemoveBackupAsync(backup.Id);
        Backups.Remove(backup);
        OnPropertyChanged(nameof(EmptyStateVisibility));

        StatusSeverity = InfoBarSeverity.Informational;
        StatusMessage = "Sauvegarde retirée de l'historique.";
        IsStatusOpen = true;
    }

    [RelayCommand]
    public async Task ExportBackupsCsvAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            await _backupService.ExportBackupsToCsvAsync(filePath);
            StatusSeverity = InfoBarSeverity.Success;
            StatusMessage = $"Historique des sauvegardes exporté en CSV ({Backups.Count} éléments).";
            IsStatusOpen = true;
        }
        catch (Exception ex)
        {
            StatusSeverity = InfoBarSeverity.Error;
            StatusMessage = $"Erreur d'exportation : {ex.Message}";
            IsStatusOpen = true;
        }
    }

    public string BackupDirectory => _backupService.BackupDirectory;

    [RelayCommand]
    public void OpenBackupFolder()
    {
        try
        {
            if (Directory.Exists(BackupDirectory))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{BackupDirectory}\"",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogError("Impossible d'ouvrir le dossier de sauvegarde", ex);
        }
    }
}
