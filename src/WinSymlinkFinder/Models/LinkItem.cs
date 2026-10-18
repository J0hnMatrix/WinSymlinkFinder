using System;
using System.IO;

namespace WinSymlinkFinder.Models;

/// <summary>
/// Représente un lien symbolique ou point de jonction détecté.
/// </summary>
public sealed class LinkItem
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required string Directory { get; init; }
    public required LinkType Type { get; init; }
    public required string Target { get; init; }
    public bool IsTargetValid { get; init; }
    public bool IsRelative { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime LastWriteTime { get; init; }
    public long? FileSizeBytes { get; init; }

    /// <summary>
    /// Libellé convivial du type de lien pour l'UI.
    /// </summary>
    public string TypeDisplayName => Type switch
    {
        LinkType.SymbolicLinkFile => "Lien symbolique (Fichier)",
        LinkType.SymbolicLinkDirectory => "Lien symbolique (Dossier)",
        LinkType.Junction => "Jonction NTFS (Dossier)",
        LinkType.AppExecLink => "Alias d'application (AppExecLink)",
        _ => "Autre point d'analyse"
    };

    /// <summary>
    /// Badge court pour l'UI.
    /// </summary>
    public string TypeBadge => Type switch
    {
        LinkType.SymbolicLinkFile => "Symlink Fichier",
        LinkType.SymbolicLinkDirectory => "Symlink Dossier",
        LinkType.Junction => "Jonction",
        LinkType.AppExecLink => "Alias App",
        _ => "Reparse"
    };

    /// <summary>
    /// Glyphe Segoe Fluent Icons pour représenter le type de lien.
    /// </summary>
    public string IconGlyph => Type switch
    {
        LinkType.SymbolicLinkFile => "\uE7C3",      // Document / Page
        LinkType.SymbolicLinkDirectory => "\uED25", // FolderHorizontal
        LinkType.Junction => "\uE8B7",              // Folder
        LinkType.AppExecLink => "\uE71D",          // AllApps
        _ => "\uE71B"                              // Link
    };

    /// <summary>
    /// Statut textuel pour l'affichage.
    /// </summary>
    public string StatusText => IsTargetValid ? "Valide" : "Cible introuvable (cassé)";

    /// <summary>
    /// Glyphe d'état (check ou warning).
    /// </summary>
    public string StatusGlyph => IsTargetValid ? "\uE73E" : "\uE783";

    /// <summary>
    /// Indique si l'élément est un dossier / répertoire.
    /// </summary>
    public bool IsDirectory => Type == LinkType.SymbolicLinkDirectory || Type == LinkType.Junction;

    /// <summary>
    /// Taille formatée si applicable.
    /// </summary>
    public string FormattedSize
    {
        get
        {
            if (!FileSizeBytes.HasValue) return "—";
            long bytes = FileSizeBytes.Value;
            string[] sizes = ["o", "Ko", "Mo", "Go", "To"];
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
