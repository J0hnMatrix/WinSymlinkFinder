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

    private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, App.MainWindowHandle);
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                ViewModel.SelectedPath = folder.Path;
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
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, App.MainWindowHandle);
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Fichier CSV (*.csv)", new List<string> { ".csv" });
            savePicker.SuggestedFileName = $"liens_ntfs_{DateTime.Now:yyyyMMdd_HHmmss}";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await ViewModel.ExportCsvAsync(file.Path);
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
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, App.MainWindowHandle);
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Fichier JSON (*.json)", new List<string> { ".json" });
            savePicker.SuggestedFileName = $"liens_ntfs_{DateTime.Now:yyyyMMdd_HHmmss}";

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await ViewModel.ExportJsonAsync(file.Path);
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
