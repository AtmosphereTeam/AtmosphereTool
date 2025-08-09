using AtmosphereTool.Helpers;
using AtmosphereTool.Views;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;
using WinResLoader = Windows.ApplicationModel.Resources.ResourceLoader;

namespace AtmosphereTool.FeaturePages;

public sealed partial class InterfaceTweaks : Page
{
    private readonly WinResLoader _resourceLoader = WinResLoader.GetForViewIndependentUse("FeaturePages");

    public string GetLocalizedString(string key)
    {
        var localized = _resourceLoader.GetString(key);
        return string.IsNullOrEmpty(localized) ? key : localized;
    }
    private readonly string UserSID = RegistryHelper.GetCurrentUserSid();
    public InterfaceTweaks()
    {
        InitializeComponent();
        LoadControls();
        LocalizeControls();
    }

    private void LoadControls()
    {
        // Load settings
        AltTabToggle.IsOn = RegistryHelper.Read("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "AltTabSettings") == null;
        ExtractToggle.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{b8cdcb65-b1bf-4b42-9428-1dfdb7ee92af}") == null &&
            RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{BD472F60-27FA-11cf-B8B4-444553540000}") == null &&
            RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{EE07CEF5-3441-4CFB-870A-4002C724783A}") == null &&
            RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{D12E3394-DE4B-4777-93E9-DF0AC88F8584}") == null;
        RunWithPriorityToggle.IsOn = RegistryHelper.Exists("HKCR", "exefile\\shell\\Priority");
        TakeOwnershipToggle.IsOn = RegistryHelper.Exists("HKCR", "*\\shell\\Take Ownership") &&
            RegistryHelper.Exists("HKCR", "Directory\\shell\\Take Ownership") &&
            RegistryHelper.Exists("HKCR", "*\\shell\\runas") &&
            RegistryHelper.Exists("HKCR", "Drive\\shell\\runas");
        AddTerminalsToggle.IsOn = RegistryHelper.Exists("HKCR", "TermsRunAsTI");
        OldContextMenuHeader.Visibility = Environment.OSVersion.Version.Build > 22000 ? Visibility.Collapsed : Visibility.Visible;
        OldContextMenu.IsOn = RegistryHelper.Exists("HKU", $"{UserSID}\\Software\\Classes\\CLSID\\{{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}}\\InprocServer32", "(Default)");
        EdgeSwipeToggle.IsOn = RegistryHelper.Read("HKLM", "Software\\Policies\\Microsoft\\Windows\\EdgeUI", "AllowEdgeSwipe") as int? != 0;
        AppIconsOnThumbnailsToggle.IsOn = RegistryHelper.Read("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTypeOverlay") as int? != 0;
        AutomaticFolderDiscoveryToggle.IsOn = RegistryHelper.Read("HKCU", $"{UserSID}\\Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\Bags\\AllFolders\\Shell", "FolderType") as int? != 0;
        CompactViewToggle.IsOn = RegistryHelper.Read("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "UseCompactMode") as int? != 0;
        FoldersInThisPCToggle.IsOn = FoldersInThisPCLoad();
        GalleryToggle.IsOn = RegistryHelper.Read("HKCU", $"{UserSID}\\Software\\Classes\\CLSID\\{{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}}", "System.IsPinnedToNameSpaceTree") as int? == 0;
        QuickAccessToggle.IsOn = !RegistryHelper.Exists("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "HubMode");
        RemovableDrivesInSidebarToggle.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace\\DelegateFolders\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}", "(Default)") as string == "Removable Drives" &&
            RegistryHelper.Read("HKLM", "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace\\DelegateFolders\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}", "(Default)") as string == "Removable Drives";
        OldFlyoutsExpander.Visibility = Environment.OSVersion.Version.Build > 22000 ? Visibility.Collapsed : Visibility.Visible;
        OldBatteryFlyoutToggle.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ImmersiveShell", "UseWin32BatteryFlyout") as int? == 1;
        DateAndTimeFlyoutToggle.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ImmersiveShell", "UseWin32TrayClockExperience") as int? == 1;
        OldVolumeFlyoutToggle.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\MTCUVC", "EnableMtcUvc") as int? == 0;
        ShortcutIconToggle.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Icons", "29") as string != "C:\\Windows\\AtmosphereModules\\Other\\Blank.ico,0";
        ShortcutNameToggle.IsOn = !RegistryHelper.Exists("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates", "ShortcutNameTemplate");
        SnapLayoutsToggle.IsOn = RegistryHelper.Read("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "EnableSnapAssistFlyout") as int? == 1 &&
            RegistryHelper.Read("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "EnableSnapBar") as int? == 1;
        VerboseStatusMessageToggle.IsOn = RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "verbosestatus") as int? == 1;
        VisualEffectsToggle.IsOn = VisualEffectsLoad();
        MoreOptions.Visibility = RegistryHelper.Exists("HKLM", "SOFTWARE\\AME\\Playbooks\\Applied\\{8bbb362c-858b-41d9-a9ea-83a4b9669c43}", "SelectedOptions") ? Visibility.Visible : Visibility.Collapsed;
        // Subscribe
        AltTabToggle.Toggled += AltTabToggled;
        ExtractToggle.Toggled += ExtractToggled;
        RunWithPriorityToggle.Toggled += RunWithPriorityToggled;
        TakeOwnershipToggle.Toggled += TakeOwnershipToggled;
        AddTerminalsToggle.Toggled += AddTerminals;
        OldContextMenu.Toggled += ToggleOldContextMenu;
        EdgeSwipeToggle.Toggled += EdgeSwipeToggled;
        AppIconsOnThumbnailsToggle.Toggled += AppIconsOnThumbnailsToggled;
        AutomaticFolderDiscoveryToggle.Toggled += AutomaticFolderDiscoveryToggled;
        CompactViewToggle.Toggled += CompactViewToggled;
        FoldersInThisPCToggle.Toggled += FoldersInThisPCToggled;
        GalleryToggle.Toggled += GalleryToggled;
        QuickAccessToggle.Toggled += QuickAccessToggled;
        RemovableDrivesInSidebarToggle.Toggled += RemovableDrivesInSidebarToggled;
        OldBatteryFlyoutToggle.Toggled += OldBatteryFlyoutToggled;
        DateAndTimeFlyoutToggle.Toggled += DateAndTimeFlyoutToggled;
        OldVolumeFlyoutToggle.Toggled += OldVolumeFlyoutToggled;
        ShortcutIconToggle.Toggled += ShortcutIcon;
        ShortcutNameToggle.Toggled += ShortcutNameToggled;
        SnapLayoutsToggle.Toggled += SnapLayoutsToggled;
        VerboseStatusMessageToggle.Toggled += VerboseStatusMessageToggled;
        VisualEffectsToggle.Toggled += VisualEffects;
    }

    private void LocalizeControls()
    {
        AltTabCard.Header = GetLocalizedString("AltTabCard");
        ContextMenusExpander.Header = GetLocalizedString("ContextMenusExpander");
        ExtractCard.Header = GetLocalizedString("ExtractCardHeader");
        ExtractCard.Description = GetLocalizedString("ExtractCardDescription");
        RunWithPriorityCard.Header = GetLocalizedString("RunWithPriorityCardHeader");
        RunWithPriorityCard.Description = GetLocalizedString("RunWithPriorityCardDescription");
        DebloatSendToCard.Header = GetLocalizedString("DebloatSendToCard");
        TakeOwnershipCard.Header = GetLocalizedString("TakeOwnershipCardHeader");
        TakeOwnershipCard.Description = GetLocalizedString("TakeOwnershipCardDescription");
        AddTerminalsCard.Header = GetLocalizedString("AddTerminalsCardHeader");
        AddTerminalsCard.Description = GetLocalizedString("AddTerminalsCardDescription");
        AddTerminalsExtraText.Text = GetLocalizedString("AddTerminalsExtraText");
        AddTerminalsAll.Content = GetLocalizedString("AddTerminalsAll");
        AddTerminalsNoWinTerminal.Content = GetLocalizedString("AddTerminalsNoWinTerminal");
        AddTerminalsDisable.Content = GetLocalizedString("AddTerminalsDisable");
        OldContextMenuHeader.Header = GetLocalizedString("OldContextMenuHeader");
        EdgeSwipeCard.Header = GetLocalizedString("EdgeSwipeCardHeader");
        EdgeSwipeCard.Description = GetLocalizedString("EdgeSwipeCardDescription");
        FileExplorerCustomizationsExpander.Header = GetLocalizedString("FileExplorerCustomizationsExpander");
        AppIconsOnThumbnailsCard.Header = GetLocalizedString("AppIconsOnThumbnailsCard");
        AutomaticFolderDiscoveryCard.Header = GetLocalizedString("AutomaticFolderDiscoveryCard");
        CompactViewCard.Header = GetLocalizedString("CompactViewCard");
        FoldersInThisPCCard.Header = GetLocalizedString("FoldersInThisPCCard");
        GalleryCard.Header = GetLocalizedString("GalleryCard");
        QuickAccessCard.Header = GetLocalizedString("QuickAccessCard");
        RemovableDrivesInSidebarCard.Header = GetLocalizedString("RemovableDrivesInSidebarCard");
        OldFlyoutsExpander.Header = GetLocalizedString("OldFlyoutsExpander");
        OldBatteryFlyoutCard.Header = GetLocalizedString("OldBatteryFlyoutCard");
        OldDateAndTimeFlyout.Header = GetLocalizedString("OldDateAndTimeFlyout");
        OldVolumeFlyoutCard.Header = GetLocalizedString("OldVolumeFlyoutCard");
        ShortcutIconCard.Header = GetLocalizedString("ShortcutIconCard");
        ShortcutIconExtraText.Text = GetLocalizedString("ShortcutIconExtraText");
        ShortcutIconDefault.Content = GetLocalizedString("ShortcutIconDefault");
        ShortcutIconClassic.Content = GetLocalizedString("ShortcutIconClassic");
        ShortcutIconDisable.Content = GetLocalizedString("ShortcutIconDisable");
        ShortcutNameCard.Header = GetLocalizedString("ShortcutNameCardHeader");
        ShortcutNameCard.Description = GetLocalizedString("ShortcutNameCardDescription");
        SnapLayoutsCard.Header = GetLocalizedString("SnapLayoutsCard");
        VerboseStatusMessageCard.Header = GetLocalizedString("VerboseStatusMessageCard");
        VisualEffectsCard.Header = GetLocalizedString("VisualEffectsCard");
        VisualEffectsExtraText.Text = GetLocalizedString("VisualEffectsExtraText");
        VisualEffectsConfig.Content = GetLocalizedString("VisualEffectsConfig");
        MoreOptions.Header = GetLocalizedString("MoreOptions");
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        LogHelper.LogInfo("Navigated To InterfaceTweaks");
        if (App.MainWindow.Content is ShellPage shellPage)
        {
            shellPage.SetBreadcrumb(new Folder { Name = "Interface Tweaks", Page = typeof(InterfaceTweaks) });
        }
        if (e.Parameter == null || e.Parameter.ToString() == string.Empty) { return; }
        if (e.Parameter is string target)
        {
            var elementMap = new Dictionary<string, FrameworkElement>
            {
                { "AltTab", AltTabCard },
                // Context Menus
                { "Extract", ExtractCard },
                { "DebloatSendTo", DebloatSendToCard },
                { "TakeOwnership", TakeOwnershipCard },
                { "AddTerminals", AddTerminalsCard },
                { "OldContextMenu", OldContextMenuHeader },
                // EdgeSwipe
                { "EdgeSwipe", EdgeSwipeCard },
                // File Explorer
                { "AppIconsOnThumbnails", AutomaticFolderDiscoveryCard },
                { "CompactView", CompactViewCard },
                { "Gallery", GalleryCard },
                { "QuickAccess", QuickAccessCard },
                { "RemovableDrivesInSidebar", RemovableDrivesInSidebarCard },
                // Old Flyouts
                { "OldBatteryFlyout", OldBatteryFlyoutCard },
                { "OldDateAndTimeFlyout", OldDateAndTimeFlyout },
                { "OldVolumeFlyout", OldVolumeFlyoutCard },
                // End
                { "ShortcutIcon", ShortcutIconCard },
                { "ShortcutName", ShortcutNameCard },
                { "SnapLayouts", SnapLayoutsCard },
                { "VerboseStatusMessage", VerboseStatusMessageCard },
                { "VisualEffects", VisualEffectsCard }
            };

            var elementGroups = new Dictionary<string, string>
            {
                // No Expander
                { "AltTab", "None" },
                { "EdgeSwipe", "None" },
                { "ShortcutIcon", "None" },
                { "ShortcutName", "None" },
                { "SnapLayouts", "None" },
                { "VerboseStatusMessage", "None" },
                { "VisualEffects", "None" },
                // Context Menus
                { "Extract", "ContextExpander" },
                { "DebloatSendTo", "ContextExpander" },
                { "TakeOwnership", "ContextExpander" },
                { "AddTerminals", "ContextExpander" },
                { "OldContextMenu", "ContextExpander" },
                // File Explorer
                { "AppIconsOnThumbnails", "ExplorerExpander" },
                { "CompactView", "ExplorerExpander" },
                { "Gallery", "ExplorerExpander" },
                { "QuickAccess", "ExplorerExpander" },
                { "RemovableDrivesInSidebar", "ExplorerExpander" },
                // Old Flyouts
                { "OldBatteryFlyout", "FlyoutsExpander" },
                { "OldDateAndTimeFlyout", "FlyoutsExpander" },
                { "OldVolumeFlyout", "FlyoutsExpander" }
            };

            if (elementMap.TryGetValue(target, out var element))
            {
                if (elementGroups.TryGetValue(target, out var expander))
                {
                    switch (expander)
                    {
                        case "None":
                            _ = HighlightBorderAsync((SettingsCard)element);
                            break;
                        case "ContextExpander":
                            _ = HighlightBorderAsync((SettingsCard)element, ContextMenusExpander);
                            break;
                        case "ExplorerExpander":
                            _ = HighlightBorderAsync((SettingsCard)element, FileExplorerCustomizationsExpander);
                            break;
                        case "FlyoutsExpander":
                            _ = HighlightBorderAsync((SettingsCard)element, OldFlyoutsExpander);
                            break;
                    }
                }
            }
        }
    }

    private async Task HighlightBorderAsync(SettingsCard control, SettingsExpander? expander = null)
    {
        if (expander != null) 
        { 
            expander.IsExpanded = true;
            await Task.Delay(200); 
        }
        control.StartBringIntoView();
        var originalMargin = control.Margin;
        var originalThickness = control.BorderThickness;
        var originalBrush = control.BorderBrush;
        if (control == ExtractCard || control == AppIconsOnThumbnailsCard || control == OldBatteryFlyoutCard) { control.Margin = new Thickness(0, 2, 0, 0);  }
        control.BorderThickness = new Thickness(1);
        control.BorderBrush = new SolidColorBrush(Colors.White);
        await Task.Delay(5000);
        control.Margin = originalMargin;
        control.BorderBrush = originalBrush;
        control.BorderThickness = originalThickness;
    }

    private static bool FoldersInThisPCLoad()
    {
        var folders = new List<(string, string, string)>
        {
            // 3D Objects
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}" ),
            // Desktop
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}" ),
            // Documents
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{A8CDFF1C-4878-43be-B5FD-F8091C1C60D0}" ),
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{d3162b92-9365-467a-956b-92703aca08af}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{A8CDFF1C-4878-43be-B5FD-F8091C1C60D0}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{d3162b92-9365-467a-956b-92703aca08af}" ),
            // Downloads
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{374DE290-123F-4565-9164-39C4925E467B}" ),
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{088e3905-0323-4b02-9826-5d99428e115f}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{374DE290-123F-4565-9164-39C4925E467B}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{088e3905-0323-4b02-9826-5d99428e115f}" ),
            // Music
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{1CF1260C-4DD0-4ebb-811F-33C572699FDE}" ),
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{1CF1260C-4DD0-4ebb-811F-33C572699FDE}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}" ),
            // Pictures
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{3ADD1653-EB32-4cb0-BBD7-DFA0ABB5ACCA}" ),
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{24ad3ad4-a569-4530-98e1-ab02f9417aa8}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{3ADD1653-EB32-4cb0-BBD7-DFA0ABB5ACCA}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{24ad3ad4-a569-4530-98e1-ab02f9417aa8}" ),
            // Videos
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{A0953C92-50DC-43bf-BE83-3742FED03C9C}" ),
            ( "HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{A0953C92-50DC-43bf-BE83-3742FED03C9C}" ),
            ( "HKLM", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MyComputer\\NameSpace", "{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}" )
        };
        foreach (var folder in folders)
        {
            if (!RegistryHelper.Exists(folder.Item1, folder.Item2, folder.Item3))
            {
                return false;
            }
        }
        return true;
    }

    private bool VisualEffectsLoad()
    {
        var index = 0;
        var AtmosphereEffectsCount = 0;
        var WindowsEffectsCount = 0;

        var Effects = new List<(string, string, string)>
        {
            ("HKCU", $"{UserSID}\\Control Panel\\Desktop", "FontSmoothing"),
            ("HKCU", $"{UserSID}\\Control Panel\\Desktop", "UserPreferencesMask"),
            ("HKCU", $"{UserSID}\\Control Panel\\Desktop", "DragFullWindows"),
            ("HKCU", $"{UserSID}\\Control Panel\\Desktop\\WindowMetrics", "MinAnimate"),
            ("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ListviewAlphaSelect"),
            ("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "IconsOnly"),
            ("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarAnimations"),
            ("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ListviewShadow"),
            ("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects", "VisualFXSetting"),
            ("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\DWM", "EnableAeroPeek"),
            ("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\DWM", "AlwaysHibernateThumbnails")
        };

        var EffectData = new List<(object, object)>
        {
            (2, 2),
            ("9012038010000000", "9E1E078012000000"),
            (1, 1),
            (0, 1),
            (1, 1),
            (0, 0),
            (0, 1),
            (1, 1),
            (3, 0),
            (0, 1),
            (0, 1)
        };

        foreach (var effect in Effects)
        {
            var data = RegistryHelper.Read(effect.Item1, effect.Item2, effect.Item3);
            if (data == EffectData[index].Item1)
            {
                AtmosphereEffectsCount++;
            }
            if (data == EffectData[index].Item2)
            {
                WindowsEffectsCount++;
            }
            index++;
        }
        return WindowsEffectsCount > AtmosphereEffectsCount;
    }

    private void AltTabToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: AltTab Toggled On");
            RegistryHelper.Delete("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "AltTabSettings");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: AltTab Toggled Off");
            RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "AltTabSettings", 1, "REG_DWORD");
        }
    }

    private void ExtractToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu Extract Toggled On");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{b8cdcb65-b1bf-4b42-9428-1dfdb7ee92af}");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{BD472F60-27FA-11cf-B8B4-444553540000}");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{EE07CEF5-3441-4CFB-870A-4002C724783A}");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{D12E3394-DE4B-4777-93E9-DF0AC88F8584}");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu Extract Toggled Off");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{b8cdcb65-b1bf-4b42-9428-1dfdb7ee92af}", "", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{BD472F60-27FA-11cf-B8B4-444553540000}", "", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{EE07CEF5-3441-4CFB-870A-4002C724783A}", "", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{D12E3394-DE4B-4777-93E9-DF0AC88F8584}", "", "REG_SZ");
        }
    }

    private void RunWithPriorityToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu RunWithPriority Toggled On");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority", "MUIVerb", "Run with priority", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority", "SubCommands", "", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\001flyout", "(Default)", "Realtime", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\001flyout\\command", "(Default)", "powershell start -file 'cmd' -args '/c start \"\"\"Realtime App\"\"\" /Realtime \"\"\"%1\"\"\"' -verb runas", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\002flyout", "(Default)", "High", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\002flyout\\command", "(Default)", "cmd /c start \"\" /High \"%1\"", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\003flyout", "(Default)", "Above Normal", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\003flyout\\command", "(Default", "cmd /c start \"\" /AboveNormal \"%1\"", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\004flyout", "(Default)" ,"Normal", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\004flyout\\command", "(Default", "cmd /c start \"\" /Normal \"%1\"", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\005flyout", "(Default)" ,"Below Normal", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\005flyout\\command", "(Default", "cmd /c start \"\" /BelowNormal \"%1\"", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\006flyout", "(Default)" ,"Low", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "exefile\\shell\\Priority\\006flyout\\command", "(Default", "cmd /c start \"\" /Low \"%1\"", "REG_SZ");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu RunWithPriority Toggled Off");
            RegistryHelper.DeleteKey("HKCR", "exefile\\shell\\Priority");
        }
    }

    private void DebloatSendToClick(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow.Content is ShellPage shellPage)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu DebloatSendTo Clicked");
            shellPage.AddBreadcrumb(new Folder { Name = "Debloat Send To", Page = typeof(SubPages.DebloatSendTo) });
            shellPage.RootFrame.Navigate(typeof(SubPages.DebloatSendTo));
        }
    }

    private void TakeOwnershipToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu TakeOwnership Toggled On");
            RegistryHelper.DeleteKey("HKCR", "*\\shell\\Take Ownership");
            RegistryHelper.DeleteKey("HKCR", "*\\shell\\runas");
            RegistryHelper.AddOrUpdate("HKCR", "*\\shell\\Take Ownership", "(Default)", "Take Ownership", "REG_SZ");
            RegistryHelper.Delete("HKCR", "*\\shell\\Take Ownership", "Extended");
            RegistryHelper.AddOrUpdate("HKCR", "*\\shell\\Take Ownership", "HasLUAShield", "", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "*\\shell\\Take Ownership", "NoWorkingDirectory", "", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "*\\shell\\Take Ownership", "NeverDefault", "", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "*\\shell\\Take Ownership\\command", "(Default)", "PowerShell -windowstyle hidden -command \"Start-Process cmd -ArgumentList '/c takeown /f \\\"%1\\\" && icacls \\\"%1\\\" /grant *S-1-3-4:F /t /c /l & pause' -Verb runAs\"", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "*\\shell\\Take Ownership\\command", "IsolatedCommand", "PowerShell -windowstyle hidden -command \"Start-Process cmd -ArgumentList '/c takeown /f \\\"%1\\\" && icacls \\\"%1\\\" /grant *S-1-3-4:F /t /c /l & pause' -Verb runAs\"", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Directory\\shell\\Take Ownership", "(Default)", "Take Ownership", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Directory\\shell\\Take Ownership", "AppliesTo", "NOT (System.ItemPathDisplay:=\"C:\\Users\" OR System.ItemPathDisplay:=\"C:\\ProgramData\" OR System.ItemPathDisplay:=\"C:\\Windows\" OR System.ItemPathDisplay:=\"C:\\Windows\\System32\" OR System.ItemPathDisplay:=\"C:\\Program Files\" OR System.ItemPathDisplay:=\"C:\\Program Files (x86)\")", "REG_SZ");
            RegistryHelper.Delete("HKCR", "Directory\\shell\\Take Ownership", "Extended");
            RegistryHelper.AddOrUpdate("HKCR", "Directory\\shell\\Take Ownership", "HasLUAShield", "", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Directory\\shell\\Take Ownership", "NoWorkingDirectory", "", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Directory\\shell\\Take Ownership", "Position", "middle", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Directory\\shell\\Take Ownership\\command", "(Default)", "PowerShell -windowstyle hidden -command \"$Y = ($null | choice).Substring(1,1); Start-Process cmd -ArgumentList ('/c takeown /f \\\"%1\\\" /r /d ' + $Y + ' && icacls \\\"%1\\\" /grant *S-1-3-4:F /t /c /l /q & pause') -Verb runAs\"", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Directory\\shell\\Take Ownership\\command", "IsolatedCommand", "PowerShell -windowstyle hidden -command \"$Y = ($null | choice).Substring(1,1); Start-Process cmd -ArgumentList ('/c takeown /f \\\"%1\\\" /r /d ' + $Y + ' && icacls \\\"%1\\\" /grant *S-1-3-4:F /t /c /l /q & pause') -Verb runAs\"", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Directory\\shell\\Take Ownership", "(Default)", "Take Ownership", "REG_SZ");
            RegistryHelper.Delete("HKCR", "Drive\\shell\\runas", "Extended");
            RegistryHelper.AddOrUpdate("HKCR", "Drive\\shell\\runas", "HasLUAShield", "", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Drive\\shell\\runas", "NoWorkingDirectory", "", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Drive\\shell\\runas", "Position", "middle", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Drive\\shell\\runas", "AppliesTo", "NOT (System.ItemPathDisplay:=\"C:\\\")", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Drive\\shell\\runas\\command", "(Default)", "cmd.exe /c takeown /f \"%1\\\" /r /d y && icacls \"%1\\\" /grant *S-1-3-4:F /t /c & Pause", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKCR", "Drive\\shell\\runas\\command", "IsolatedCommand", "cmd.exe /c takeown /f \"%1\\\" /r /d y && icacls \"%1\\\" /grant *S-1-3-4:F /t /c & Pause", "REG_SZ");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu TakeOwnership Toggled Off");
            RegistryHelper.DeleteKey("HKCR", "*\\shell\\Take Ownership");
            RegistryHelper.DeleteKey("HKCR", "Directory\\shell\\TakeOwnership");
            RegistryHelper.DeleteKey("HKCR", "*\\shell\\runas");
            RegistryHelper.DeleteKey("HKCR", "Drive\\shell\\runas");
        }
    }

    private async void AddTerminals(object sender, RoutedEventArgs e)
    {
        // I aint rewriting all of that in C#
        if (sender is ToggleSwitch toggle)
        {
            if (toggle.IsOn)
            {
                LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu AddTerminals Toggled On");
                var AddTerminalsReg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts\\Add Terminals.reg");
                await CommandHelper.StartInCmd($"reg import \"{AddTerminalsReg}\"");
            }
            else
            {
                LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu AddTerminals Toggled Off");
                var RemoveTerminals = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts\\Remove Terminals.reg");
                await CommandHelper.StartInCmd($"reg import \"{RemoveTerminals}\"");
            }
        }
        if (sender is Button button)
        {
            switch (button.Tag)
            {
                case "AddTerminalsAll":
                    LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu AddTerminals Toggled On");
                    var AddTerminalsReg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts\\Add Terminals.reg");
                    await CommandHelper.StartInCmd($"reg import \"{AddTerminalsReg}\"");
                    break;
                case "AddTerminalsNoWinTerminal":
                    LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu AddTerminalsNoWinTerminal Toggled On");
                    var AddTerminalsNoWinReg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts\\Add Terminals (no Windows Terminal).reg");
                    await CommandHelper.StartInCmd($"reg import \"{AddTerminalsNoWinReg}\"");
                    break;
                case "AddTerminalsDisable":
                    LogHelper.LogInfo("[InterfaceTweaks]: ContextMenu AddTerminals Toggled Off");
                    var RemoveTerminals = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts\\Remove Terminals.reg");
                    await CommandHelper.StartInCmd($"reg import \"{RemoveTerminals}\"");
                    break;
            }
        }
    }

    private async void ToggleOldContextMenu(object sender, RoutedEventArgs e)
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
        var logoff = await ShowConfirmationDialogAsync("Dialog_Title_LogOff", "Dialog_Content_LogOff");
        if (logoff) { await CommandHelper.StartInCmd("logoff"); }
    }

    private void EdgeSwipeToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: EdgeSwipe Toggled On");
            RegistryHelper.Delete("HKLM", "Software\\Policies\\Microsoft\\Windows\\EdgeUI", "AllowEdgeSwipe");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: EdgeSwipe Toggled Off");
            RegistryHelper.AddOrUpdate("HKLM", "Software\\Policies\\Microsoft\\Windows\\EdgeUI", "AllowEdgeSwipe", 0, "REG_DWORD");
        }
    }

    private void AppIconsOnThumbnailsToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations AppIconsOnThumbnail Toggled On");
            RegistryHelper.Delete("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTypeOverlay");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations AppIconsOnThumbnail Toggled Off");
            RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTypeOverlay", 0, "REG_DWORD");
        }
    }

    private void AutomaticFolderDiscoveryToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations AutomaticFolderDiscover Toggled On");
            RegistryHelper.Delete("HKCU", $"{UserSID}\\Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\Bags\\AllFolders\\Shell", "FolderType");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations AutomaticFolderDiscover Toggled Off");
            RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\Bags\\AllFolders\\Shell", "FolderType", 0, "REG_DWORD");
        }
    }

    private void CompactViewToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations CompactView Toggled On");
            RegistryHelper.Delete("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "UseCompactMode");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations CompactView Toggled Off");
            RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "UseCompactMode", 0, "REG_DWORD");
        }
    }

    private async void FoldersInThisPCToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations FoldersInThisPC Toggled On");
            var RestoreFolders = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts\\Restore all folders in This PC.reg");
            await CommandHelper.StartInCmd($"reg import \"{RestoreFolders}\"");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations FoldersInThisPC Toggled Off");
            var RemoveFolders = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts\\Remove all folders in This PC.reg");
            await CommandHelper.StartInCmd($"reg import \"{RemoveFolders}\"");
        }
    }

    private void GalleryToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations Gallery Toggled On");
            RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Software\\Classes\\CLSID\\{{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}}", "System.IsPinnedToNameSpaceTree", 0, "REG_DWORD");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations Gallery Toggled Off");
            RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Software\\Classes\\CLSID\\{{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}}", "System.IsPinnedToNameSpaceTree", 1, "REG_DWORD");
        }
    }

    private void QuickAccessToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations QuickAccess Toggled On");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "HubMode");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations QuickAccess Toggled Off");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "HubMode", 1, "REG_DWORD");
        }
    }

    private void RemovableDrivesInSidebarToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations RemovableDrivesInSidebar Toggled On");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace\\DelegateFolders\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}", "(Default)", "Removable Drives", "REG_SZ");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace\\DelegateFolders\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}", "(Default)", "Removable Drives", "REG_SZ");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: FileExplorerCustomizations RemovableDrivesInSidebar Toggled Off");
            RegistryHelper.DeleteKey("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace\\DelegateFolders\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}");
            RegistryHelper.DeleteKey("HKLM", "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Desktop\\NameSpace\\DelegateFolders\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}");
        }
    }

    private void OldBatteryFlyoutToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: OldFlyouts OldBatteryFlyout Toggled On");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ImmersiveShell", "UseWin32BatteryFlyout", 1, "REG_DWORD");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: OldFlyouts OldBatteryFlyout Toggled Off");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ImmersiveShell", "UseWin32BatteryFlyout", 0, "REG_DWORD");
        }
    }

    private void DateAndTimeFlyoutToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: OldFlyouts OldDateAndTimeFlyout Toggled On");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ImmersiveShell", "UseWin32TrayClockExperience", 1, "REG_DWORD");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: OldFlyouts OldDateAndTimeFlyout Toggled Off");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ImmersiveShell", "UseWin32TrayClockExperience", 0, "REG_DWORD");
        }
    }

    private void OldVolumeFlyoutToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: OldFlyouts OldVolumeFlyout Toggled On");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\MTCUVC", "EnableMtcUvc", 0, "REG_DWORD");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: OldFlyouts OldVolumeFlyout Toggled Off");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\MTCUVC", "EnableMtcUvc");
        }
    }
    
    private void ShortcutIcon(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle)
        {
            if (toggle.IsOn)
            {
                LogHelper.LogInfo("[InterfaceTweaks]: ShortcutIcon Toggled On");
                RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Icons", "29");
            }
            else
            {
                RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Icons", "29", "C:\\Windows\\AtmosphereModules\\Other\\Blank.ico,0", "REG_SZ");
                LogHelper.LogInfo("[InterfaceTweaks]: ShortcutIcon Toggled Off");
            }
        }
        if (sender is Button button)
        {
            ShortcutIconToggle.Toggled -= ShortcutIcon;
            switch (button.Tag)
            {
                case "ShortcutIconDefault":
                    LogHelper.LogInfo("[InterfaceTweaks]: ShortcutIcon Toggled Default");
                    RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Icons", "29");
                    ShortcutIconToggle.IsOn = true;
                    break;
                case "ShortcutIconClassic":
                    LogHelper.LogInfo("[InterfaceTweaks]: ShortcutIcon Toggled Classic");
                    RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Icons", "29", "C:\\Windows\\AtmosphereModules\\Other\\Classic.ico,0", "REG_SZ");
                    ShortcutIconToggle.IsOn = true;
                    break;
                case "ShortcutIconDisable":
                    LogHelper.LogInfo("[InterfaceTweaks]: ShortcutIcon Disabled");
                    RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Icons", "29", "C:\\Windows\\AtmosphereModules\\Other\\Blank.ico,0", "REG_SZ");
                    ShortcutIconToggle.IsOn = false;
                    break;
            }
            ShortcutIconToggle.Toggled += ShortcutIcon;
        }
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
    }

    private void ShortcutNameToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: ShortcutName Toggled On");
            RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates", "ShortcutNameTemplate", "\"%s.lnk\"", "REG_SZ");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: ShortcutName Toggled Off");
            RegistryHelper.Delete("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\NamingTemplates", "ShortcutNameTemplate");
        }
    }

    private void SnapLayoutsToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: SnapLayouts Toggled On");
            RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "EnableSnapAssistFlyout", 1, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "EnableSnapBar", 1, "REG_DWORD");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: SnapLayouts Toggled Off");
            RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "EnableSnapAssistFlyout", 0, "REG_DWORD");
            RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "EnableSnapBar", 0, "REG_DWORD");
        }
    }

    private void VerboseStatusMessageToggled(object sender, RoutedEventArgs e)
    {
        var toggle = (ToggleSwitch)sender;
        if (toggle.IsOn)
        {
            LogHelper.LogInfo("[InterfaceTweaks]: VerboseStatusMessage Toggled On");
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "verbosestatus", 1, "REG_DWORD");
        }
        else
        {
            LogHelper.LogInfo("[InterfaceTweaks]: VerboseStatusMessage Toggled Off");
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "verbosestatus");
        }
    }

    private async void VisualEffects(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle)
        {
            if (toggle.IsOn)
            {
                LogHelper.LogInfo("[InterfaceTweaks]: VisualEffects Toggled On");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Control Panel\\Desktop", "FontSmoothing", 2, "REG_SZ");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Control Panel\\Desktop", "UserPreferencesMask", "REG_BINARY", "9E1E078012000000");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Control Panel\\Desktop", "DragFullWindows", 1, "REG_SZ");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Control Panel\\Desktop\\WindowMetrics", "MinAnimate", 1, "REG_SZ");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ListviewAlphaSelect", 1, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "IconsOnly", 0, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarAnimations", 1, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ListviewShadow", 1, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects", "VisualFXSetting", 0, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\DWM", "EnableAeroPeek", 1, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\DWM", "AlwaysHibernateThumbnails", 1, "REG_DWORD");
            }
            else
            {
                LogHelper.LogInfo("[InterfaceTweaks]: VisualEffects Toggled Off");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Control Panel\\Desktop", "FontSmoothing", 2, "REG_SZ");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Control Panel\\Desktop", "UserPreferencesMask", "REG_BINARY", "9012038010000000");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Control Panel\\Desktop", "DragFullWindows", 1, "REG_SZ");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\Control Panel\\Desktop\\WindowMetrics", "MinAnimate", 0, "REG_SZ");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ListviewAlphaSelect", 1, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "IconsOnly", 0, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarAnimations", 0, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ListviewShadow", 1, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects", "VisualFXSetting", 3, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\DWM", "EnableAeroPeek", 0, "REG_DWORD");
                RegistryHelper.AddOrUpdate("HKCU", $"{UserSID}\\SOFTWARE\\Microsoft\\Windows\\DWM", "AlwaysHibernateThumbnails", 0, "REG_DWORD");
            }
            if (await ShowConfirmationDialogAsync("Dialog_Title_LogOff", "Dialog_Content_LogOff"))
            {
                await CommandHelper.StartInCmd("logoff");
            }
        }
        if (sender is Button)
        {
                LogHelper.LogInfo("[InterfaceTweaks]: VisualEffects Showing Config");
            _ = CommandHelper.RunProcess("C:\\Windows\\System32\\SystemPropertiesPerformance.exe", wait: false, hidden: false);
        }
    }

    private void MoreOptionsClick(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("[InterfaceTweaks]: MoreOptions Clicked");
        if (App.MainWindow.Content is ShellPage shellPage)
        {
            shellPage.RootFrame.Navigate(typeof(AtmosphereSettingsPage));
        }
    }

    private async Task<bool> ShowConfirmationDialogAsync(string titleKey, string contentKey, string primaryKey = "Dialog_PrimaryText", string closeKey = "Dialog_CloseText")
    {
        var title = GetLocalizedString(titleKey);
        var content = GetLocalizedString(contentKey);
        var primaryText = GetLocalizedString(primaryKey);
        var closeText = GetLocalizedString(closeKey);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}

