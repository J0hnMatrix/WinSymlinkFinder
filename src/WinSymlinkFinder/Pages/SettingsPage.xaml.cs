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
}
