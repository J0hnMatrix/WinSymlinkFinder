using System;
using System.Collections.Generic;
using System.Threading;
using WinSymlinkFinder.Models;

namespace WinSymlinkFinder.Services;

/// <summary>
/// Contrat du service de scan NTFS pour la détection de liens symboliques et jonctions.
/// </summary>
public interface INtfsLinkScanner
{
    /// <summary>
    /// Scanne récursivement un répertoire pour trouver les liens symboliques et jonctions.
    /// Émet les résultats au fur et à mesure sans bloquer le thread appelant.
    /// </summary>
    IAsyncEnumerable<LinkItem> ScanDirectoryAsync(
        string rootPath,
        IProgress<ScanProgressReport>? progress,
        CancellationToken cancellationToken);
}
