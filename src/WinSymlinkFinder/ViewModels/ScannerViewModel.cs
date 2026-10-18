using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using WinSymlinkFinder.Models;
using WinSymlinkFinder.Services;

namespace WinSymlinkFinder.ViewModels;

public sealed partial class ScannerViewModel : ObservableObject
{
    private readonly INtfsLinkScanner _scanner;
    private readonly IExportService _exportService;
    private readonly DispatcherQueue _dispatcherQueue;

    private CancellationTokenSource? _scanCts;
    private readonly Stopwatch _stopwatch = new();
    private readonly List<LinkItem> _allLinks = [];

    [ObservableProperty]
    private string _selectedPath = @"C:\";

    [ObservableProperty]
    private ObservableCollection<string> _availableDrives = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStartScan))]
    [NotifyPropertyChangedFor(nameof(ScanningProgressBarVisibility))]
    private bool _isScanning;

    public bool CanStartScan => !IsScanning && !string.IsNullOrWhiteSpace(SelectedPath);
    public Visibility ScanningProgressBarVisibility => IsScanning ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScannedItemsCountText))]
    private long _scannedItemsCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LinksFoundCountText))]
    private int _linksFoundCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErrorsCountText))]
    private int _errorsCount;

    [ObservableProperty]
    private string _currentDirectoryStatus = "Prêt à scanner.";

    [ObservableProperty]
    private string _elapsedTimeText = "00:00:00";

    // Cartes de statistiques
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SymlinkFilesCountText))]
    private int _symlinkFilesCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SymlinkDirsCountText))]
    private int _symlinkDirsCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(JunctionsCountText))]
    private int _junctionsCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrokenLinksCountText))]
    private int _brokenLinksCount;

    public string ScannedItemsCountText => ScannedItemsCount.ToString("N0");
    public string LinksFoundCountText => LinksFoundCount.ToString("N0");
    public string ErrorsCountText => ErrorsCount.ToString("N0");
    public string SymlinkFilesCountText => SymlinkFilesCount.ToString("N0");
    public string SymlinkDirsCountText => SymlinkDirsCount.ToString("N0");
    public string JunctionsCountText => JunctionsCount.ToString("N0");
    public string BrokenLinksCountText => BrokenLinksCount.ToString("N0");
    public string FilteredLinksCountText => FilteredLinks.Count.ToString("N0");

    // Filtres
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _filterFiles = true;

    [ObservableProperty]
    private bool _filterDirectories = true;

    [ObservableProperty]
    private bool _filterJunctions = true;

    [ObservableProperty]
    private bool _filterOnlyBroken = false;

    [ObservableProperty]
    private LinkItem? _selectedItem;

    [ObservableProperty]
    private string? _statusInfoBarMessage;

    [ObservableProperty]
    private bool _isStatusInfoBarOpen;

    public ObservableCollection<LinkItem> FilteredLinks { get; } = [];

    public Visibility EmptyStateVisibility => FilteredLinks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public ScannerViewModel() : this(new NtfsLinkScanner(), new ExportService())
    {
    }

    public ScannerViewModel(INtfsLinkScanner scanner, IExportService exportService)
    {
        _scanner = scanner;
        _exportService = exportService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? DispatcherQueue.GetForCurrentThread();

        LoadAvailableDrives();
    }

    private void LoadAvailableDrives()
    {
        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => d.RootDirectory.FullName)
                .ToList();

            foreach (var d in drives)
            {
                AvailableDrives.Add(d);
            }

            if (AvailableDrives.Count > 0 && !AvailableDrives.Contains(SelectedPath))
            {
                SelectedPath = AvailableDrives[0];
            }
        }
        catch
        {
            AvailableDrives.Add(@"C:\");
            SelectedPath = @"C:\";
        }
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();
    partial void OnFilterFilesChanged(bool value) => ApplyFilter();
    partial void OnFilterDirectoriesChanged(bool value) => ApplyFilter();
    partial void OnFilterJunctionsChanged(bool value) => ApplyFilter();
    partial void OnFilterOnlyBrokenChanged(bool value) => ApplyFilter();
    partial void OnSelectedPathChanged(string value) => OnPropertyChanged(nameof(CanStartScan));

    [RelayCommand]
    public async Task StartScanAsync()
    {
        if (IsScanning || string.IsNullOrWhiteSpace(SelectedPath)) return;

        if (!Directory.Exists(SelectedPath))
        {
            ShowNotification($"Le répertoire '{SelectedPath}' est introuvable.");
            return;
        }

        IsScanning = true;
        _allLinks.Clear();
        FilteredLinks.Clear();
        ScannedItemsCount = 0;
        LinksFoundCount = 0;
        ErrorsCount = 0;
        SymlinkFilesCount = 0;
        SymlinkDirsCount = 0;
        JunctionsCount = 0;
        BrokenLinksCount = 0;
        ElapsedTimeText = "00:00:00";
        CurrentDirectoryStatus = $"Démarrage de l'analyse sur {SelectedPath}...";

        _scanCts = new CancellationTokenSource();
        _stopwatch.Restart();

        // Timer pour actualiser le temps écoulé
        var timerTask = Task.Run(async () =>
        {
            while (IsScanning)
            {
                await Task.Delay(500);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    ElapsedTimeText = _stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                });
            }
        });

        var progress = new Progress<ScanProgressReport>(report =>
        {
            ScannedItemsCount = report.ItemsScanned;
            LinksFoundCount = report.LinksFound;
            ErrorsCount = report.ErrorsEncountered;
            CurrentDirectoryStatus = report.CurrentDirectory;
        });

        try
        {
            await Task.Run(async () =>
            {
                await foreach (var link in _scanner.ScanDirectoryAsync(SelectedPath, progress, _scanCts.Token))
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        _allLinks.Add(link);

                        // Mise à jour compteurs par type
                        switch (link.Type)
                        {
                            case LinkType.SymbolicLinkFile:
                                SymlinkFilesCount++;
                                break;
                            case LinkType.SymbolicLinkDirectory:
                                SymlinkDirsCount++;
                                break;
                            case LinkType.Junction:
                                JunctionsCount++;
                                break;
                        }

                        if (!link.IsTargetValid)
                        {
                            BrokenLinksCount++;
                        }

                        if (MatchesFilter(link))
                        {
                            FilteredLinks.Add(link);
                            OnPropertyChanged(nameof(EmptyStateVisibility));
                            OnPropertyChanged(nameof(FilteredLinksCountText));
                        }
                    });
                }
            }, _scanCts.Token);

            CurrentDirectoryStatus = $"Analyse terminée avec succès : {LinksFoundCount} liens trouvés ({ScannedItemsCount:N0} éléments examinés).";
        }
        catch (OperationCanceledException)
        {
            CurrentDirectoryStatus = $"Analyse interrompue par l'utilisateur. {LinksFoundCount} liens trouvés.";
        }
        catch (Exception ex)
        {
            CurrentDirectoryStatus = $"Erreur lors de l'analyse : {ex.Message}";
        }
        finally
        {
            _stopwatch.Stop();
            IsScanning = false;
            ElapsedTimeText = _stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
            _scanCts.Dispose();
            _scanCts = null;
        }
    }

    [RelayCommand]
    public void StopScan()
    {
        if (IsScanning && _scanCts != null)
        {
            CurrentDirectoryStatus = "Arrêt de l'analyse en cours...";
            _scanCts.Cancel();
        }
    }

    [RelayCommand]
    public void ClearResults()
    {
        if (IsScanning) return;
        _allLinks.Clear();
        FilteredLinks.Clear();
        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(FilteredLinksCountText));
        ScannedItemsCount = 0;
        LinksFoundCount = 0;
        ErrorsCount = 0;
        SymlinkFilesCount = 0;
        SymlinkDirsCount = 0;
        JunctionsCount = 0;
        BrokenLinksCount = 0;
        CurrentDirectoryStatus = "Résultats effacés. Prêt pour un nouveau scan.";
    }

    [RelayCommand]
    public void OpenInExplorer(LinkItem? item)
    {
        item ??= SelectedItem;
        if (item == null) return;

        try
        {
            if (File.Exists(item.FullPath) || Directory.Exists(item.FullPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{item.FullPath}\"",
                    UseShellExecute = true
                });
            }
            else if (Directory.Exists(item.Directory))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{item.Directory}\"",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Impossible d'ouvrir l'Explorateur : {ex.Message}");
        }
    }

    [RelayCommand]
    public void OpenTargetInExplorer(LinkItem? item)
    {
        item ??= SelectedItem;
        if (item == null || string.IsNullOrWhiteSpace(item.Target)) return;

        try
        {
            string target = item.Target;
            if (item.IsRelative)
            {
                target = Path.GetFullPath(Path.Combine(item.Directory, item.Target));
            }

            if (File.Exists(target))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{target}\"",
                    UseShellExecute = true
                });
            }
            else if (Directory.Exists(target))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{target}\"",
                    UseShellExecute = true
                });
            }
            else
            {
                ShowNotification("La cible de ce lien n'existe pas ou est inaccessible.");
            }
        }
        catch (Exception ex)
        {
            ShowNotification($"Impossible d'ouvrir la cible : {ex.Message}");
        }
    }

    [RelayCommand]
    public void CopyPath(LinkItem? item)
    {
        item ??= SelectedItem;
        if (item == null) return;

        var dataPackage = new DataPackage();
        dataPackage.SetText(item.FullPath);
        Clipboard.SetContent(dataPackage);
        ShowNotification("Chemin copié dans le presse-papiers.");
    }

    [RelayCommand]
    public void CopyTarget(LinkItem? item)
    {
        item ??= SelectedItem;
        if (item == null) return;

        var dataPackage = new DataPackage();
        dataPackage.SetText(item.Target);
        Clipboard.SetContent(dataPackage);
        ShowNotification("Cible copiée dans le presse-papiers.");
    }

    [RelayCommand]
    public async Task ExportCsvAsync(string? saveFilePath)
    {
        if (string.IsNullOrWhiteSpace(saveFilePath)) return;

        try
        {
            await _exportService.ExportToCsvAsync(saveFilePath, _allLinks);
            ShowNotification($"Exportation CSV réussie ({_allLinks.Count} éléments).");
        }
        catch (Exception ex)
        {
            ShowNotification($"Erreur d'exportation : {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ExportJsonAsync(string? saveFilePath)
    {
        if (string.IsNullOrWhiteSpace(saveFilePath)) return;

        try
        {
            await _exportService.ExportToJsonAsync(saveFilePath, _allLinks);
            ShowNotification($"Exportation JSON réussie ({_allLinks.Count} éléments).");
        }
        catch (Exception ex)
        {
            ShowNotification($"Erreur d'exportation : {ex.Message}");
        }
    }

    public void ApplyFilter()
    {
        FilteredLinks.Clear();
        foreach (var item in _allLinks)
        {
            if (MatchesFilter(item))
            {
                FilteredLinks.Add(item);
            }
        }
        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(FilteredLinksCountText));
    }

    private bool MatchesFilter(LinkItem item)
    {
        if (FilterOnlyBroken && item.IsTargetValid)
        {
            return false;
        }

        bool typeMatches = item.Type switch
        {
            LinkType.SymbolicLinkFile => FilterFiles,
            LinkType.SymbolicLinkDirectory => FilterDirectories,
            LinkType.Junction => FilterJunctions,
            _ => true
        };

        if (!typeMatches)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return true;
        }

        return item.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
            || item.FullPath.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
            || item.Target.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase);
    }

    private void ShowNotification(string message)
    {
        StatusInfoBarMessage = message;
        IsStatusInfoBarOpen = true;
    }
}
