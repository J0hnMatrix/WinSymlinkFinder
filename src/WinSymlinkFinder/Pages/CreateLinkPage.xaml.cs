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
            string initial = @"C:\";
            if (!string.IsNullOrWhiteSpace(ViewModel.LinkPath))
            {
                try
                {
                    string? dir = Path.GetDirectoryName(ViewModel.LinkPath);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        initial = dir;
                    }
                    else if (Directory.Exists(ViewModel.LinkPath))
                    {
                        initial = ViewModel.LinkPath;
                    }
                }
                catch
                {
                }
            }

            string? folder = NativeDialogHelper.PickFolder(App.MainWindowHandle, initial, "Choisir le dossier conteneur du lien");
            if (!string.IsNullOrEmpty(folder))
            {
                string currentName = "NouveauLien";
                if (!string.IsNullOrWhiteSpace(ViewModel.LinkPath))
                {
                    try
                    {
                        string name = Path.GetFileName(ViewModel.LinkPath);
                        if (!string.IsNullOrEmpty(name))
                        {
                            currentName = name;
                        }
                    }
                    catch
                    {
                    }
                }
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
            string initial = @"C:\";
            if (!string.IsNullOrWhiteSpace(ViewModel.TargetPath))
            {
                try
                {
                    if (Directory.Exists(ViewModel.TargetPath))
                    {
                        initial = ViewModel.TargetPath;
                    }
                    else
                    {
                        string? dir = Path.GetDirectoryName(ViewModel.TargetPath);
                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        {
                            initial = dir;
                        }
                    }
                }
                catch
                {
                }
            }

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
            string initial = @"C:\";
            if (!string.IsNullOrWhiteSpace(ViewModel.TargetPath))
            {
                try
                {
                    if (File.Exists(ViewModel.TargetPath))
                    {
                        string? dir = Path.GetDirectoryName(ViewModel.TargetPath);
                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        {
                            initial = dir;
                        }
                    }
                    else if (Directory.Exists(ViewModel.TargetPath))
                    {
                        initial = ViewModel.TargetPath;
                    }
                }
                catch
                {
                }
            }

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
