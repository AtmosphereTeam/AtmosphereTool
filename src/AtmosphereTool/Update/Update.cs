using System.Diagnostics;
using System.Reflection;
using AtmosphereTool.Helpers;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;

namespace AtmosphereTool.Update;
public class Update
{
    public static async Task<(bool available, bool preview)> CheckForUpdate()
    {
        try
        {
            LogHelper.LogInfo("Checking for updates");
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7.55.1");
            var releasesUrl = "https://api.github.com/repos/Goldendraggon/AtmosphereTool/releases";
            var response = await httpClient.GetAsync(releasesUrl);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var array = JArray.Parse(json);
            var tag = array.First?["tag_name"]?.Value<string>();
            if (array.Count == 0 || string.IsNullOrEmpty(tag))
            {
                LogHelper.LogCritical("No releases found or tag is missing.");
                return (false, false);
            }
            var isNewer = CompareVersions(tag);
            var isPreview = tag.Contains('-');
            LogHelper.LogInfo($"Update available: {isNewer} Preview: {isPreview}");
            return (isNewer, isPreview);
        }
        catch (Exception e)
        {
            LogHelper.LogCritical("Failed to check for updates. Error: " + e.Message);
            return (false, false);
        }
    }

    private static bool CompareVersions(string ver)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;
        ver = ver.TrimStart('v');
        if (ver.Contains('-'))
        {
            var dashIndex = ver.IndexOf('-');
            if (dashIndex >= 0)
            {
                ver = ver[..dashIndex];
            }
        }
        var subver = ver.Split('.');
        var intsubver = subver.Select(int.Parse).ToArray();
        if (intsubver.Length < 2) { return false; }
        if (intsubver[0] > version.Major) { return true; }
        if (intsubver[0] < version.Major) { return false; }
        if (intsubver[1] > version.Minor) { return true; }
        if (intsubver[1] < version.Minor) { return false; }
        if (intsubver[2] > version.Build) { return true; }
        return false;
    }

    public static async Task UpdateTool()
    {
        LogHelper.LogInfo("Checking for Updates");
        var (update, preview) = await CheckForUpdate();
        if (update && !preview)
        {
            var updatedialog = new ContentDialog
            {
                Title = "Update",
                Content = "A newer version of AtmosphereTool is out. \nWould you like to update?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.MainWindow.Content.XamlRoot
            };
            if (await updatedialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var updater = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update\\Updater.ps1");
                if (File.Exists(updater))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{updater}\"",
                        UseShellExecute = true,
                        Verb = AdminHelper.IsAdministrator ? "runas" : ""
                    };
                    Process.Start(psi);
                    Environment.Exit(0);
                }
            }
        }
    }
}
