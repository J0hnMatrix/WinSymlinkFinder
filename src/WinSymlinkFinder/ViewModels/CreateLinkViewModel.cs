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
    private string _linkPath = string.Empty;

    [ObservableProperty]
    private string _targetPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBeRelative))]
    [NotifyPropertyChangedFor(nameof(RelativeOptionHelpText))]
    private LinkType _selectedLinkType = LinkType.Junction;

    [ObservableProperty]
    private bool _isRelative;

    [ObservableProperty]
    private bool _isCreating;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusOpen;

    [ObservableProperty]
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSwitchToJunctionButtonVisibility))]
    private bool _showSwitchToJunctionButton;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOpenDevSettingsButtonVisibility))]
    private bool _showOpenDevSettingsButton;

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
