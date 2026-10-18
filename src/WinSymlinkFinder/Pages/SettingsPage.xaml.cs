using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinSymlinkFinder.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += SettingsPage_Loaded;
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (App.MainWindowInstance?.Content is FrameworkElement root)
        {
            switch (root.RequestedTheme)
            {
                case ElementTheme.Light:
                    ThemeRadioButtons.SelectedIndex = 0;
                    break;
                case ElementTheme.Dark:
                    ThemeRadioButtons.SelectedIndex = 1;
                    break;
                default:
                    ThemeRadioButtons.SelectedIndex = 2;
                    break;
            }
        }
    }

    private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeRadioButtons.SelectedItem is RadioButton selected && App.MainWindowInstance?.Content is FrameworkElement root)
        {
            string? tag = selected.Tag?.ToString();
            root.RequestedTheme = tag switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }

    private int _versionClickCount;
    private static DebugWindow? _debugWindowInstance;

    private void VersionButton_Click(object sender, RoutedEventArgs e)
    {
        _versionClickCount++;

        if (_versionClickCount == 7)
        {
            _versionClickCount = 0;
            DebugTipInfoBar.Title = "Mode Développeur Déverrouillé !";
            DebugTipInfoBar.Message = "Ouverture de la fenêtre de diagnostic et du journal d'événements en direct.";
            DebugTipInfoBar.Severity = InfoBarSeverity.Success;
            DebugTipInfoBar.IsOpen = true;

            OpenDebugWindow();
        }
        else if (_versionClickCount >= 4)
        {
            int remaining = 7 - _versionClickCount;
            DebugTipInfoBar.Title = "Mode Développeur";
            DebugTipInfoBar.Message = $"Encore {remaining} clic{(remaining > 1 ? "s" : "")} pour ouvrir le journal de débogage...";
            DebugTipInfoBar.Severity = InfoBarSeverity.Informational;
            DebugTipInfoBar.IsOpen = true;
        }
    }

    public static void OpenDebugWindow()
    {
        if (_debugWindowInstance == null)
        {
            _debugWindowInstance = new DebugWindow();
            _debugWindowInstance.Closed += (s, e) => _debugWindowInstance = null;
        }
        _debugWindowInstance.Activate();
    }
}
