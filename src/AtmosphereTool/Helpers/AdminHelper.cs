using System.Diagnostics;
using System.Security.Principal;
using Microsoft.UI.Xaml;

namespace AtmosphereTool.Helpers;
public static class AdminHelper
{
    public static bool IsAdministrator => IsRunningAsAdministrator();

    public static void RestartAsAdministrator()
    {
        var dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var directory = Path.GetDirectoryName(dllPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException("Could not determine assembly directory.");
        }
        var exeName = Path.GetFileNameWithoutExtension(dllPath) + ".exe";
        var exePath = Path.Combine(directory, exeName);
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"Executable not found: {exePath}");
        }
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas"
        };
        try
        {
            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Failed to restart as admin: {ex.Message}");
        }
    }

    public static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
