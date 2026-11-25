using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WinSymlinkFinder.Models;

/// <summary>
/// Contexte de sérialisation JSON généré à la compilation pour l'export des liens (compatible Trimming / AOT).
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(IEnumerable<LinkItem>))]
public partial class AppExportJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Contexte de sérialisation JSON généré à la compilation pour l'historique des sauvegardes (compatible Trimming / AOT).
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<DeletedLinkBackup>))]
public partial class AppBackupJsonContext : JsonSerializerContext
{
}
