using System.Management;
using System.Reflection;
using AtmosphereTool.ViewModels;

namespace AtmosphereTool.Helpers;
public static class LogHelper
{
    public static readonly string LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AtmosphereTool\\Logs");
    public static readonly string CurrentLogFile = Path.Combine(LogDirectory, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.md");
    public static readonly string LatestLog = Path.Combine(LogDirectory, $"latest.md");

    private static readonly Lock _lock = new();

    public static (string? OSName, string? Build) GetWinVersion()
    {
        // Used for getting edition name
        using var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
        var version = Environment.OSVersion.Version;
        var versionString = $"Build {version.Build}";
        foreach (var os in searcher.Get())
        {
            return (os["Caption"]?.ToString(), versionString);
        }
        return (string.Empty, versionString);
    }
    // Borrow from Hardware page
    public static async void Initialize()
    {
        Directory.CreateDirectory(LogDirectory);
        var logs = Directory.GetFiles(LogDirectory);
        if (logs.Length > 20) { await CommandHelper.StartInCmd($"del /f /q \"{LogDirectory}\"\\*"); }
        if (AdminHelper.IsAdministrator) { await CommandHelper.StartInCmd($"icacls {LogDirectory} /grant Everyone:F /T"); }
        var (OSName, Build) = GetWinVersion();
        var version = Assembly.GetExecutingAssembly().GetName().Version!;
        var atmosphereToolVer = RegistryHelper.Read("HKLM", "SOFTWARE\\AME\\Playbooks\\Applied\\{8BBB362C-858B-41D9-A9EA-83A4B9669C43}", "Version") as string;

        var header = $@"
# AtmosphereTool ver. {version.Major}.{version.Minor}.{version.Build}

**OS:** {OSName} {Build}
**AtmosphereOS ver. {atmosphereToolVer}
**CPU:** {MainViewModel.GetCpuName()}  
**GPU:** {MainViewModel.GetGpuName()}  
**RAM:** {MainViewModel.GetInstalledMemory()}  
**DISK:** {MainViewModel.GetDiskName()}  
**System Uptime:** {MainViewModel.GetSystemUptime()}  
**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}

---

## Logs

";

        try
        {
            // Reset Latest Log
            File.WriteAllText(LatestLog, header);
            File.WriteAllText(CurrentLogFile, header);
        }
        catch (UnauthorizedAccessException)
        {
            // Done so it wouldn't crash on boot
        }
    }

    public static void LogInfo(string message)
    {
        AppendLog("Info", message);
    }

    public static void LogWarning(string message)
    {
        AppendLog("Warning", message);
    }

    public static void LogError(string message)
    {
        AppendLog("Error", message);
    }

    public static void LogCritical(string message)
    {
        AppendLog("Critical", message);
    }

    private static void AppendLog(string level, string message)
    {
        lock (_lock)
        {
            var entry = $"- [{level} | {DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
            try
            {
                File.AppendAllText(CurrentLogFile, entry);
                File.AppendAllText(LatestLog, entry);
            }
            catch (UnauthorizedAccessException)
            {
                // Again line 63
            }
        }
    }
}
