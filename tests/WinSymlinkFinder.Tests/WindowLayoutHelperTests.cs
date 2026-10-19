using WinSymlinkFinder.Core.Helpers;

namespace WinSymlinkFinder.Tests;

public class WindowLayoutHelperTests
{
    [Fact]
    public void CalculateWindowSize_NormalDpi_ReturnsExactBaseDimensions()
    {
        // 1080p standard monitor (1920x1040 work area, 100% scaling)
        var (width, height) = WindowLayoutHelper.CalculateWindowSize(
            baseWidthDips: 1605,
            baseHeightDips: 1015,
            scaleFactor: 1.0,
            workAreaWidth: 1920,
            workAreaHeight: 1040);

        Assert.Equal(1605, width);
        Assert.Equal(1015, height);
    }

    [Fact]
    public void CalculateWindowSize_HighDpi4K_ScalesProportionally()
    {
        // 4K monitor at 150% (3840x2100 work area)
        var (width150, height150) = WindowLayoutHelper.CalculateWindowSize(
            baseWidthDips: 1605,
            baseHeightDips: 1015,
            scaleFactor: 1.5,
            workAreaWidth: 3840,
            workAreaHeight: 2100);

        Assert.Equal(2408, width150);
        Assert.Equal(1523, height150);

        // 4K monitor at 200% (3840x2100 work area)
        var (width200, height200) = WindowLayoutHelper.CalculateWindowSize(
            baseWidthDips: 1605,
            baseHeightDips: 1015,
            scaleFactor: 2.0,
            workAreaWidth: 3840,
            workAreaHeight: 2100);

        Assert.Equal(3210, width200);
        Assert.Equal(2030, height200);
    }

    [Fact]
    public void CalculateWindowSize_ConstrainedScreen_ClampsToWorkAreaMargin()
    {
        // 1080p laptop screen at 150% scaling: work area 1920x1040, but desired size is 2408x1523
        var (width, height) = WindowLayoutHelper.CalculateWindowSize(
            baseWidthDips: 1605,
            baseHeightDips: 1015,
            scaleFactor: 1.5,
            workAreaWidth: 1920,
            workAreaHeight: 1040);

        Assert.Equal(1920 - WindowLayoutHelper.ScreenMargin, width);
        Assert.Equal(1040 - WindowLayoutHelper.ScreenMargin, height);
    }

    [Fact]
    public void CalculateCenteredPosition_PrimaryMonitor_CentersCorrectly()
    {
        var (x, y) = WindowLayoutHelper.CalculateCenteredPosition(
            windowWidth: 1605,
            windowHeight: 1015,
            workAreaX: 0,
            workAreaY: 0,
            workAreaWidth: 1920,
            workAreaHeight: 1040);

        Assert.Equal(157, x);
        Assert.Equal(12, y);
    }

    [Fact]
    public void CalculateCenteredPosition_SecondaryMonitor_OffsetsByMonitorOrigin()
    {
        // Secondary monitor placed to the right (X=1920, Y=0, Width=2560, Height=1400)
        var (x, y) = WindowLayoutHelper.CalculateCenteredPosition(
            windowWidth: 2006,
            windowHeight: 1269,
            workAreaX: 1920,
            workAreaY: 0,
            workAreaWidth: 2560,
            workAreaHeight: 1400);

        Assert.Equal(1920 + (2560 - 2006) / 2, x);
        Assert.Equal((1400 - 1269) / 2, y);
    }
}
