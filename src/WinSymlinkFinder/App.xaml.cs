using Microsoft.UI.Xaml;

namespace WinSymlinkFinder;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public static Window? MainWindowInstance { get; private set; }

    public static IntPtr MainWindowHandle => MainWindowInstance != null
        ? WinRT.Interop.WindowNative.GetWindowHandle(MainWindowInstance)
        : IntPtr.Zero;

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
        _window = window;
        MainWindowInstance = window;
        window.Activate();
    }
}
