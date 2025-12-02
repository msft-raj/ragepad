using System;
using System.IO;

namespace RagePad.Services;

/// <summary>
/// Application-level information and paths.
/// </summary>
internal static class AppInfo
{
    public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
    
    public static string SessionDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RagePad");
    
    public static string SessionFile => Path.Combine(SessionDir, "session.txt");
    public static string BackupDir => Path.Combine(SessionDir, "backup");
    
    public static string GetVersion()
    {
        var versionFile = Path.Combine(BaseDirectory, "version.txt");
        if (File.Exists(versionFile))
        {
            return File.ReadAllText(versionFile).Trim();
        }
        return "0.1";
    }
}
