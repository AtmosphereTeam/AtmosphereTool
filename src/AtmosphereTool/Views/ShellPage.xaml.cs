using AtmosphereTool.Contracts.Services;
using AtmosphereTool.Helpers;
using AtmosphereTool.ViewModels;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.System;
using WinResLoader = Windows.ApplicationModel.Resources.ResourceLoader;

namespace AtmosphereTool.Views;

public sealed partial class ShellPage : Page
{
    public Frame RootFrame => NavigationFrame;
    private static readonly ResourceLoader _resourceLoader = new();
    public ShellViewModel ViewModel
    {
        get;
    }

    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);

        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;
        AppTitleBarText.Text = _resourceLoader.GetString("AppDisplayName");
        Shell_AtmosphereSettings.Visibility = RegistryHelper.Read("HKLM", "SOFTWARE\\AME\\Playbooks\\Applied\\{8BBB362C-858B-41D9-A9EA-83A4B9669C43}", "Version") != null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);

        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        App.AppTitlebar = AppTitleBarText as UIElement;
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }

    private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        var keyboardAccelerator = new KeyboardAccelerator() { Key = key };

        if (modifiers.HasValue)
        {
            keyboardAccelerator.Modifiers = modifiers.Value;
        }

        keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

        return keyboardAccelerator;
    }

    private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var navigationService = App.GetService<INavigationService>();

        var result = navigationService.GoBack();

        args.Handled = result;
    }

    // Search Box
    private readonly WinResLoader searchBoxResourceLoader = WinResLoader.GetForViewIndependentUse("SettingsItems");

    public string GetLocalizedString(string key)
    {
        var localized = searchBoxResourceLoader.GetString(key);
        return string.IsNullOrEmpty(localized) ? key : localized;
    }

    private CancellationTokenSource? _cts;

    private readonly List<SettingItem> AllSettings = new()
    {
        // WindowsSettingsPage
        new() { DisplayName = "Change Username", Key = "ChangeUser", Page = typeof(WindowsSettingsPage) },
        new() { DisplayName = "Change User Password", Key = "ChangeUserPassword", Page = typeof(WindowsSettingsPage) },
        new() { DisplayName = "Change User PFP", Key = "ChangeUserPFP", Page = typeof(WindowsSettingsPage) },
        new() { DisplayName = "Create New User", Key = "CreateNewUser", Page = typeof(WindowsSettingsPage) },
        new() { DisplayName = "Enhanced Security", Key = "EnhancedSecurity", Page = typeof(WindowsSettingsPage) },
        new() { DisplayName = "Hide User Names", Key = "HideUserNames", Page = typeof(WindowsSettingsPage) },
        new() { DisplayName = "Hibernation", Key = "Hibernation", Page = typeof(WindowsSettingsPage) },
        new() { DisplayName = "Visual Basic Script", Key = "VBS", Page = typeof(WindowsSettingsPage) },
        new() { DisplayName = "Notification Center", Key = "NotificationCenter", Page = typeof(WindowsSettingsPage) },
        // GeneralConfig
        new() { DisplayName = "Copilot", Key = "Copilot", Page = typeof(FeaturePages.GeneralConfig) },
        new() { DisplayName = "Recall", Key = "Recall", Page = typeof(FeaturePages.GeneralConfig) },
        new() { DisplayName = "Background Apps", Key = "BackgroundApps", Page = typeof(FeaturePages.GeneralConfig) },
        new() { DisplayName = "Delivery Optimizations", Key = "DeliveryOptimizations", Page = typeof(FeaturePages.GeneralConfig) },
        new() { DisplayName = "FSO And GameBar", Key = "FSOAndGameBar", Page = typeof(FeaturePages.GeneralConfig) },
        new() { DisplayName = "Phone Link", Key = "PhoneLink", Page = typeof(FeaturePages.GeneralConfig) },
        new() { DisplayName = "Search Indexing", Key = "SearchIndex", Page = typeof(FeaturePages.GeneralConfig) },
        new() { DisplayName = "Store App Archiving", Key = "StoreAppArchiving", Page = typeof(FeaturePages.GeneralConfig) },
        new() { DisplayName = "System Restore", Key = "SystemRestore", Page = typeof(FeaturePages.GeneralConfig) },
        new() { DisplayName = "Update Notifications", Key = "UpdateNotifications", Page = typeof(FeaturePages.GeneralConfig) },
    };

    private List<SettingItem> GetAtmosphereSettings()
    {
        var options = RegistryHelper.Read("HKLM", "SOFTWARE\\AME\\Playbooks\\Applied\\{8bbb362c-858b-41d9-a9ea-83a4b9669c43}", "SelectedOptions") as string[] ?? [];
        if (options.Length == 0) { return []; } // is user running atmosphere
        var settings = new List<SettingItem>();
        settings.Add(new() { DisplayName = "Mitigations", Key = "Mitigations", Page = typeof(AtmosphereSettingsPage) });
        settings.Add(new() { DisplayName = "Power Saving", Key = "PowerSaving", Page = typeof(AtmosphereSettingsPage) });
        settings.Add(new() { DisplayName = "Start Menu", Key = "WinStartMenu", Page = typeof(AtmosphereSettingsPage) });
        if (Environment.OSVersion.Version.Build > 22000) { settings.Add(new() { DisplayName = "Old Context Menu", Key = "OldContextMenu", Page = typeof(AtmosphereSettingsPage) }); }
        settings.Add(new() { DisplayName = "Translucent Flyouts", Key = "TranslucentFlyouts", Page = typeof(AtmosphereSettingsPage) });
        settings.Add(new() { DisplayName = "Mica Explorer", Key = "MicaExplorer", Page = typeof(AtmosphereSettingsPage) });
        if (options.Contains("ameliorate")) { return settings; }
        settings.Add(new() { DisplayName = "Defender", Key = "Defender", Page = typeof(AtmosphereSettingsPage) });
        settings.Add(new() { DisplayName = "Windows Updates", Key = "Updates", Page = typeof(AtmosphereSettingsPage) });
        settings.Add(new() { DisplayName = "Configure Services", Key = "ConfigServices", Page = typeof(AtmosphereSettingsPage) });
        settings.Add(new() { DisplayName = "Configure Telemetry", Key = "ConfigTelemetry", Page = typeof(AtmosphereSettingsPage) });
        settings.Add(new() { DisplayName = "Repair Windows", Key = "RepairWindows", Page = typeof(AtmosphereSettingsPage) });
        return settings;
    }

    private List<SettingItem>? _cachedAvailableSettings;

    private List<SettingItem> GetAllAvailableSettings(bool forceRefresh = false)
    {
        if (_cachedAvailableSettings != null && !forceRefresh) { return _cachedAvailableSettings; }
        var combined = new List<SettingItem>();
        combined.AddRange(AllSettings);
        combined.AddRange(GetAtmosphereSettings());
        foreach (var item in combined)
        {
            item.DisplayName = searchBoxResourceLoader.GetString(item.Key);
        }
        _cachedAvailableSettings = combined;
        return combined;
    }


    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        try
        {
            // debounce so you only search 300ms after user stops typing
            await Task.Delay(300, token);
            var text = sender.Text;
            var splitText = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var suitableItems = new List<string>(10);
            // show all settings if nothing searched
            if (string.IsNullOrWhiteSpace(text))
            {
                foreach (var setting in GetAllAvailableSettings())
                {
                    suitableItems.Add(setting.DisplayName);
                }
                if (!token.IsCancellationRequested)
                {
                    sender.ItemsSource = suitableItems;
                }
                return;
            }
            // if searched
            foreach (var setting in GetAllAvailableSettings())
            {
                bool found = true;
                foreach (var key in splitText)
                {
                    if (!setting.DisplayName.Contains(key, StringComparison.CurrentCultureIgnoreCase))
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    suitableItems.Add(setting.DisplayName);
                }
            }
            if (!token.IsCancellationRequested)
            {
                sender.ItemsSource = suitableItems;
            }
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }
    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion == null)
        {
            var match = AllSettings.FirstOrDefault(s =>
                s.DisplayName.Equals(args.QueryText, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                if (App.MainWindow.Content is ShellPage shellPage)
                {
                    shellPage.RootFrame.Navigate(match.Page, match.Key);
                }
            }
        }
    }

    private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var selected = GetAllAvailableSettings().FirstOrDefault(s => s.DisplayName == args.SelectedItem.ToString());
        if (selected != null)
        {
            if (App.MainWindow.Content is ShellPage shellPage)
            {
                shellPage.RootFrame.Navigate(selected.Page, selected.Key);
            }
        }
    }
}
public class SettingItem
{
    public required string DisplayName { get; set; } = string.Empty;
    public required string Key { get; set; }
    public required Type Page { get; set; }
}
