using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinSymlinkFinder.Models;
using WinSymlinkFinder.Services;

namespace WinSymlinkFinder.ViewModels;

public sealed partial class CreateLinkViewModel : ObservableObject
{
    private readonly ILinkManagementService _linkService;

    [ObservableProperty]
    public partial string LinkPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TargetPath { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBeRelative))]
    [NotifyPropertyChangedFor(nameof(RelativeOptionHelpText))]
    public partial LinkType SelectedLinkType { get; set; } = LinkType.Junction;

    [ObservableProperty]
    public partial bool IsRelative { get; set; }

    [ObservableProperty]
    public partial bool IsCreating { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsStatusOpen { get; set; }

    [ObservableProperty]
    public partial InfoBarSeverity StatusSeverity { get; set; } = InfoBarSeverity.Informational;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSwitchToJunctionButtonVisibility))]
    public partial bool ShowSwitchToJunctionButton { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOpenDevSettingsButtonVisibility))]
    public partial bool ShowOpenDevSettingsButton { get; set; }

    public Visibility ShowSwitchToJunctionButtonVisibility => ShowSwitchToJunctionButton ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ShowOpenDevSettingsButtonVisibility => ShowOpenDevSettingsButton ? Visibility.Visible : Visibility.Collapsed;

    public bool CanBeRelative => SelectedLinkType == LinkType.SymbolicLinkDirectory || SelectedLinkType == LinkType.SymbolicLinkFile;

    public string RelativeOptionHelpText => CanBeRelative
        ? "Crée un lien avec un chemin relatif. Pratique pour déplacer le dossier parent sans briser le lien (nécessite d'être sur le même lecteur)."
        : "Option grisée : les jonctions NTFS et les hard links nécessitent obligatoirement des chemins absolus sous Windows.";

    public CreateLinkViewModel() : this(new LinkManagementService())
    {
    }

    public CreateLinkViewModel(ILinkManagementService linkService)
    {
        _linkService = linkService;
    }

    public bool CanCreateLink =>
        !IsCreating &&
        !string.IsNullOrWhiteSpace(LinkPath) &&
        !string.IsNullOrWhiteSpace(TargetPath);

    partial void OnLinkPathChanged(string value) => OnPropertyChanged(nameof(CanCreateLink));
    partial void OnTargetPathChanged(string value) => OnPropertyChanged(nameof(CanCreateLink));
    partial void OnIsCreatingChanged(bool value) => OnPropertyChanged(nameof(CanCreateLink));

    partial void OnSelectedLinkTypeChanged(LinkType value)
    {
        if (!CanBeRelative)
        {
            IsRelative = false;
        }
    }

    [RelayCommand]
    public void SwitchToJunction()
    {
        SelectedLinkType = LinkType.Junction;
        IsRelative = false;
        ShowSwitchToJunctionButton = false;
        StatusSeverity = InfoBarSeverity.Informational;
        StatusMessage = "Type modifié : « Jonction NTFS » sélectionnée. Vous pouvez maintenant cliquer sur « Créer le lien maintenant » sans privilèges administrateur !";
    }

    [RelayCommand]
    public void OpenDevSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:developers",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogError("Impossible d'ouvrir ms-settings:developers", ex);
        }
    }

    [RelayCommand]
    public async Task CreateLinkAsync()
    {
        if (!CanCreateLink) return;

        IsCreating = true;
        IsStatusOpen = false;
        ShowSwitchToJunctionButton = false;
        ShowOpenDevSettingsButton = false;

        try
        {
            await _linkService.CreateLinkAsync(LinkPath, TargetPath, SelectedLinkType, IsRelative);

            StatusSeverity = InfoBarSeverity.Success;
            StatusMessage = $"Lien ({SelectedLinkType}) créé avec succès à l'emplacement '{LinkPath}' !";
            IsStatusOpen = true;

            // Réinitialiser les champs
            LinkPath = string.Empty;
            TargetPath = string.Empty;
        }
        catch (Exception ex)
        {
            StatusSeverity = InfoBarSeverity.Error;
            StatusMessage = ex.Message;
            IsStatusOpen = true;

            bool isPrivilege = ex is UnauthorizedAccessException || ex.Message.Contains("privilege", StringComparison.OrdinalIgnoreCase);
            ShowSwitchToJunctionButton = isPrivilege && SelectedLinkType == LinkType.SymbolicLinkDirectory;
            ShowOpenDevSettingsButton = isPrivilege;

            AppLogService.Instance.LogError("Erreur lors de la création du lien dans CreateLinkViewModel", ex);
        }
        finally
        {
            IsCreating = false;
        }
    }
}
