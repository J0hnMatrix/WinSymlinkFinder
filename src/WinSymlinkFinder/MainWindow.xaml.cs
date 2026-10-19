using Windows.Graphics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinSymlinkFinder.Core.Helpers;
using WinSymlinkFinder.Helpers;
using WinSymlinkFinder.Pages;
using WinSymlinkFinder.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinSymlinkFinder;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        uint dpi = DpiHelper.GetWindowDpi(hwnd);
        double scaleFactor = DpiHelper.GetScaleFactor(hwnd);

        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        int workAreaX = displayArea?.WorkArea.X ?? 0;
        int workAreaY = displayArea?.WorkArea.Y ?? 0;
        int workAreaWidth = displayArea?.WorkArea.Width ?? 0;
        int workAreaHeight = displayArea?.WorkArea.Height ?? 0;

        var (targetWidth, targetHeight) = WindowLayoutHelper.CalculateWindowSize(
            baseWidthDips: WindowLayoutHelper.DefaultBaseWidthDips,
            baseHeightDips: WindowLayoutHelper.DefaultBaseHeightDips,
            scaleFactor: scaleFactor,
            workAreaWidth: workAreaWidth,
            workAreaHeight: workAreaHeight);

        AppWindow.Resize(new SizeInt32(targetWidth, targetHeight));

        if (displayArea != null)
        {
            var (posX, posY) = WindowLayoutHelper.CalculateCenteredPosition(
                targetWidth,
                targetHeight,
                workAreaX,
                workAreaY,
                workAreaWidth,
                workAreaHeight);

            AppWindow.Move(new PointInt32(posX, posY));
        }

        AppLogService.Instance.LogInfo($"Taille de fenêtre initialisée : {targetWidth}x{targetHeight} (DPI={dpi}, échelle={scaleFactor * 100:F0}%, Écran={workAreaWidth}x{workAreaHeight})");

        NavFrame.Navigate(typeof(HomePage));
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "home":
                    NavFrame.Navigate(typeof(HomePage));
                    break;
                case "create_link":
                    NavFrame.Navigate(typeof(CreateLinkPage));
                    break;
                case "backups":
                    NavFrame.Navigate(typeof(BackupsPage));
                    break;
                case "about":
                    NavFrame.Navigate(typeof(AboutPage));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown navigation item tag: {item.Tag}");
            }
        }
    }
}
