namespace WinSymlinkFinder.Models;

/// <summary>
/// Type de lien NTFS détecté.
/// </summary>
public enum LinkType
{
    /// <summary>
    /// Lien symbolique pointant vers un fichier.
    /// </summary>
    SymbolicLinkFile,

    /// <summary>
    /// Lien symbolique pointant vers un répertoire.
    /// </summary>
    SymbolicLinkDirectory,

    /// <summary>
    /// Point de jonction NTFS (répertoire uniquement).
    /// </summary>
    Junction,

    /// <summary>
    /// Alias d'exécution d'application Windows Store / UWP.
    /// </summary>
    AppExecLink,

    /// <summary>
    /// Lien physique NTFS (fichier uniquement).
    /// </summary>
    HardLink,

    /// <summary>
    /// Autre point d'analyse NTFS (WOF, Cloud Files, etc.).
    /// </summary>
    OtherReparsePoint
}
