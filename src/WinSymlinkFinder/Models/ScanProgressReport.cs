using System;

namespace WinSymlinkFinder.Models;

/// <summary>
/// Rapport d'avancement émis pendant le scan NTFS.
/// </summary>
public sealed class ScanProgressReport
{
    public long ItemsScanned { get; init; }
    public int LinksFound { get; init; }
    public int ErrorsEncountered { get; init; }
    public required string CurrentDirectory { get; init; }
    public LinkItem? LatestLinkFound { get; init; }
}

/// <summary>
/// Statistiques globales de l'opération de scan.
/// </summary>
public sealed class ScanStatistics
{
    public long TotalItemsScanned { get; set; }
    public int SymlinkFilesCount { get; set; }
    public int SymlinkDirsCount { get; set; }
    public int JunctionsCount { get; set; }
    public int BrokenLinksCount { get; set; }
    public int OtherReparseCount { get; set; }
    public int ErrorsCount { get; set; }
    public TimeSpan ElapsedTime { get; set; }

    public int TotalLinksCount => SymlinkFilesCount + SymlinkDirsCount + JunctionsCount + OtherReparseCount;
}
