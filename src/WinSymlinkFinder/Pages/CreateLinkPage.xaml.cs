using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinSymlinkFinder.Helpers;
using WinSymlinkFinder.Models;
using WinSymlinkFinder.ViewModels;

namespace WinSymlinkFinder.Pages;

public sealed partial class CreateLinkPage : Page
{
    public CreateLinkViewModel ViewModel { get; }

    public CreateLinkPage()
    {
        InitializeComponent();
        ViewModel = new CreateLinkViewModel();
        DataContext = ViewModel;
    }

    private void LinkTypeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LinkTypeRadioButtons.SelectedItem is RadioButton rb && rb.Tag is string tag)
        {
            ViewModel.SelectedLinkType = tag switch
            {
                "SymlinkDir" => LinkType.SymbolicLinkDirectory,
                "SymlinkFile" => LinkType.SymbolicLinkFile,
                "Junction" => LinkType.Junction,
                "HardLink" => LinkType.HardLink,
                _ => LinkType.SymbolicLinkDirectory
            };
        }
    }

    private void BrowseLinkFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? initial = !string.IsNullOrEmpty(ViewModel.LinkPath) && Directory.Exists(Path.GetDirectoryName(ViewModel.LinkPath))
                ? Path.GetDirectoryName(ViewModel.LinkPath)
                : @"C:\";

            string? folder = NativeDialogHelper.PickFolder(App.MainWindowHandle, initial, "Choisir le dossier conteneur du lien");
            if (!string.IsNullOrEmpty(folder))
            {
                // Concatène avec le nom de fichier ou défaut
                string currentName = !string.IsNullOrEmpty(ViewModel.LinkPath) ? Path.GetFileName(ViewModel.LinkPath) : "NouveauLien";
                if (string.IsNullOrEmpty(currentName)) currentName = "NouveauLien";
                ViewModel.LinkPath = Path.Combine(folder, currentName);
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Erreur lors de la sélection : {ex.Message}";
            ViewModel.StatusSeverity = InfoBarSeverity.Error;
            ViewModel.IsStatusOpen = true;
        }
    }

    private void BrowseTargetFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? initial = !string.IsNullOrEmpty(ViewModel.TargetPath) && Directory.Exists(ViewModel.TargetPath)
                ? ViewModel.TargetPath
                : @"C:\";

            string? folder = NativeDialogHelper.PickFolder(App.MainWindowHandle, initial, "Sélectionner le dossier cible");
            if (!string.IsNullOrEmpty(folder))
            {
                ViewModel.TargetPath = folder;
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Erreur lors de la sélection : {ex.Message}";
            ViewModel.StatusSeverity = InfoBarSeverity.Error;
            ViewModel.IsStatusOpen = true;
        }
    }

    private void BrowseTargetFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? initial = !string.IsNullOrEmpty(ViewModel.TargetPath) && File.Exists(ViewModel.TargetPath)
                ? Path.GetDirectoryName(ViewModel.TargetPath)
                : @"C:\";

            string? file = NativeDialogHelper.PickFile(App.MainWindowHandle, "Sélectionner le fichier cible", initial);
            if (!string.IsNullOrEmpty(file))
            {
                ViewModel.TargetPath = file;
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Erreur lors de la sélection : {ex.Message}";
            ViewModel.StatusSeverity = InfoBarSeverity.Error;
            ViewModel.IsStatusOpen = true;
        }
    }
}
