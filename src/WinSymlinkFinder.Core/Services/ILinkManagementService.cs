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
        if (string.IsNullOrWhiteSpace(linkPath)) throw new ArgumentException("L'emplacement du lien ne peut pas être vide.", nameof(linkPath));
        if (string.IsNullOrWhiteSpace(targetPath)) throw new ArgumentException("Le chemin de l'élément d'origine ne peut pas être vide.", nameof(targetPath));

        if (File.Exists(linkPath) || Directory.Exists(linkPath))
        {
            throw new IOException($"Un fichier ou dossier existe déjà à l'emplacement '{linkPath}'.");
        }

        string? parent = Path.GetDirectoryName(Path.GetFullPath(linkPath));
        if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
        {
            Directory.CreateDirectory(parent);
        }

        string finalTarget = targetPath;
        if (isRelative)
        {
            if (type != LinkType.SymbolicLinkFile && type != LinkType.SymbolicLinkDirectory)
            {
                throw new InvalidOperationException($"Le mode relatif n'est pas supporté pour le type '{type}'. Les jonctions NTFS et les hard links nécessitent obligatoirement des chemins absolus.");
            }

            string linkFull = Path.GetFullPath(linkPath);
            string targetFull = Path.GetFullPath(targetPath);
            string linkRoot = Path.GetPathRoot(linkFull) ?? string.Empty;
            string targetRoot = Path.GetPathRoot(targetFull) ?? string.Empty;

            if (!linkRoot.Equals(targetRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Impossible de créer un lien relatif entre deux lecteurs distincts ('{linkRoot}' et '{targetRoot}').\n" +
                    "Sous Windows, les chemins relatifs ne sont valides qu'au sein d'un même lecteur. Décochez la case « Créer un lien relatif » pour utiliser un chemin absolu.");
            }

            string? linkDir = Path.GetDirectoryName(linkFull);
            if (!string.IsNullOrEmpty(linkDir))
            {
                finalTarget = Path.GetRelativePath(linkDir, targetFull);
            }
        }
        else
        {
            finalTarget = Path.GetFullPath(targetPath);
        }

        AppLogService.Instance.LogInfo($"Création d'un lien : type={type}, linkPath='{linkPath}', targetPath='{targetPath}', finalTarget='{finalTarget}', isRelative={isRelative}");

        await Task.Run(() =>
        {
            try
            {
                switch (type)
                {
                    case LinkType.SymbolicLinkFile:
                        File.CreateSymbolicLink(linkPath, finalTarget);
                        break;

                    case LinkType.SymbolicLinkDirectory:
                        Directory.CreateSymbolicLink(linkPath, finalTarget);
                        break;

                    case LinkType.Junction:
                        CreateJunctionViaCmd(linkPath, Path.GetFullPath(targetPath));
                        break;

                    case LinkType.HardLink:
                        if (!CreateHardLink(linkPath, Path.GetFullPath(targetPath), IntPtr.Zero))
                        {
                            int err = Marshal.GetLastWin32Error();
                            throw new IOException($"Échec de la création du hard link (code Win32 {err}). Vérifiez que la cible existe et est sur le même volume NTFS.");
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Le type de lien '{type}' n'est pas pris en charge pour la création.");
                }
            }
            catch (Exception ex) when (IsPrivilegeException(ex))
            {
                string suggestion = type == LinkType.SymbolicLinkDirectory
                    ? "Solutions possibles :\n" +
                      "• Utilisez plutôt le type « Jonction NTFS » (recommandé pour les dossiers locaux, ne nécessite AUCUN privilège administrateur).\n" +
                      "• Ou activez le « Mode développeur » dans les Paramètres Windows (Système > Pour les développeurs) pour autoriser la création de liens symboliques sans élévation.\n" +
                      "• Ou lancez WinSymlinkFinder en tant qu'administrateur."
                    : "Solutions possibles :\n" +
                      "• Activez le « Mode développeur » dans les Paramètres Windows (Système > Pour les développeurs) pour autoriser la création de liens symboliques sans élévation.\n" +
                      "• Ou lancez WinSymlinkFinder en tant qu'administrateur.";

                throw new UnauthorizedAccessException(
                    $"Privilège administrateur Windows manquant (Privilege Not Held) :\n\n" +
                    $"Windows restreint la création de liens symboliques pour les utilisateurs standards.\n\n{suggestion}", ex);
            }
        });

        AppLogService.Instance.LogInfo($"Lien créé avec succès : '{linkPath}' -> '{finalTarget}'");
    }

    private static bool IsPrivilegeException(Exception ex)
    {
        if (ex is UnauthorizedAccessException) return true;
        if (ex.HResult == unchecked((int)0x80070522)) return true; // ERROR_PRIVILEGE_NOT_HELD
        if (ex.Message.Contains("privilege", StringComparison.OrdinalIgnoreCase)) return true;
        if (ex.InnerException != null && IsPrivilegeException(ex.InnerException)) return true;
        return false;
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
