using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace AtmosphereTool.SubPages;

public sealed partial class ProgressPage : Page
{
    public ObservableCollection<string> StatusMessages { get; } = new();

    public ProgressPage()
    {
        this.InitializeComponent();
        StatusListView.ItemsSource = StatusMessages;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = Start();
    }

    public async Task Start()
    {
        StatusMessages.Clear();
        ProgressBar.Value = 0;
        // TODO: Base verification + Advanced verification
        var checks = new Func<Task<bool>>[]
        {
        () => CheckProcessAsync("MsMpEng"),
        () => CheckDirectoryAsync("%ProgramFiles%\\Windows Defender"),
        () => CheckDirectoryAsync("%ProgramData%\\Microsoft\\Windows Defender"),
        () => CheckServiceAsync("wuauserv"),
        () => CheckFileAsync("%WINDIR%\\System32\\wuaueng.dll"),
        () => CheckFileAsync("%WINDIR%\\System32\\wuapi.dll"),
        () => CheckDirectoryAsync("%ProgramFiles(x86)%\\Microsoft\\Edge"),
        () => CheckDirectoryAsync("%WINDIR%\\SystemApps\\*MicrosoftEdge*"),
        () => CheckProcessAsync("WinStore.App"),
        () => CheckFileAsync("%WINDIR%\\System32\\smartscreen.exe"),
        () => CheckFileAsync("%WINDIR%\\System32\\SIHClient.exe"),
        () => CheckFileAsync("%WINDIR%\\System32\\StorSvc.dll")
        };

        ProgressBar.Maximum = checks.Length;
        var completed = 0;
        var successCount = 0;

        foreach (var check in checks)
        {
            var result = await check();
            if (result) successCount++;
            completed++;
            ProgressBar.Value = completed;
        }

        // Determine install state
        DispatcherQueue.TryEnqueue(() =>
        {
            if (successCount == checks.Length)
            {
                FinalStatusIcon.Glyph = "\uE8FB"; // Accept
                FinalStatusIcon.Foreground = new SolidColorBrush(Colors.Green);
                FinalStatusText.Text = "Atmosphere is fully installed.";
            }
            else if (successCount > 0)
            {
                FinalStatusIcon.Glyph = "\uE7BA"; // Warning
                FinalStatusIcon.Foreground = new SolidColorBrush(Colors.Orange);
                FinalStatusText.Text = "Atmosphere is partially installed. Please contact our team or re-apply AtmosphereOS playbook";
            }
            else
            {
                FinalStatusIcon.Glyph = "\uEB90"; // Block Contact / Error
                FinalStatusIcon.Foreground = new SolidColorBrush(Colors.Red);
                FinalStatusText.Text = "Atmosphere is not installed.";
            }
        });
    }


    private async Task<bool> CheckProcessAsync(string processName)
    {
        return await Task.Run(() =>
        {
            var exists = Process.GetProcessesByName(processName).Length > 0;
            AddStatus($"Process '{processName}': {(exists ? "Found" : "Not Found")}");
            return exists;
        });
    }

    private async Task<bool> CheckDirectoryAsync(string path)
    {
        return await Task.Run(() =>
        {
            var expanded = Environment.ExpandEnvironmentVariables(path);
            var exists = Directory.Exists(expanded);
            AddStatus($"Directory '{expanded}': {(exists ? "Found" : "Not Found")}");
            return exists;
        });
    }

    private async Task<bool> CheckFileAsync(string path)
    {
        return await Task.Run(() =>
        {
            var expanded = Environment.ExpandEnvironmentVariables(path);
            var exists = File.Exists(expanded);
            AddStatus($"File '{expanded}': {(exists ? "Found" : "Not Found")}");
            return exists;
        });
    }

    private async Task<bool> CheckServiceAsync(string serviceName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var sc = new ServiceController(serviceName);
                var status = sc.Status;
                AddStatus($"Service '{serviceName}': Found, Status = {status}");
                return true;
            }
            catch
            {
                AddStatus($"Service '{serviceName}': Not Found");
                return false;
            }
        });
    }
    private void BackButtonClick(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }
    private void AddStatus(string message)
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            StatusMessages.Add(message);

            // Scroll to the newest item
            if (StatusListView.Items.Count > 0)
            {
                StatusListView.ScrollIntoView(StatusListView.Items[^1]);
            }
        });
    }
}
