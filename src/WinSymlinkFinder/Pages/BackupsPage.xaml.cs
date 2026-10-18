using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinSymlinkFinder.Helpers;
using WinSymlinkFinder.Models;
using WinSymlinkFinder.ViewModels;

namespace WinSymlinkFinder.Pages;

public sealed partial class BackupsPage : Page
{
    public BackupsViewModel ViewModel { get; }

    public BackupsPage()
    {
        InitializeComponent();
        ViewModel = new BackupsViewModel();
        DataContext = ViewModel;

        Loaded += async (s, e) =>
        {
            await ViewModel.LoadBackupsAsync();
        };
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadBackupsAsync();
    }

    private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenBackupFolder();
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string defaultName = $"sauvegardes_liens_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string? file = NativeDialogHelper.PickSaveFile(App.MainWindowHandle, ".csv", "Fichier CSV", defaultName);
            if (!string.IsNullOrEmpty(file))
            {
                await ViewModel.ExportBackupsCsvAsync(file);
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Erreur lors de l'exportation : {ex.Message}";
            ViewModel.StatusSeverity = InfoBarSeverity.Error;
            ViewModel.IsStatusOpen = true;
        }
    }

    private async void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        var backup = (sender as FrameworkElement)?.DataContext as DeletedLinkBackup;
        if (backup == null) return;

        var confirmDialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Restaurer le lien",
            Content = $"Voulez-vous recréer le lien suivant ?\n\nEmplacement : {backup.OriginalPath}\nCible : {backup.Target}\nType : {backup.TypeDisplayName}",
            PrimaryButtonText = "Restaurer",
            CloseButtonText = "Annuler",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.RestoreBackupAsync(backup);
        }
    }

    private async void DeleteBackup_Click(object sender, RoutedEventArgs e)
    {
        var backup = (sender as FrameworkElement)?.DataContext as DeletedLinkBackup;
        if (backup == null) return;

        var confirmDialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Supprimer de l'historique",
            Content = $"Voulez-vous retirer définitivement la sauvegarde du lien '{backup.Name}' de l'historique ?\n(Cette action ne touche à aucun fichier sur votre disque)",
            PrimaryButtonText = "Supprimer",
            CloseButtonText = "Annuler",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.RemoveBackupAsync(backup);
        }
    }
}
