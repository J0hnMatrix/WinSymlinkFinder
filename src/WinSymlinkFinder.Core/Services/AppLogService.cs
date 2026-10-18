using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WinSymlinkFinder.Services;

public sealed class LogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public required string Level { get; init; }
    public required string Message { get; init; }
    public string? ExceptionDetails { get; init; }

    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss.fff");

    public string FormattedText =>
        $"[{Timestamp:HH:mm:ss.fff}] [{Level}] {Message}" +
        (!string.IsNullOrEmpty(ExceptionDetails) ? $"\n--> {ExceptionDetails}" : string.Empty);
}

/// <summary>
/// Service centralisé de journalisation pour le diagnostic et le mode debug.
/// </summary>
public sealed class AppLogService
{
    private static readonly Lazy<AppLogService> _lazyInstance = new(() => new AppLogService());
    public static AppLogService Instance => _lazyInstance.Value;

    private readonly object _lock = new();
    private readonly List<LogEntry> _logs = [];
    private readonly string _logFilePath;

    public event Action<LogEntry>? LogAdded;

    private AppLogService()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logDir = Path.Combine(appData, "WinSymlinkFinder", "Logs");
            Directory.CreateDirectory(logDir);
            _logFilePath = Path.Combine(logDir, $"app_{DateTime.Now:yyyyMMdd}.log");
        }
        catch
        {
            _logFilePath = Path.Combine(Path.GetTempPath(), "WinSymlinkFinder.log");
        }

        LogInfo("Service de journalisation initialisé.");
    }

    public string LogFilePath => _logFilePath;

    public IReadOnlyList<LogEntry> GetAllLogs()
    {
        lock (_lock)
        {
            return _logs.ToArray();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }

    public void LogInfo(string message) => Log("INFO", message);
    public void LogDebug(string message) => Log("DEBUG", message);
    public void LogWarning(string message) => Log("WARN", message);
    public void LogError(string message, Exception? ex = null)
    {
        string? details = ex != null ? $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}" : null;
        Log("ERROR", message, details);
    }

    private void Log(string level, string message, string? exceptionDetails = null)
    {
        var entry = new LogEntry
        {
            Level = level,
            Message = message,
            ExceptionDetails = exceptionDetails
        };

        lock (_lock)
        {
            _logs.Add(entry);
            if (_logs.Count > 1000)
            {
                _logs.RemoveAt(0);
            }
        }

        try
        {
            File.AppendAllText(_logFilePath, entry.FormattedText + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // Éviter de crasher si l'accès au fichier échoue
        }

        LogAdded?.Invoke(entry);
    }
}
