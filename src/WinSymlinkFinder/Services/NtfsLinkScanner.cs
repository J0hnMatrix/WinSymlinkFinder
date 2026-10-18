using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using WinSymlinkFinder.Models;
using WinSymlinkFinder.Services.Native;

namespace WinSymlinkFinder.Services;

/// <summary>
/// Moteur de scan NTFS asynchrone non-bloquant.
/// </summary>
public sealed class NtfsLinkScanner : INtfsLinkScanner
{
    private const int ProgressReportBatchSize = 100;

    public async IAsyncEnumerable<LinkItem> ScanDirectoryAsync(
        string rootPath,
        IProgress<ScanProgressReport>? progress,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            yield break;
        }

        long itemsScanned = 0;
        int linksFound = 0;
        int errorsEncountered = 0;

        // File d'attente pour parcours itératif (évite stack overflow)
        var pendingDirs = new Queue<string>();
        pendingDirs.Enqueue(rootPath);

        // Maintien d'un ensemble pour détecter et empêcher toute redondance
        var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (pendingDirs.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string currentDir = pendingDirs.Dequeue();
            if (!visitedPaths.Add(currentDir))
            {
                continue;
            }

            // Notification d'avancement périodique
            itemsScanned++;
            if (itemsScanned % ProgressReportBatchSize == 0)
            {
                progress?.Report(new ScanProgressReport
                {
                    ItemsScanned = itemsScanned,
                    LinksFound = linksFound,
                    ErrorsEncountered = errorsEncountered,
                    CurrentDirectory = currentDir
                });
                // Permet au thread UI et autres tâches de respirer
                await Task.Yield();
            }

            DirectoryInfo dirInfo;
            try
            {
                dirInfo = new DirectoryInfo(currentDir);
            }
            catch (Exception)
            {
                errorsEncountered++;
                continue;
            }

            // 1. Traitement des sous-répertoires
            IEnumerable<DirectoryInfo> subDirs;
            try
            {
                subDirs = dirInfo.EnumerateDirectories();
            }
            catch (UnauthorizedAccessException)
            {
                errorsEncountered++;
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                errorsEncountered++;
                continue;
            }
            catch (PathTooLongException)
            {
                errorsEncountered++;
                continue;
            }
            catch (IOException)
            {
                errorsEncountered++;
                continue;
            }

            foreach (var subDir in subDirs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                itemsScanned++;

                bool isReparsePoint = (subDir.Attributes & FileAttributes.ReparsePoint) != 0;

                if (isReparsePoint)
                {
                    // C'est un lien symbolique de répertoire, une jonction, ou un reparse point
                    var reparseInfo = ReparsePointHelper.GetReparseInfo(subDir.FullName, isDirectory: true);
                    if (reparseInfo != null && reparseInfo.LinkType != LinkType.OtherReparsePoint)
                    {
                        linksFound++;
                        var linkItem = new LinkItem
                        {
                            Name = subDir.Name,
                            FullPath = subDir.FullName,
                            Directory = currentDir,
                            Type = reparseInfo.LinkType,
                            Target = reparseInfo.TargetPath,
                            IsTargetValid = reparseInfo.IsTargetValid,
                            IsRelative = reparseInfo.IsRelative,
                            CreationTime = subDir.CreationTime,
                            LastWriteTime = subDir.LastWriteTime,
                            FileSizeBytes = null
                        };

                        progress?.Report(new ScanProgressReport
                        {
                            ItemsScanned = itemsScanned,
                            LinksFound = linksFound,
                            ErrorsEncountered = errorsEncountered,
                            CurrentDirectory = currentDir,
                            LatestLinkFound = linkItem
                        });

                        yield return linkItem;
                    }

                    // IMPORTANT : Ne PAS descendre récursivement dans un reparse point pour éviter les boucles infinies !
                }
                else
                {
                    // Répertoire normal : on l'ajoute pour exploration future
                    pendingDirs.Enqueue(subDir.FullName);
                }
            }

            // 2. Traitement des fichiers du répertoire courant
            IEnumerable<FileInfo> files;
            try
            {
                files = dirInfo.EnumerateFiles();
            }
            catch (UnauthorizedAccessException)
            {
                errorsEncountered++;
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                errorsEncountered++;
                continue;
            }
            catch (PathTooLongException)
            {
                errorsEncountered++;
                continue;
            }
            catch (IOException)
            {
                errorsEncountered++;
                continue;
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                itemsScanned++;

                bool isReparsePoint = (file.Attributes & FileAttributes.ReparsePoint) != 0;
                if (isReparsePoint)
                {
                    var reparseInfo = ReparsePointHelper.GetReparseInfo(file.FullName, isDirectory: false);
                    if (reparseInfo != null && reparseInfo.LinkType != LinkType.OtherReparsePoint)
                    {
                        linksFound++;
                        var linkItem = new LinkItem
                        {
                            Name = file.Name,
                            FullPath = file.FullName,
                            Directory = currentDir,
                            Type = reparseInfo.LinkType,
                            Target = reparseInfo.TargetPath,
                            IsTargetValid = reparseInfo.IsTargetValid,
                            IsRelative = reparseInfo.IsRelative,
                            CreationTime = file.CreationTime,
                            LastWriteTime = file.LastWriteTime,
                            FileSizeBytes = file.Length
                        };

                        progress?.Report(new ScanProgressReport
                        {
                            ItemsScanned = itemsScanned,
                            LinksFound = linksFound,
                            ErrorsEncountered = errorsEncountered,
                            CurrentDirectory = currentDir,
                            LatestLinkFound = linkItem
                        });

                        yield return linkItem;
                    }
                }
            }
        }

        // Rapport final
        progress?.Report(new ScanProgressReport
        {
            ItemsScanned = itemsScanned,
            LinksFound = linksFound,
            ErrorsEncountered = errorsEncountered,
            CurrentDirectory = "Scan terminé."
        });
    }
}
