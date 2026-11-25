using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WinSymlinkFinder.Core.Helpers;
using WinSymlinkFinder.Models;

namespace WinSymlinkFinder.Services;

/// <summary>
/// Service d'exportation des résultats de scan en CSV ou JSON.
/// </summary>
public interface IExportService
{
    Task ExportToCsvAsync(string filePath, IEnumerable<LinkItem> items);
    Task ExportToJsonAsync(string filePath, IEnumerable<LinkItem> items);
}

public sealed class ExportService : IExportService
{
    public async Task ExportToCsvAsync(string filePath, IEnumerable<LinkItem> items)
    {
        var sb = new StringBuilder();
        // En-têtes CSV
        sb.AppendLine("Nom;Type;Chemin;Cible;EstValide;EstRelatif;DateCreation;DateModification;TailleOctets");

        foreach (var item in items)
        {
            string line = string.Join(";",
                CsvHelper.EscapeCsv(item.Name),
                CsvHelper.EscapeCsv(item.TypeDisplayName),
                CsvHelper.EscapeCsv(item.FullPath),
                CsvHelper.EscapeCsv(item.Target),
                item.IsTargetValid ? "Oui" : "Non",
                item.IsRelative ? "Oui" : "Non",
                item.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                item.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                item.FileSizeBytes?.ToString() ?? string.Empty
            );
            sb.AppendLine(line);
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    public async Task ExportToJsonAsync(string filePath, IEnumerable<LinkItem> items)
    {
        using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, items, AppExportJsonContext.Default.IEnumerableLinkItem);
    }
}
