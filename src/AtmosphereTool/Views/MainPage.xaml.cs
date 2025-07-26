using AtmosphereTool.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;

namespace AtmosphereTool.Views;

public sealed partial class MainPage : Page
{

    public MainPage()
    {
        InitializeComponent();
        if (!IsRunningAsAdministrator())
        {
            AdminWarningInfoBar.IsOpen = true;
        }
        AcrylicStatusAsync();
    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Start();
    }
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.Stop();
    }
    private async void TryEnableAcrylic(object sender, RoutedEventArgs e)
    {
        try
        {
            Registry.SetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "EnableTransparency",
                1,
                RegistryValueKind.DWord
            );
            var dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var dotnetExe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(dotnetExe) || !File.Exists(dotnetExe))
            {
                throw new InvalidOperationException("Could not locate executable to restart.");
            }
            var startInfo = new ProcessStartInfo
            {
                FileName = dotnetExe,
                Arguments = $"\"{dllPath}\"",
                UseShellExecute = true,
            };
            try
            {
                Process.Start(startInfo);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"Failed to restart: {ex.Message}");
            }
            AcrylicInfoBar.IsOpen = false;
            return;
        }
        catch (Exception ex)
        {
            var errordialog = new ContentDialog
            {
                Title = "Failed to enable transparency.",
                Content = ex.ToString(),
                PrimaryButtonText = "Ok",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.MainWindow.Content.XamlRoot
            };
            await errordialog.ShowAsync();
            return;
        }
    }
    private void AcrylicStatusAsync()
    {
        var value = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "EnableTransparency",
            null
        );
        if (value is int intValue && intValue == 1)
        {
            return;
        }
        else
        {
            AcrylicInfoBar.IsOpen = true;
            return;
        }

    }
    public static void RestartAsAdministrator()
    {
        var dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var dotnetExe = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(dotnetExe) || !File.Exists(dotnetExe))
        {
            throw new InvalidOperationException("Could not locate dotnet.exe to restart.");
        }
        var startInfo = new ProcessStartInfo
        {
            FileName = dotnetExe,
            Arguments = $"\"{dllPath}\"",
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
