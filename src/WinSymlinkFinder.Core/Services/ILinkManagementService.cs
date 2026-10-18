using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WinSymlinkFinder.Models;

namespace WinSymlinkFinder.Services;

public interface ILinkManagementService
{
    Task CreateLinkAsync(string linkPath, string targetPath, LinkType type, bool isRelative = false);
    Task DeleteLinkAsync(LinkItem link);
    Task RestoreLinkAsync(DeletedLinkBackup backup);
}

public sealed class LinkManagementService : ILinkManagementService
{
    private readonly ILinkBackupService _backupService;

    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    public LinkManagementService() : this(LinkBackupService.Instance)
    {
    }

    public LinkManagementService(ILinkBackupService backupService)
    {
        _backupService = backupService;
    }

    public async Task CreateLinkAsync(string linkPath, string targetPath, LinkType type, bool isRelative = false)
    {
        if (string.IsNullOrWhiteSpace(linkPath)) throw new ArgumentException("Le chemin du lien ne peut pas être vide.", nameof(linkPath));
        if (string.IsNullOrWhiteSpace(targetPath)) throw new ArgumentException("Le chemin de la cible ne peut pas être vide.", nameof(targetPath));

        if (File.Exists(linkPath) || Directory.Exists(linkPath))
        {
            throw new IOException($"Un fichier ou dossier existe déjà à l'emplacement '{linkPath}'.");
        }

        string? parent = Path.GetDirectoryName(linkPath);
        if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
        {
            Directory.CreateDirectory(parent);
        }

        AppLogService.Instance.LogInfo($"Création d'un lien : type={type}, linkPath='{linkPath}', targetPath='{targetPath}', isRelative={isRelative}");

        await Task.Run(() =>
        {
            switch (type)
            {
                case LinkType.SymbolicLinkFile:
                    File.CreateSymbolicLink(linkPath, targetPath);
                    break;

                case LinkType.SymbolicLinkDirectory:
                    Directory.CreateSymbolicLink(linkPath, targetPath);
                    break;

                case LinkType.Junction:
                    CreateJunctionViaCmd(linkPath, targetPath);
                    break;

                case LinkType.HardLink:
                    if (!CreateHardLink(linkPath, targetPath, IntPtr.Zero))
                    {
                        int err = Marshal.GetLastWin32Error();
                        throw new IOException($"Échec de la création du hard link (code Win32 {err}). Vérifiez que la cible existe et est sur le même volume NTFS.");
                    }
                    break;

                default:
                    throw new NotSupportedException($"Le type de lien '{type}' n'est pas pris en charge pour la création.");
            }
        });

        AppLogService.Instance.LogInfo($"Lien créé avec succès : '{linkPath}'");
    }

    public async Task DeleteLinkAsync(LinkItem link)
    {
        if (link.IsSystemLink)
        {
            AppLogService.Instance.LogWarning($"Tentative de suppression bloquée : le lien '{link.FullPath}' est un composant système protégé.");
            throw new InvalidOperationException("Ce lien est un composant de compatibilité du système d'exploitation Windows et est protégé contre la suppression pour garantir la stabilité du système.");
        }

        AppLogService.Instance.LogInfo($"Demande de suppression du lien : '{link.FullPath}' ({link.TypeDisplayName})");

        // 1. Sauvegarde préalable automatique
        await _backupService.BackupDeletedLinkAsync(link);

        // 2. Suppression sécurisée du lien sans toucher à la cible
        await Task.Run(() =>
        {
            if (link.Type == LinkType.SymbolicLinkDirectory || link.Type == LinkType.Junction)
            {
                if (Directory.Exists(link.FullPath))
                {
                    // En .NET et sous Windows, Directory.Delete sans récursion supprime uniquement le point de jonction/reparse point
                    Directory.Delete(link.FullPath, recursive: false);
                }
            }
            else
            {
                if (File.Exists(link.FullPath))
                {
                    File.Delete(link.FullPath);
                }
            }
        });

        AppLogService.Instance.LogInfo($"Lien supprimé avec succès : '{link.FullPath}' (sauvegarde enregistrée).");
    }

    public async Task RestoreLinkAsync(DeletedLinkBackup backup)
    {
        AppLogService.Instance.LogInfo($"Restauration du lien : '{backup.OriginalPath}' -> '{backup.Target}'");

        if (File.Exists(backup.OriginalPath) || Directory.Exists(backup.OriginalPath))
        {
            throw new IOException($"Impossible de restaurer : un fichier ou dossier existe déjà à l'emplacement '{backup.OriginalPath}'.");
        }

        await CreateLinkAsync(backup.OriginalPath, backup.Target, backup.Type, backup.IsRelative);
        await _backupService.RemoveBackupAsync(backup.Id);

        AppLogService.Instance.LogInfo($"Lien restauré et retiré de la liste des sauvegardes : '{backup.OriginalPath}'");
    }

    private static void CreateJunctionViaCmd(string linkPath, string targetPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Impossible de lancer cmd.exe pour mklink.");
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (proc.ExitCode != 0)
        {
            string errMessage = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
            throw new IOException($"Erreur lors de la création de la jonction : {errMessage.Trim()}");
        }
    }
}
