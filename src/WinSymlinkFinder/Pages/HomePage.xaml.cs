using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinSymlinkFinder.Models;
using WinSymlinkFinder.Services;
using WinSymlinkFinder.ViewModels;

namespace WinSymlinkFinder.Pages;

public sealed partial class HomePage : Page
{
    public ScannerViewModel ViewModel { get; }

    public HomePage()
    {
        InitializeComponent();
        ViewModel = new ScannerViewModel();
        DataContext = ViewModel;

        if (ViewModel.AvailableDrives.Contains(ViewModel.SelectedPath))
        {
            DriveComboBox.SelectedItem = ViewModel.SelectedPath;
        }
    }

    private void DriveComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DriveComboBox.SelectedItem is string drive && !string.IsNullOrEmpty(drive))
        {
            ViewModel.SelectedPath = drive;
            AppLogService.Instance.LogInfo($"Lecteur sélectionné via ComboBox : '{drive}'");
        }
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? folder = Helpers.NativeDialogHelper.PickFolder(App.MainWindowHandle, ViewModel.SelectedPath);
            if (!string.IsNullOrEmpty(folder))
            {
                DriveComboBox.SelectedItem = null;
                ViewModel.SelectedPath = folder;
                AppLogService.Instance.LogInfo($"Chemin de scan mis à jour via Parcourir : '{folder}'");
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusInfoBarMessage = $"Erreur lors de la sélection du dossier : {ex.Message}";
            ViewModel.IsStatusInfoBarOpen = true;
            AppLogService.Instance.LogError("Erreur dans BrowseFolder_Click", ex);
        }
    }

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string defaultName = $"liens_ntfs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string? file = Helpers.NativeDialogHelper.PickSaveFile(App.MainWindowHandle, ".csv", "Fichier CSV", defaultName);
            if (!string.IsNullOrEmpty(file))
            {
                await ViewModel.ExportCsvAsync(file);
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusInfoBarMessage = $"Erreur lors de l'exportation CSV : {ex.Message}";
            ViewModel.IsStatusInfoBarOpen = true;
        }
    }

    private async void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string defaultName = $"liens_ntfs_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string? file = Helpers.NativeDialogHelper.PickSaveFile(App.MainWindowHandle, ".json", "Fichier JSON", defaultName);
            if (!string.IsNullOrEmpty(file))
            {
                await ViewModel.ExportJsonAsync(file);
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusInfoBarMessage = $"Erreur lors de l'exportation JSON : {ex.Message}";
            ViewModel.IsStatusInfoBarOpen = true;
        }
    }

    private void OpenInExplorer_Click(object sender, RoutedEventArgs e)
    {
        var item = (sender as FrameworkElement)?.DataContext as LinkItem;
        ViewModel.OpenInExplorer(item);
    }

    private void OpenTargetInExplorer_Click(object sender, RoutedEventArgs e)
    {
        var item = (sender as FrameworkElement)?.DataContext as LinkItem;
        ViewModel.OpenTargetInExplorer(item);
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        var item = (sender as FrameworkElement)?.DataContext as LinkItem;
        ViewModel.CopyPath(item);
    }

    private void CopyTarget_Click(object sender, RoutedEventArgs e)
    {
        var item = (sender as FrameworkElement)?.DataContext as LinkItem;
        ViewModel.CopyTarget(item);
    }

    private async void DeleteLink_Click(object sender, RoutedEventArgs e)
    {
        var item = (sender as FrameworkElement)?.DataContext as LinkItem;
        if (item == null) return;

        if (item.IsSystemLink)
        {
            var errorDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Suppression impossible",
                Content = $"Le lien '{item.Name}' est un composant système Windows protégé.\nIl ne peut pas être supprimé afin de préserver l'intégrité et la stabilité du système d'exploitation.",
                CloseButtonText = "Compris"
            };
            await errorDialog.ShowAsync();
            return;
        }

        var confirmDialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Confirmer la suppression du lien",
            Content = $"Êtes-vous sûr de vouloir supprimer le lien suivant ?\n\nNom : {item.Name}\nType : {item.TypeDisplayName}\nEmplacement : {item.FullPath}\nCible : {item.Target}\n\nNote : Seul le lien sera supprimé ; la cible d'origine restera totalement intacte.\nUne sauvegarde sera automatiquement effectuée pour permettre une restauration ultérieure.",
            PrimaryButtonText = "Supprimer le lien",
            CloseButtonText = "Annuler",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteLinkAsync(item);
        }
    }
}
