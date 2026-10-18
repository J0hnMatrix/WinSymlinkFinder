using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinSymlinkFinder.Models;
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
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? folder = Helpers.NativeDialogHelper.PickFolder(App.MainWindowHandle, ViewModel.SelectedPath);
            if (!string.IsNullOrEmpty(folder))
            {
                ViewModel.SelectedPath = folder;
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusInfoBarMessage = $"Erreur lors de la sélection du dossier : {ex.Message}";
            ViewModel.IsStatusInfoBarOpen = true;
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
}
