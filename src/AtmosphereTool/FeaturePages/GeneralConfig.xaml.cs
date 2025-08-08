using AtmosphereTool.Helpers;
using AtmosphereTool.Views;


using Microsoft.UI;
// using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.WinUI.Controls;

using System.Diagnostics;
using System.ServiceProcess;

using WinResLoader = Windows.ApplicationModel.Resources.ResourceLoader;

namespace AtmosphereTool.FeaturePages;

public sealed partial class GeneralConfig : Page
{
    private readonly WinResLoader _resourceLoader = WinResLoader.GetForViewIndependentUse("FeaturePages");

    public string GetLocalizedString(string key)
    {
        var localized = _resourceLoader.GetString(key);
        return string.IsNullOrEmpty(localized) ? key : localized;
    }

    public GeneralConfig()
    {
        InitializeComponent();
        LoadControls();
        LocalizeControls();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        LogHelper.LogInfo("Navigated To GeneralConfig");
        if (App.MainWindow.Content is ShellPage shellPage)
        {
            shellPage.SetBreadcrumb(new Folder { Name = "General Configuration", Page = typeof(GeneralConfig) });
        }
        if (e.Parameter == null || e.Parameter.ToString() == string.Empty) { return; }
        if (e.Parameter is string target)
        {
            var elementMap = new Dictionary<string, FrameworkElement>
            {
                { "Copilot", AIExpander },
                { "Recall", AIExpander },
                { "Updates", AutoWinUpdates },
                { "BackgroundApps", BackgroundApps },
                { "DeliveryOptimizations", DeliveryOptimizations },
                { "FSOAndGameBar", FSOAndGameBar },
                { "PhoneLink", PhoneLink },
                { "SearchIndex", SearchIndex },
                { "StoreAppArchiving", StoreAppArchiving },
                { "SystemRestore", SystemRestore },
                { "UpdateNotifications", UpdateNotifications },
            };

            if (elementMap.TryGetValue(target, out var element))
            {
                element.StartBringIntoView();
                if (element is SettingsCard card)
                {
                    _ = HighlightBorderAsync(card);
                }
                if (element is SettingsExpander expander)
                {
                    var originalBrush = expander.BorderBrush;
                    expander.BorderBrush = new SolidColorBrush(Colors.White);
                    _ = Task.Delay(2000).ContinueWith(_ =>
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            expander.BorderBrush = originalBrush;
                        });
                    });
                }
            }
        }
    }
    private static async Task HighlightBorderAsync(Control control)
    {
        await Task.Delay(200);
        control.StartBringIntoView();
        var originalBrush = control.BorderBrush;
        control.BorderBrush = new SolidColorBrush(Colors.White);
        await Task.Delay(5000);
        control.BorderBrush = originalBrush;
    }
    private void LoadControls()
    {
        var usersid = RegistryHelper.GetCurrentUserSid();
        ToggleRecall.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "DisableAIDataAnalysis") == null;
        ToggleCopilot.IsOn = RegistryHelper.Read("HKU", $"{usersid}\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot", "TurnOffWindowsCopilot") == null;
        ToggleUpdates.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AUOptions") != null;
        ToggleBackgroundApps.IsOn = RegistryHelper.Read("HKU", $"{usersid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled") as int? == 1 && RegistryHelper.Read("HKU", $"{usersid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search", "BackgroundAppGlobalToggle") as int? == 0;
        ToggleDeliveryOptimizations.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization", "DODownloadMode") as int? == 0;
        ToggleFSOAndGameBar.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\PolicyManager\\default\\ApplicationManagement\\AllowGameDVR", "value") as int? == 0;
        TogglePhoneLink.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "SettingsPageVisibility") is string hiddenpages && hiddenpages.Split(';').Contains("mobile-devices-addphone");
        using var sc = new ServiceController("WSearch");
        ToggleSearchIndex.IsOn = sc.Status == ServiceControllerStatus.Running;
        ToggleStoreAppArchiving.IsOn = RegistryHelper.Read("HKLM", "Software\\Policies\\Microsoft\\Windows\\Appx", "AllowAutomaticAppArchiving") as int? == 1;
        ToggleSystemRestore.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableSR") == null;
        ToggleUpdateNotifications.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate", "SetAutoRestartNotificationDisable") == null &&
            RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "RestartNotificationsAllowed2") == null &&
            RegistryHelper.Read("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate", "SetUpdateNotificationLevel") == null;
        MoreOptions.Visibility = RegistryHelper.Exists("HKLM", "SOFTWARE\\AME\\Playbooks\\Applied\\{8bbb362c-858b-41d9-a9ea-83a4b9669c43}", "SelectedOptions") ? Visibility.Visible : Visibility.Collapsed;
        // Subscribe to events
        ToggleRecall.Toggled += RecallToggled;
        ToggleCopilot.Toggled += CopilotToggled;
        ToggleUpdates.Toggled += AutomaticUpdatesToggled;
        ToggleBackgroundApps.Toggled += BackgroundAppsToggled;
        ToggleDeliveryOptimizations.Toggled += DeliveryOptimizationsToggled;
        ToggleFSOAndGameBar.Toggled += FSOAndGameBarToggled;
        TogglePhoneLink.Toggled += PhoneLinkToggled;
        ToggleSearchIndex.Toggled += SearchIndexingConfiguration;
        DisableSearchIndex.Click += SearchIndexingConfiguration;
        MinimalSearchIndex.Click += SearchIndexingConfiguration;
        DefaultSearchIndex.Click += SearchIndexingConfiguration;
        ToggleStoreAppArchiving.Toggled += StoreAppArchivingToggled;
        ToggleSystemRestore.Toggled += SystemRestoreToggled;
        ToggleUpdateNotifications.Toggled += UpdateNotificationsToggled;
    }

    private void LocalizeControls()
    {
        AIExpander.Header = GetLocalizedString("AIExpander");
        AutoWinUpdates.Header = GetLocalizedString("AutoWinUpdates");
        AutoWinUpdates.Description = GetLocalizedString("AutoWinUpdatesDescription");
        BackgroundApps.Header = GetLocalizedString("BackgroundApps");
        DeliveryOptimizations.Header = GetLocalizedString("DeliveryOptimizations");
        FSOAndGameBar.Header = GetLocalizedString("FSOAndGameBar");
        PhoneLink.Header = GetLocalizedString("PhoneLink");
        SearchIndex.Header = GetLocalizedString("SearchIndex");
        SearchIndexConfigText.Text = GetLocalizedString("SearchIndexConfigText");
        DisableSearchIndex.Content = GetLocalizedString("DisableSearchIndex");
        MinimalSearchIndex.Content = GetLocalizedString("MinimalSearchIndex");
        DefaultSearchIndex.Content = GetLocalizedString("DefaultSearchIndex");
        StoreAppArchiving.Header = GetLocalizedString("StoreAppArchiving");
        SystemRestore.Header = GetLocalizedString("SystemRestore");
        UpdateNotifications.Header = GetLocalizedString("UpdateNotifications");
    }
    private void RecallToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("Recall toggled on");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "DisableAIDataAnalysis");

        }
        else
        {
            LogHelper.LogInfo("Recall toggled off");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "DisableAIDataAnalysis", 1, "REG_DWORD");
        }
    }

    private async void CopilotToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        var usersid = RegistryHelper.GetCurrentUserSid();
        ToggleCopilotProgress.IsActive = true;
        ToggleCopilot.IsEnabled = false;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("Copilot Toggled On");
            var IsCopilotAvailable = RegistryHelper.Read("HKCU", "Software\\Microsoft\\Windows\\Shell\\Copilot", "IsCopilotAvailable") as int? == 1;
            if (!IsCopilotAvailable) { RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowCopilotButton", 1, "REG_DWORD"); }
            if (IsCopilotAvailable) { await CommandHelper.StartInCmd("winget install -e --id 9NHT9RB2F4HD --uninstall-previous -h --accept-source-agreements --accept-package-agreements --force --disable-interactivity"); }
            foreach (var process in Process.GetProcessesByName("explorer"))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }
            }
            RegistryHelper.Delete("HKU", $"{usersid}\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot", "TurnOffWindowsCopilot");
            await CommandHelper.RunProcess("explorer", wait: false, hidden: false);
            

        }
        else
        {
            LogHelper.LogInfo("Copilot Toggled Off");
            await CommandHelper.StartInPowershell("Get-AppxPackage -AllUsers Microsoft.Copilot* | Remove-AppxPackage -AllUsers");
            foreach (var process in Process.GetProcessesByName("explorer"))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }
            }
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowCopilotButton", 0, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot", "TurnOffWindowsCopilot", 1, "REG_DWORD");
            await CommandHelper.RunProcess("explorer", wait: false, hidden: false);
        }
        ToggleCopilotProgress.IsActive = false;
        ToggleCopilot.IsEnabled = true;
    }

    private void AutomaticUpdatesToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[AutomaticUpdates]: Toggled off");
            ReplaceOption("auto-updates-default", "auto-updates-disable");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AUOptions", "2", "REG_DWORD");
        }
        else
        {
            LogHelper.LogInfo("[AutomaticUpdates]: Toggled on");
            ReplaceOption("auto-updates-disable", "auto-updates-default");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AUOptions");
        }
    }

    private void BackgroundAppsToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        var usersid = RegistryHelper.GetCurrentUserSid();
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("Background Apps Toggled On");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 1, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search", "BackgroundAppGlobalToggle", 0, "REG_DWORD");
        }
        else
        {
            LogHelper.LogInfo("Background Apps Toggled Off");
            RegistryHelper.Delete("HKU", $"{usersid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled");
            RegistryHelper.Delete("HKU", $"{usersid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search", "BackgroundAppGlobalToggle");
        }
    }

    // too many problems
    // private async void CPUIdleToggled(object sender, RoutedEventArgs e)
    // {
    //     var toggle = (ToggleSwitch)sender;
    //     if (toggle.IsOn)
    //     {
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current sub_processor 5d76a2ca-e8c0-402f-a133-2158492d58ad 0");
    //         await CommandHelper.RunProcess("powercfg", "/setactive scheme_current");
    //     }
    //     else
    //     {
    //         var psi = new ProcessStartInfo
    //         {
    //             FileName = "powershell.exe",
    //             Arguments = "-NonI -NoP -C \"Get-CimInstance Win32_Processor | Foreach-Object { if ([int]$_.NumberOfLogicalProcessors -gt [int]$_.NumberOfCores) { exit 262 } }\"",
    //             UseShellExecute = false,
    //             CreateNoWindow = true
    //         };
    //         using var process = Process.Start(psi);
    //         if (process == null)
    //         {
    //             toggle.IsOn = true;
    //             return;
    //         }
    //         await process.WaitForExitAsync();
    //         if (process.ExitCode == 262)
    //         {
    //             ShowInfoDialog("CPUIdle_SMT_Title", "CPUIdle_SMT_Content");
    //             toggle.IsOn = true;
    //             return;
    //         }
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current sub_processor 5d76a2ca-e8c0-402f-a133-2158492d58ad 1");
    //         await CommandHelper.RunProcess("powercfg", "/setactive scheme_current");
    //     }
    // }

    private void DeliveryOptimizationsToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization", "DODownloadMode", 0, "REG_DWORD");
        }
        else
        {
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization", "DODownloadMode");
        }
    }

    private void FSOAndGameBarToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        var usersid = RegistryHelper.GetCurrentUserSid();
        if (toggle.IsOn)
        {
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\PolicyManager\\default\\ApplicationManagement\\AllowGameDVR", "value", 1, "REG_DWORD");
            RunAsTi.RunAsTrustedInstaller("cmd.exe /c reg add \"HKLM\\SOFTWARE\\Microsoft\\WindowsRuntime\\ActivatableClassId\\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter\" /v \"ActivationType\" /t REG_DWORD /d \"1\" /f");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 0, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_EFSEFeatureFlags", 0, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 0, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_Enabled", 1, "REG_DWORD");
            RegistryHelper.Delete("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_DSEBehavior");
            RegistryHelper.Delete("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_FSEBehavior");
            RegistryHelper.Delete("HKU", $"{usersid}\\System\\GameBar", "GamePanelStartupTipIndex");
            RegistryHelper.Delete("HKU", $"{usersid}\\System\\GameBar", "ShowStartupPanel");
            RegistryHelper.Delete("HKU", $"{usersid}\\System\\GameBar", "UseNexusForGameBarEnabled");
            RegistryHelper.Delete("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "__COMPAT_LAYER");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "AppCaptureEnabled");
            RegistryHelper.DeleteKey("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR");
        }
        else
        {
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\PolicyManager\\default\\ApplicationManagement\\AllowGameDVR", "value", 0, "REG_DWORD");
            RunAsTi.RunAsTrustedInstaller("cmd.exe /c reg add \"HKLM\\SOFTWARE\\Microsoft\\WindowsRuntime\\ActivatableClassId\\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter\" /v \"ActivationType\" /t REG_DWORD /d \"0\" /f");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_DSEBehavior", 2, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", 1, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_EFSEFeatureFlags", 0, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_FSEBehavior", 2, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameConfigStore", "GameDVR_Enabled", 0, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameBar", "GamePanelStartupTipIndex", 3, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameBar", "ShowStartupPanel", 0, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\System\\GameBar", "UseNexusForGameBarEnabled", 0, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "__COMPAT_LAYER", "~ DISABLEDXMAXIMIZEDWINDOWEDMODE", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "AppCaptureEnabled", 0, "REG_DWORD");
        }
    }

    private void PhoneLinkToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "NoConnectedUser");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsStore\\WindowsUpdate", "AutoDownload", 4, "REG_DWORD");
            if (RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "SettingsPageVisibility") is not string hiddenpages) { return; }
            LogHelper.LogInfo("Old hidden settings pages: " + hiddenpages);
            var newhiddenpages = string.Join(";", hiddenpages.Split(';').Where(item => item != "mobile-devices-addphone"));
            LogHelper.LogInfo("New hidden settings pages: " + newhiddenpages);
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "SettingsPageVisibility", newhiddenpages, "REG_SZ");
        }
        else
        {
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "NoConnectedUser", 1, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsStore\\WindowsUpdate", "AutoDownload", 2, "REG_DWORD");
            if (RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "SettingsPageVisibility") is not string hiddenpages) { return; }
            LogHelper.LogInfo("Old hidden settings pages: " + hiddenpages);
            var hiddenpageslist = hiddenpages.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList();
            if (!hiddenpageslist.Contains("mobile-devices-addphone")) { hiddenpageslist.Add("mobile-devices-addphone"); }
            var newhiddenpages = string.Join(";", hiddenpageslist);
            LogHelper.LogInfo("New hidden settings pages: " + newhiddenpages);
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "SettingsPageVisibility", newhiddenpages, "REG_SZ");
        }
    }

    private async void SearchIndexingConfiguration(object sender, RoutedEventArgs e)
    {
        var indexConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts\\indexConf.cmd");
        if (sender is ToggleSwitch toggle)
        {
            if (toggle.IsOn)
            {
                await CommandHelper.RunProcess($"{indexConfig}", "/start");
            }
            else
            {
                await CommandHelper.RunProcess($"{indexConfig}", "/stop");
            }
        }

        if (sender is Button button)
        {
            switch (button.Tag)
            {
                case "DisableSearchIndex":
                    await CommandHelper.RunProcess($"{indexConfig}", "/stop");
                    await CommandHelper.RunProcess($"{indexConfig}", "/cleanpolicies");
                    ToggleSearchIndex.Toggled -= SearchIndexingConfiguration;
                    ToggleSearchIndex.IsOn = false;
                    ToggleSearchIndex.Toggled += SearchIndexingConfiguration;
                    break;
                case "MinimalSearchIndex":
                    await CommandHelper.RunProcess($"{indexConfig}", "/stop");
                    await CommandHelper.RunProcess($"{indexConfig}", "/cleanpolicies");
                    await CommandHelper.RunProcess($"{indexConfig}", $"/include \"{Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)}\"");
                    await CommandHelper.RunProcess($"{indexConfig}", $"/include \"C:\\Windows\\AtmosphereDesktop\"");
                    await CommandHelper.RunProcess($"{indexConfig}", $"/exclude \"C:\\Users\"");
                    await CommandHelper.RunProcess($"{indexConfig}", "/start");
                    ToggleSearchIndex.Toggled -= SearchIndexingConfiguration;
                    ToggleSearchIndex.IsOn = true;
                    ToggleSearchIndex.Toggled += SearchIndexingConfiguration;
                    break;
                case "DefaultSearchIndex":
                    await CommandHelper.RunProcess($"{indexConfig}", "/stop");
                    await CommandHelper.RunProcess($"{indexConfig}", "/cleanpolicies");
                    await CommandHelper.RunProcess($"{indexConfig}", $"/include \"{Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)}\"");
                    await CommandHelper.RunProcess($"{indexConfig}", $"/include \"C:\\Windows\\AtmosphereDesktop\"");
                    await CommandHelper.RunProcess($"{indexConfig}", $"/include \"C:\\Users\"");
                    foreach (var user in Directory.GetDirectories("C:\\Users"))
                    {
                        var appdata = Path.Combine(user, "AppData");
                        var microsoftEdgeBackups = Path.Combine(user, "MicrosoftEdgeBackups");
                        if (Path.Exists(appdata)) { await CommandHelper.RunProcess($"{indexConfig}", $"/exclude \"{appdata}\""); }
                        if (Path.Exists(microsoftEdgeBackups)) { await CommandHelper.RunProcess($"{indexConfig}", $"/exclude \"{microsoftEdgeBackups}\""); }
                    }
                    await CommandHelper.RunProcess($"{indexConfig}", "/start");
                    ToggleSearchIndex.Toggled -= SearchIndexingConfiguration;
                    ToggleSearchIndex.IsOn = true;
                    ToggleSearchIndex.Toggled += SearchIndexingConfiguration;
                    break;
            }
        }
    }

    // too many problems
    // private async void SleepToggled(object sender, RoutedEventArgs e)
    // {
    //     var toggle = (ToggleSwitch)sender;
    //     if (toggle.IsOn)
    //     {
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 25dfa149-5dd1-4736-b5ab-e8a37b5b8187 1");
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 abfc2519-3608-4c2a-94ea-171b0ed546ab 1");
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 94ac6d29-73ce-41a6-809f-6363ba21b47e 1");
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 7bc4a2f9-d8fc-4469-b07b-33eb785aaca0 120");
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d 1");
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 2e601130-5351-4d9d-8e04-252966bad054 d502f7ee-1dc7-4efd-a55d-f04b6f5c0545 1");
    //         await CommandHelper.RunProcess("powercfg", "/setactive scheme_current");
    //     }
    //     else
    //     {
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 25dfa149-5dd1-4736-b5ab-e8a37b5b8187 0");
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 abfc2519-3608-4c2a-94ea-171b0ed546ab 0");
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 94ac6d29-73ce-41a6-809f-6363ba21b47e 0");
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 7bc4a2f9-d8fc-4469-b07b-33eb785aaca0 0");
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d 0");
    //         await CommandHelper.RunProcess("powercfg", "/setacvalueindex scheme_current 2e601130-5351-4d9d-8e04-252966bad054 d502f7ee-1dc7-4efd-a55d-f04b6f5c0545 0");
    //         await CommandHelper.RunProcess("powercfg", "/setactive scheme_current");
    //     }
    // }

    private void StoreAppArchivingToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            RegistryHelper.AddOrUpdate("HKLM", "Software\\Policies\\Microsoft\\Windows\\Appx", "AllowAutomaticAppArchiving", 1, "REG_DWORD");
        }
        else
        {
            RegistryHelper.AddOrUpdate("HKLM", "Software\\Policies\\Microsoft\\Windows\\Appx", "AllowAutomaticAppArchiving", 0, "REG_DWORD");
        }
    }

    private void SystemRestoreToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableSR");
        }
        else
        {
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableSR", 1, "REG_DWORD");
        }
    }

    private void UpdateNotificationsToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate", "SetAutoRestartNotificationDisable");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "RestartNotificationsAllowed2");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate", "SetUpdateNotificationLevel");
        }
        else
        {
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate", "SetAutoRestartNotificationDisable", 1, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "RestartNotificationsAllowed2", 0, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate", "SetUpdateNotificationLevel", 2, "REG_DWORD");
        }
    }

    private void MoreOptionsClick(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow.Content is ShellPage shellPage)
        {
            shellPage.RootFrame.Navigate(typeof(AtmosphereSettingsPage));
        }
    }

    private void ReplaceOption(string option, string newOption)
    {
        var options = RegistryHelper.Read("HKLM", "SOFTWARE\\AME\\Playbooks\\Applied\\{8bbb362c-858b-41d9-a9ea-83a4b9669c43}", "SelectedOptions") as string[] ?? Array.Empty<string>();
        LogHelper.LogInfo("Original Value: " + string.Join(" ", options));
        // Manual loop because Array.IndexOf returns 0
        var optionindex = -1; // Initialize optionindex
        for (var i = 0; i < options.Length; i++)
        {
            if (options[i] == option)
            {
                optionindex = i;
            }
        }
        if (optionindex > 1) // Small check
        {
            options[optionindex] = newOption;
            LogHelper.LogInfo("New value: " + string.Join(" ", options));
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\AME\\Playbooks\\Applied\\{8bbb362c-858b-41d9-a9ea-83a4b9669c43}", "SelectedOptions", options, "REG_MULTI_SZ");
        }
    }
    // private void ShowInfoDialog(string titleKey, string contentKey)
    // {
    //     var title = GetLocalizedString(titleKey);
    //     var content = GetLocalizedString(contentKey);
    //     var primaryText = GetLocalizedString("Dialog_PrimaryText");
    //     App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
    //     {
    //         var dialog = new ContentDialog
    //         {
    //             Title = title,
    //             Content = content,
    //             PrimaryButtonText = primaryText,
    //             DefaultButton = ContentDialogButton.Primary,
    //             XamlRoot = App.MainWindow.Content.XamlRoot
    //         };
    // 
    //         await dialog.ShowAsync();
    //     });
    // }
}

