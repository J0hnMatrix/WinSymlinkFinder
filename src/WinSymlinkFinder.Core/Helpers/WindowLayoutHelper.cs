namespace WinSymlinkFinder.Core.Helpers;

/// <summary>
/// Fournit des calculs de dimensionnement et de positionnement de fenêtre
/// prenant en compte le facteur d'échelle DPI et les limites de la zone de travail de l'écran.
/// </summary>
public static class WindowLayoutHelper
{
    public const double DefaultBaseWidthDips = 1605.0;
    public const double DefaultBaseHeightDips = 1015.0;
    public const int MinWindowWidth = 800;
    public const int MinWindowHeight = 600;
    public const int ScreenMargin = 32;

    /// <summary>
    /// Calcule la taille de la fenêtre en pixels physiques pour un facteur d'échelle donné,
    /// en s'assurant qu'elle ne dépasse pas l'espace disponible de la zone de travail de l'écran.
    /// </summary>
    public static (int Width, int Height) CalculateWindowSize(
        double baseWidthDips,
        double baseHeightDips,
        double scaleFactor,
        int workAreaWidth = 0,
        int workAreaHeight = 0)
    {
        if (scaleFactor <= 0)
        {
            scaleFactor = 1.0;
        }

        int targetWidth = (int)Math.Round(baseWidthDips * scaleFactor, MidpointRounding.AwayFromZero);
        int targetHeight = (int)Math.Round(baseHeightDips * scaleFactor, MidpointRounding.AwayFromZero);

        if (workAreaWidth > 0 && targetWidth > workAreaWidth)
        {
            targetWidth = Math.Max(MinWindowWidth, workAreaWidth - ScreenMargin);
        }

        if (workAreaHeight > 0 && targetHeight > workAreaHeight)
        {
            targetHeight = Math.Max(MinWindowHeight, workAreaHeight - ScreenMargin);
        }

        return (targetWidth, targetHeight);
    }

    /// <summary>
    /// Calcule la position centrée de la fenêtre dans la zone de travail de l'écran.
    /// </summary>
    public static (int X, int Y) CalculateCenteredPosition(
        int windowWidth,
        int windowHeight,
        int workAreaX,
        int workAreaY,
        int workAreaWidth,
        int workAreaHeight)
    {
        int x = workAreaX + Math.Max(0, (workAreaWidth - windowWidth) / 2);
        int y = workAreaY + Math.Max(0, (workAreaHeight - windowHeight) / 2);
        return (x, y);
    }
}
