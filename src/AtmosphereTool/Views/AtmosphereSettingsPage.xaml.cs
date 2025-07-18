using AtmosphereTool.Helpers;
using AtmosphereTool.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AtmosphereTool.Views;

public sealed partial class AtmosphereSettingsPage : Page
{
    public AtmosphereSettingsViewModel ViewModel
    {
        get;
    }
    private readonly AtmosphereLocalizationHelper _localizationHelper = new();
    public AtmosphereSettingsPage()
    {
        ViewModel = App.GetService<AtmosphereSettingsViewModel>();
        InitializeComponent();
        LoadControls();
        LocalizeControls();
    }
    private void LoadControls()
    {
        // Unsubscribe
        ToggleUpdates.Toggled -= AutomaticUpdates;
        AtmosphereUI.Toggled -= ToggleUIModification;
        ToggleHibernation.Toggled -= HibernationToggle;
        TogglePS.Toggled -= TogglePowerSaving;
        ToggleStartMenu.Toggled -= ToggleStartMenuModifications;
        OldContextMenu.Toggled -= ToggleOldContextMenu;
        TranslucentFlyouts.Toggled -= TranslucentFlyoutsToggle;
        // Load state
        var options = RegistryHelper.Read("HKLM", "SOFTWARE\\AME\\Playbooks\\Applied\\{8bbb362c-858b-41d9-a9ea-83a4b9669c43}", "SelectedOptions") as string[] ?? [];
        DefenderCard.Visibility = options.Contains("ameliorate") ? Visibility.Visible : Visibility.Collapsed;
        AutoUpdatesHeader.Visibility = options.Contains("ameliorate") ? Visibility.Visible : Visibility.Collapsed;
        ConfigServicesHeader.Visibility = options.Contains("ameliorate") ? Visibility.Visible : Visibility.Collapsed;
        ConfigTelemetryHeader.Visibility = options.Contains("ameliorate") ? Visibility.Visible : Visibility.Collapsed;
        RepairWindowsHeader.Visibility = options.Contains("ameliorate") ? Visibility.Visible : Visibility.Collapsed;
        ToggleUpdates.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AUOptions") != null;
        AtmosphereUI.IsOn = options.Contains("modify-ui");
        ToggleHibernation.IsOn = PowerHelper.IsHibernationEnabled();
        TogglePS.IsOn = options.Contains("disable-power-saving");
        ToggleStartMenu.IsOn = options.Contains("openshell");
        var usersid = RegistryHelper.GetCurrentUserSid();
        OldContextMenu.Visibility = Environment.OSVersion.Version.Build > 22000 ? Visibility.Visible : Visibility.Collapsed;
        OldContextMenu.IsOn = RegistryHelper.Read("HKU", $"{usersid}\\Software\\Classes\\CLSID\\{{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}}\\InprocServer32", "(Default)") != null;
        // Resubscribe
        ToggleUpdates.Toggled += AutomaticUpdates;
        AtmosphereUI.Toggled += ToggleUIModification;
        ToggleHibernation.Toggled += HibernationToggle;
        TogglePS.Toggled += TogglePowerSaving;
        ToggleStartMenu.Toggled += ToggleStartMenuModifications;
        OldContextMenu.Toggled += ToggleOldContextMenu;
    }
    private void LocalizeControls()
    {
        AtmosphereConfigHeader.Header = _localizationHelper.GetLocalizedString("AtmosphereConfigHeader");
        AtmosphereConfigHeader.Description = _localizationHelper.GetLocalizedString("AtmosphereConfigDescription");
        ToggleDefender.Content = _localizationHelper.GetLocalizedString("ToggleDefender");
        MitigationsHeader.Header = _localizationHelper.GetLocalizedString("MitigationsHeader");
        WindowsMitigations.Content = _localizationHelper.GetLocalizedString("WindowsMitigations");
        DisableMitigations.Content = _localizationHelper.GetLocalizedString("DisableMitigations");
        AutoUpdatesHeader.Header = _localizationHelper.GetLocalizedString("AutoUpdatesHeader");
        AutoUpdatesHeader.Description = _localizationHelper.GetLocalizedString("AutoUpdatesDescription");
        AtmosphereUIModsHeader.Header = _localizationHelper.GetLocalizedString("AtmosphereUIModsHeader");
        HibernationHeader.Header = _localizationHelper.GetLocalizedString("HibernationHeader");
        PowerSavingHeader.Header = _localizationHelper.GetLocalizedString("PowerSavingHeader");
        VBSHeader.Header = _localizationHelper.GetLocalizedString("VBSHeader");
        ConfigVBS.Content = _localizationHelper.GetLocalizedString("ConfigVBS");
        WinStartMenuHeader.Header = _localizationHelper.GetLocalizedString("WinStartMenuHeader");
        OldContextMenuHeader.Header = _localizationHelper.GetLocalizedString("OldContextMenuHeader");
        TroubleshootingHeader.Header = _localizationHelper.GetLocalizedString("TroubleshootingHeader");
        TroubleshootingHeader.Description = _localizationHelper.GetLocalizedString("TroubleshootingDescription");
        TFInfoText.Text = _localizationHelper.GetLocalizedString("TFInfoText");
        MicaExplorerHeader.Header = _localizationHelper.GetLocalizedString("MicaExplorerHeader");
        ConfigServicesHeader.Header = _localizationHelper.GetLocalizedString("ConfigServicesHeader");
        Services.Content = _localizationHelper.GetLocalizedString("Services");
        ConfigTelemetryHeader.Header = _localizationHelper.GetLocalizedString("ConfigTelemetryHeader");
        Telemetry.Content = _localizationHelper.GetLocalizedString("Telemetry");
        RepairWindowsHeader.Header = _localizationHelper.GetLocalizedString("RepairWindowsHeader");
        RepairComponents.Content = _localizationHelper.GetLocalizedString("RepairComponents");
        UninstallAtmosphereHeader.Header = _localizationHelper.GetLocalizedString("UninstallAtmosphereHeader");
        RemoveOS.Content = _localizationHelper.GetLocalizedString("RemoveOS");
    }
    private async void ToggleWinDefender(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        button.IsEnabled = false;
        var (Success, Error) = await CommandHelper.StartInPowershell(@"C:\Windows\AtmosphereModules\Scripts\ScriptWrappers\ToggleDefender.ps1", false, true, true);
        if (Success)
        {
            button.IsEnabled = true;
        }
        else
        {
            button.IsEnabled = true;
            LogHelper.LogError($"[ToggleWinDefender]: Failed to launch defender script: {Error}");
            if (Error != null)
            {
                ShowErrorDialog("Failed to launch defender script ", Error);
            }
        }
    }
    private async void HardwareMitigations(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        switch (button.Tag)
        {
            case "WindowsMitigations":
                LogHelper.LogInfo("[HardwareMitigations]: WindowsMitigations clicked");
                var confirmaction = await ShowConfirmationDialogAsync("Mitigations_Title", "Mitigations_Content");
                if (!confirmaction)
                {
                    LogHelper.LogInfo("[HardwareMitigations]: User Cancelled");
                    return;
                }
                var enableall = await ShowConfirmationDialogAsync("AllMitigations_Title", "AllMitigations_Content");
                if (enableall)
                {
                    LogHelper.LogInfo("[HardwareMitigations]: Enabling all mitigations");
                    if (!(CommandHelper.StartInCmd(@"C:\Windows\AtmosphereDesktop\7. Security\Mitigations\Enable All Mitigations.cmd /silent", true, true).Result.Success))
                    {
                        LogHelper.LogError("[HardwareMitigations]: Something went wrong. Line 58");
                        ShowErrorDialog("Error", "Something went wrong.");
                    }
                }
                else
                {
                    LogHelper.LogInfo("[HardwareMitigations]: Enabling Default mitigations");
                    if (!(CommandHelper.StartInCmd(@"C:\Windows\AtmosphereDesktop\7. Security\Mitigations\Set Windows Default Mitigations.cmd /silent", true, true).Result.Success))
                    {
                        LogHelper.LogError("[HardwareMitigations]: Something went wrong. Line 58");
                        ShowErrorDialog("Error", "Something went wrong.");
                    }
                }
                LogHelper.LogInfo("[HardwareMitigations]: Mitigations sucessfully applied");
                var restart = await ShowConfirmationDialogAsync("MitigationsEnabled_Title", "Dialog_Content_Restart");
                if (restart)
                {
                    LogHelper.LogInfo("Restarting system");
                    await CommandHelper.StartInCmd(@"shutdown /r /t 0 /f");
                }
                break;
            case "DisableMitigations":
                LogHelper.LogInfo("[HardwareMitigations]: DisableMitigations clicked");
                var confirmactionex = await ShowConfirmationDialogAsync("Mitigations_Title", "Mitigations_Content");
                if (!confirmactionex)
                {
                    LogHelper.LogInfo("[HardwareMitigations]: User cancelled");
                    return;
                }
                if (!(CommandHelper.StartInCmd(@"C:\Windows\AtmosphereDesktop\7. Security\Mitigations\Disable All Mitigations.cmd /silent", true, true).Result.Success))
                {
                    LogHelper.LogError("[HardwareMitigations]: Something went wrong. Line 89");
                    ShowErrorDialog("Error", "Something went wrong.");
                }
                LogHelper.LogInfo("[HardwareMitigations]: Mitigations sucessfully disabled");
                var restartsys = await ShowConfirmationDialogAsync("MitigationsDisabled_Title", "Dialog_Content_Restart");
                if (restartsys)
                {
                    LogHelper.LogInfo("Restarting system");
                    await CommandHelper.StartInCmd(@"shutdown /r /t 0 /f");
                }
                break;
        }
    }
    private void AutomaticUpdates(object sender, RoutedEventArgs e)
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
    private async void ToggleUIModification(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        // Get Starup Directory
        var usersid = RegistryHelper.GetCurrentUserSid();
        if (usersid == null)
        {
            toggle.IsOn = true;
            return;
        }
        var profilepath = RegistryHelper.Read("HKLM", $"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ProfileList\\{usersid}", "ProfileImagePath");
        // Safely cast profilepathstr
        if (profilepath is not string profilepathstr)
        {
            toggle.IsOn = true;
            return;
        }
        var startupdir = Path.Combine(profilepathstr, "AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup");
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("UIModifications Toggled on");
            ReplaceOption("keep-ui", "modify-ui");
            await CommandHelper.StartInPowershell("C:\\Windows\\AtmosphereModules\\AtmosphereTool\\UI\\UIApply.ps1", true, true);
        }
        else
        {
            LogHelper.LogInfo("UIModifications Toggled off");
            ReplaceOption("modify-ui", "keep-ui");
            await CommandHelper.StartInPowershell("Unregister-ScheduledTask -TaskName \"AccentColorizer\" -Confirm:$false -ErrorAction SilentlyContinue");
            await CommandHelper.StartInCmd("regsvr32 /u \"C:Windows\\AtmosphereDesktop\\4. Interface Tweaks\\File Explorer Customization\\Mica Explorer\\ExplorerBlurMica.dll\"");
            await CommandHelper.StartInPowershell("C:\\Windows\\AtmosphereModules\\Scripts\\ScriptWrappers\\ToggleTF.ps1 -Operation 3", true, true);
            var linkpath = Path.Combine(startupdir, "TranslucentFlyouts.lnk");
            if (File.Exists(linkpath))
            {
                File.Delete(linkpath);
            }
        }
    }
    private async void HibernationToggle(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Toggling Hibernation");
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            await CommandHelper.StartInCmd("powercfg /hibernate on");
        }
        else
        {
            await CommandHelper.StartInCmd("powercfg /hibernate off");
        }
    }
    private async void TogglePowerSaving(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Toggling PowerSaving");
        var toggle = (ToggleSwitch)sender;
        toggle.IsEnabled = false;
        if (toggle.IsOn)
        {
            var (Success, Error) = await CommandHelper.StartInPowershell("C:\\Windows\\AtmosphereModules\\Scripts\\ScriptWrappers\\DefaultPowerSaving.ps1", true, true);
            if (Success)
            {
                LogHelper.LogInfo("Sucessfully toggled powersaving on");
            }
            else
            {
                LogHelper.LogError("Failed to enable power saving. Error: " + Error);
                toggle.IsOn = false;
                toggle.IsEnabled = true;
            }
        }
        else
        {
            var (Success, Error) = await CommandHelper.StartInPowershell("C:\\Windows\\AtmosphereModules\\Scripts\\ScriptWrappers\\DisablePowerSaving.ps1 -Silent", true, true);
            if (Success)
            {
                LogHelper.LogInfo("Sucessfully toggled powersaving off");
            }
            else
            {
                LogHelper.LogError("Failed to disable power saving. Error: " + Error);
                toggle.IsOn = false;
                toggle.IsEnabled = true;
            }
        }
    }
    private async void ConfigureVBSClick(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Configure Virtualization Based Security Clicked");
        await CommandHelper.StartInPowershell("C:\\Windows\\AtmosphereModules\\Scripts\\ScriptWrappers\\ConfigVBS.ps1", false, true);
    }
    private async void ToggleStartMenuModifications(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Start Menu Modifications toggle toggled");
        var toggle = (ToggleSwitch)sender;
        toggle.IsEnabled = false;
        SMMProgressRing.IsActive = true;
        if (toggle.IsOn)
        {
            if (!Directory.Exists("C:\\Program Files\\Open-Shell"))
            {
                LogHelper.LogInfo("Open-Shell not found. Installing...");
                var (Success, Error) = await CommandHelper.StartInPowershell("C:\\Windows\\AtmosphereModules\\AtmosphereTool\\UI\\OpenShell.ps1", true, true);
                if (Success)
                {
                    LogHelper.LogInfo("Open-Shell successfully installed");
                    toggle.IsEnabled = true;
                    SMMProgressRing.IsActive = false;
                }
                else
                {
                    LogHelper.LogError("Open-Shell install failed. Error: " + Error);
                    toggle.IsOn = false;
                    toggle.IsEnabled = true;
                    SMMProgressRing.IsActive = false;
                    return;
                }
            }
            else
            {
                LogHelper.LogInfo("Open-Shell found, adding startup value");
                RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", "Open-Shell Start Menu", "\"C:\\Program Files\\Open-Shell\\StartMenu.exe\" -autorun", "REG_SZ");
                toggle.IsEnabled = true;
                SMMProgressRing.IsActive = false;
            }
        }
        else
        {
            LogHelper.LogInfo("Deleting Open-Shell startup value");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", "Open-Shell Start Menu");
            await CommandHelper.StartInCmd("taskkill /f /im StartMenu.exe");
            toggle.IsEnabled = true;
            SMMProgressRing.IsActive = false;
        }
    }
    private void ToggleOldContextMenu(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        var usersid = RegistryHelper.GetCurrentUserSid();
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("Old context menu toggled on");
            RegistryHelper.AddOrUpdate("HKU", $"{usersid}\\Software\\Classes\\CLSID\\{{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}}\\InprocServer32", "(Default)", string.Empty, "REG_SZ");
        }
        else
        {
            LogHelper.LogInfo("Old context menu toggled off");
            RegistryHelper.DeleteKey("HKU", $"{usersid}\\Software\\Classes\\CLSID\\{{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}}\\InprocServer32");
        }
    }
    private async void TranslucentFlyoutsToggle(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("Toggled Translucent Flyouts on");
            await CommandHelper.RunProcess("powershell.exe", "- NoProfile - ExecutionPolicy Bypass - File C:\\Windows\\AtmosphereModules\\Scripts\\ScriptWrappers\\ToggleTF.ps1 -Operation 2");
        }
        else
        {
            LogHelper.LogInfo("Toggled Translucent Flyouts off");
            await CommandHelper.RunProcess("powershell.exe", "- NoProfile - ExecutionPolicy Bypass - File C:\\Windows\\AtmosphereModules\\Scripts\\ScriptWrappers\\ToggleTF.ps1 -Operation 3");
        }
    }
    private async void TranslucentExplorerToggle(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("Toggled Translucent Explorer on");
            await CommandHelper.StartInCmd("regsvr32 /u \"C:\\Windows\\AtmosphereDesktop\\4. Interface Tweaks\\File Explorer Customization\\Mica Explorer\\ExplorerBlurMica.dll\"");
            LogHelper.LogInfo("Restarting Explorer...");
            await CommandHelper.StartInCmd("taskkill /f /im explorer.exe");
            await CommandHelper.StartInCmd("explorer");
        }
        else
        {
            LogHelper.LogInfo("Toggled Translucent Explorer off");
            await CommandHelper.StartInCmd("regsvr32 \"C:\\Windows\\AtmosphereDesktop\\4. Interface Tweaks\\File Explorer Customization\\Mica Explorer\\ExplorerBlurMica.dll\"");
            LogHelper.LogInfo("Restarting Explorer...");
            await CommandHelper.StartInCmd("taskkill /f /im explorer.exe");
            await CommandHelper.StartInCmd("explorer");
        }
    }
    private async void ConfigureServicesClick(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Configure Services Clicked");
        await CommandHelper.StartInCmd("C:\\Windows\\AtmosphereDesktop\\8. Troubleshooting\\Set services to defaults.cmd", true, true, false);
    }
    private async void ConfigureTelemetryClick(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Configure Telemetry Clicked");
        await CommandHelper.StartInCmd("C:\\Windows\\AtmosphereDesktop\\8. Troubleshooting\\Telemetry Components.cmd", true, true, false);
    }
    private async void RepairWindowsClick(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Repair Windows Clicked");
        await CommandHelper.StartInCmd("C:\\Windows\\AtmosphereDesktop\\8. Troubleshooting\\Repair Windows Components.cmd", true, true, false);
    }
    private void UninstallAtmosphereClick(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Uninstall Atmosphere Clicked");
        if (App.MainWindow.Content is ShellPage shellPage)
        {
            shellPage.RootFrame.Navigate(typeof(UninstallProgressPage));
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
    private void ShowInfoDialog(string titleKey, string contentKey)
    {
        var title = _localizationHelper.GetLocalizedString(titleKey);
        var content = _localizationHelper.GetLocalizedString(contentKey);
        var primaryText = _localizationHelper.GetLocalizedString("Dialog_PrimaryText");
        App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryText,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.MainWindow.Content.XamlRoot
            };

            await dialog.ShowAsync();
        });
    }
    private void ShowErrorDialog(string title, string content)
    {
        var primaryText = _localizationHelper.GetLocalizedString("Dialog_PrimaryText");
        App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryText,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.MainWindow.Content.XamlRoot
            };

            await dialog.ShowAsync();
        });
    }
    private async Task<bool> ShowConfirmationDialogAsync(string titleKey, string contentKey, string primaryKey = "Dialog_PrimaryText", string closeKey = "Dialog_CloseText")
    {
        var title = _localizationHelper.GetLocalizedString(titleKey);
        var content = _localizationHelper.GetLocalizedString(contentKey);
        var primaryText = _localizationHelper.GetLocalizedString(primaryKey);
        var closeText = _localizationHelper.GetLocalizedString(closeKey);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot // Really important
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
