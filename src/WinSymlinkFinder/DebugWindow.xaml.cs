using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using WinSymlinkFinder.Services;

namespace WinSymlinkFinder;

public sealed partial class DebugWindow : Window
{
    private readonly ObservableCollection<LogEntry> _displayedLogs = [];
    private string _currentFilter = string.Empty;

    public DebugWindow()
    {
        InitializeComponent();

        AppWindow.Resize(new Windows.Graphics.SizeInt32(960, 620));
        AppWindow.SetIcon("Assets/AppIcon.ico");

        LogListView.ItemsSource = _displayedLogs;
        LogPathTextBlock.Text = $"Fichier journal : {AppLogService.Instance.LogFilePath}";

        ReloadLogs();

        AppLogService.Instance.LogAdded += OnLogAdded;
        Closed += DebugWindow_Closed;
    }

    private void DebugWindow_Closed(object sender, WindowEventArgs args)
    {
        AppLogService.Instance.LogAdded -= OnLogAdded;
    }

    private void OnLogAdded(LogEntry entry)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (MatchesFilter(entry))
            {
                _displayedLogs.Add(entry);
                if (AutoScrollCheckBox.IsChecked == true && _displayedLogs.Count > 0)
                {
                    LogListView.ScrollIntoView(_displayedLogs[^1]);
                }
            }
        });
    }

    private void ReloadLogs()
    {
        _displayedLogs.Clear();
        var all = AppLogService.Instance.GetAllLogs();
        foreach (var entry in all)
        {
            if (MatchesFilter(entry))
            {
                _displayedLogs.Add(entry);
            }
        }
        if (AutoScrollCheckBox.IsChecked == true && _displayedLogs.Count > 0)
        {
            LogListView.ScrollIntoView(_displayedLogs[^1]);
        }
    }

    private bool MatchesFilter(LogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(_currentFilter)) return true;
        return entry.Message.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase)
            || entry.Level.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase)
            || (entry.ExceptionDetails != null && entry.ExceptionDetails.Contains(_currentFilter, StringComparison.OrdinalIgnoreCase));
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentFilter = SearchBox.Text;
        ReloadLogs();
    }

    private void CopyAll_Click(object sender, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        foreach (var entry in _displayedLogs)
        {
            sb.AppendLine(entry.FormattedText);
        }

        var dp = new DataPackage();
        dp.SetText(sb.ToString());
        Clipboard.SetContent(dp);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        AppLogService.Instance.Clear();
        _displayedLogs.Clear();
    }

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string path = AppLogService.Instance.LogFilePath;
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true
                });
            }
        }
        catch
        {
        }
    }
}
