using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private LinkType _selectedLinkType = LinkType.SymbolicLinkDirectory;

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

    [RelayCommand]
    public async Task CreateLinkAsync()
    {
        if (!CanCreateLink) return;

        IsCreating = true;
        IsStatusOpen = false;

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
            StatusMessage = $"Échec de la création du lien : {ex.Message}";
            IsStatusOpen = true;
            AppLogService.Instance.LogError("Erreur lors de la création du lien dans CreateLinkViewModel", ex);
        }
        finally
        {
            IsCreating = false;
        }
    }
}
