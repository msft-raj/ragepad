using System.IO;
using System.Collections.Generic;

namespace RagePad.Services;

/// <summary>
/// Manages session persistence - saving and restoring open tabs.
/// </summary>
internal sealed class SessionManager
{
    /// <summary>
    /// Represents a saved tab entry.
    /// </summary>
    public sealed class TabEntry
    {
        public string? FilePath { get; init; }
        public string? BackupPath { get; init; }
        public int UntitledNumber { get; init; }
        public bool IsUntitled => FilePath == null;
    }

    /// <summary>
    /// Saves the current session.
    /// </summary>
    public void Save(IEnumerable<(string? filePath, int untitledNumber, string content)> tabs)
    {
        try
        {
            EnsureDirectoriesExist();
            ClearBackups();

            var sessionLines = new List<string>();

            foreach (var (filePath, untitledNumber, content) in tabs)
            {
                if (filePath != null)
                {
                    sessionLines.Add($"FILE:{filePath}");
                }
                else
                {
                    var backupFile = Path.Combine(AppInfo.BackupDir, $"untitled_{untitledNumber}.txt");
                    File.WriteAllText(backupFile, content);
                    sessionLines.Add($"BACKUP:{untitledNumber}:{backupFile}");
                }
            }

            File.WriteAllLines(AppInfo.SessionFile, sessionLines);
        }
        catch
        {
            // Ignore session save errors
        }
    }

    /// <summary>
    /// Loads the previous session.
    /// </summary>
    public IEnumerable<TabEntry> Load()
    {
        var entries = new List<TabEntry>();

        try
        {
            if (!File.Exists(AppInfo.SessionFile))
                return entries;

            foreach (var line in File.ReadAllLines(AppInfo.SessionFile))
            {
                if (line.StartsWith("FILE:"))
                {
                    var path = line.Substring(5);
                    if (File.Exists(path))
                    {
                        entries.Add(new TabEntry { FilePath = path });
                    }
                }
                else if (line.StartsWith("BACKUP:"))
                {
                    var entry = ParseBackupEntry(line);
                    if (entry != null)
                    {
                        entries.Add(entry);
                    }
                }
                else if (File.Exists(line))
                {
                    // Legacy format
                    entries.Add(new TabEntry { FilePath = line });
                }
            }
        }
        catch
        {
            // Ignore session load errors
        }

        return entries;
    }

    private static TabEntry? ParseBackupEntry(string line)
    {
        var rest = line.Substring(7); // Remove "BACKUP:"
        var colonIdx = rest.IndexOf(':');

        int untitledNum;
        string backupPath;

        if (colonIdx > 0 && int.TryParse(rest.Substring(0, colonIdx), out var num))
        {
            untitledNum = num;
            backupPath = rest.Substring(colonIdx + 1);
        }
        else
        {
            // Legacy format without number
            untitledNum = 1;
            backupPath = rest;
        }

        if (File.Exists(backupPath))
        {
            return new TabEntry
            {
                BackupPath = backupPath,
                UntitledNumber = untitledNum
            };
        }

        return null;
    }

    private static void EnsureDirectoriesExist()
    {
        if (!Directory.Exists(AppInfo.SessionDir))
            Directory.CreateDirectory(AppInfo.SessionDir);
        if (!Directory.Exists(AppInfo.BackupDir))
            Directory.CreateDirectory(AppInfo.BackupDir);
    }

    private static void ClearBackups()
    {
        foreach (var file in Directory.GetFiles(AppInfo.BackupDir, "*.txt"))
        {
            File.Delete(file);
        }
    }
}
