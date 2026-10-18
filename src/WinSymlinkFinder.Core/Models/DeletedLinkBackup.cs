using System;
using System.IO;

namespace WinSymlinkFinder.Models;

/// <summary>
/// Représente la sauvegarde d'un lien supprimé permettant sa restauration ultérieure.
/// </summary>
public sealed class DeletedLinkBackup
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime DeletedAt { get; init; } = DateTime.Now;
    public required string Name { get; init; }
    public required string OriginalPath { get; init; }
    public required string Target { get; init; }
    public required LinkType Type { get; init; }
    public bool IsRelative { get; init; }

    public string FormattedDeletedAt => DeletedAt.ToString("dd/MM/yyyy HH:mm:ss");

    public bool CanBeRestored => !File.Exists(OriginalPath) && !Directory.Exists(OriginalPath);

    public string TypeDisplayName => Type switch
    {
        LinkType.SymbolicLinkFile => "Symlink Fichier",
        LinkType.SymbolicLinkDirectory => "Symlink Dossier",
        LinkType.Junction => "Jonction NTFS",
        LinkType.HardLink => "Hard Link",
        _ => "Lien"
    };

    public string IconGlyph => Type switch
    {
        LinkType.SymbolicLinkFile => "\uE7C3",
        LinkType.SymbolicLinkDirectory => "\uED25",
        LinkType.Junction => "\uE8B7",
        _ => "\uE71B"
    };
}
