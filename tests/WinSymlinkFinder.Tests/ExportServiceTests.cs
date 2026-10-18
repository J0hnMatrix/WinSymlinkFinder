using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WinSymlinkFinder.Models;
using WinSymlinkFinder.Services;
using Xunit;

namespace WinSymlinkFinder.Tests;

public class ExportServiceTests
{
    [Fact]
    public async Task ExportToCsvAsync_GeneratesValidCsvContent()
    {
        // Arrange
        var service = new ExportService();
        string tempCsv = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid()}.csv");

        var testItems = new List<LinkItem>
        {
            new()
            {
                Name = "my_link.txt",
                FullPath = @"C:\Folder\my_link.txt",
                Directory = @"C:\Folder",
                Type = LinkType.SymbolicLinkFile,
                Target = @"C:\Targets\target.txt",
                IsTargetValid = true,
                IsRelative = false,
                CreationTime = new DateTime(2025, 1, 1, 10, 0, 0),
                LastWriteTime = new DateTime(2025, 1, 2, 12, 30, 0),
                FileSizeBytes = 1024
            },
            new()
            {
                Name = "junction_dir",
                FullPath = @"C:\Folder\junction_dir",
                Directory = @"C:\Folder",
                Type = LinkType.Junction,
                Target = @"D:\RealFolder",
                IsTargetValid = false,
                IsRelative = false,
                CreationTime = new DateTime(2025, 2, 1, 8, 0, 0),
                LastWriteTime = new DateTime(2025, 2, 1, 8, 0, 0),
                FileSizeBytes = null
            }
        };

        try
        {
            // Act
            await service.ExportToCsvAsync(tempCsv, testItems);

            // Assert
            Assert.True(File.Exists(tempCsv));
            string[] lines = await File.ReadAllLinesAsync(tempCsv);
            Assert.True(lines.Length >= 3); // En-tête + 2 lignes
            Assert.Contains("Nom;Type;Chemin;Cible;EstValide", lines[0]);
            Assert.Contains("my_link.txt", lines[1]);
            Assert.Contains("junction_dir", lines[2]);
            Assert.Contains("Oui", lines[1]);
            Assert.Contains("Non", lines[2]);
        }
        finally
        {
            if (File.Exists(tempCsv))
            {
                File.Delete(tempCsv);
            }
        }
    }

    [Fact]
    public async Task ExportToJsonAsync_GeneratesValidJsonContent()
    {
        // Arrange
        var service = new ExportService();
        string tempJson = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid()}.json");

        var testItems = new List<LinkItem>
        {
            new()
            {
                Name = "folder_link",
                FullPath = @"C:\Test\folder_link",
                Directory = @"C:\Test",
                Type = LinkType.SymbolicLinkDirectory,
                Target = @"C:\TargetFolder",
                IsTargetValid = true,
                IsRelative = false,
                CreationTime = DateTime.UtcNow,
                LastWriteTime = DateTime.UtcNow,
                FileSizeBytes = null
            }
        };

        try
        {
            // Act
            await service.ExportToJsonAsync(tempJson, testItems);

            // Assert
            Assert.True(File.Exists(tempJson));
            string json = await File.ReadAllTextAsync(tempJson);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.Equal(1, doc.RootElement.GetArrayLength());

            var first = doc.RootElement[0];
            Assert.Equal("folder_link", first.GetProperty("name").GetString());
            Assert.Equal(@"C:\Test\folder_link", first.GetProperty("fullPath").GetString());
            Assert.True(first.GetProperty("isTargetValid").GetBoolean());
        }
        finally
        {
            if (File.Exists(tempJson))
            {
                File.Delete(tempJson);
            }
        }
    }
}
