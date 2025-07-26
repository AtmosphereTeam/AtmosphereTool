using AtmosphereTool.Helpers;
using AtmosphereTool.ViewModels;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.Storage;

namespace AtmosphereTool.Views;
public sealed partial class WindowsSettingsPage : Page
{
    public WindowsSettingsViewModel ViewModel
    {
        get;
    }

    private readonly WindowsSettingsLocalizationHelper _localizationHelper = new();

    public WindowsSettingsPage()
    {
        ViewModel = App.GetService<WindowsSettingsViewModel>();
        InitializeComponent();
        LoadSettings();
        LocalizeControls();
    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter == null || e.Parameter.ToString() == string.Empty) { return; }
        if (e.Parameter is string target)
        {
            var elementMap = new Dictionary<string, FrameworkElement>
            {
                { "ChangeUser", ChangeUserHeader },
                { "ChangeUserPassword", ChangeUPassword },
                { "ChangeUserPFP", CUPFP },
                { "CreateNewUser", CreateNewHeader },
                // System Settings
                { "EnhancedSecurity", EnhancedSec },
                { "HideUserNames", HideUser },
                { "Hibernation", Hibernation },
                { "VBS", VBSHeader },
                { "NotificationCenter", NotiCenter },
                { "Notification", Notification },
            };

            if (elementMap.TryGetValue(target, out var element))
            {
                element.StartBringIntoView();
                if (element is SettingsCard card)
                {
                    if (element == ChangeUserHeader ||
                        element == ChangeUPassword ||
                        element == CUPFP ||
                        element == CreateNewHeader)
                    { _ = HighlightBorderAsync(card, settingsCard); }
                    else { _ = HighlightBorderAsync(card, SystemExpander); }
                }
            }
        }
    }
    private async Task HighlightBorderAsync(SettingsCard card, SettingsExpander expander)
    {

        await Task.Delay(200);
        expander.IsExpanded = true;
        var originalExpanderThickness = expander.BorderThickness;
        expander.BorderThickness = new Thickness(1);
        await Task.Delay(300);
        if (card == ChangeUserHeader || card == EnhancedSec) { card.Margin = new Thickness(0,2,0,0); }
        card.StartBringIntoView();
        var originalThickness = card.BorderThickness;
        var originalBrush = card.BorderBrush;
        card.BorderThickness = new Thickness(1);
        card.BorderBrush = new SolidColorBrush(Colors.White);
        await Task.Delay(5000);
        expander.BorderThickness = originalExpanderThickness;
        if (card == ChangeUserHeader || card == EnhancedSec) { card.Margin = new Thickness(0,0,0,0); }
        card.BorderThickness = originalThickness;
        card.BorderBrush = originalBrush;
    }
    private void LoadSettings()
    {
        LogHelper.LogInfo("[WindowsSettingsPage] Loading Controls Settings");
        // Unsubscribe
        HideUsername.Toggled -= HideUsernameToggle;
        ToggleHibernation.Toggled -= HibernationToggle;
        VisualBasicScript.Toggled -= VisualBasicScriptToggle;
        NotificationCenter.Toggled -= NotificationCenterToggle;
        Notifications.Toggled -= NotificationsToggle;
        VerifyAtmosphere.Click -= VerifyAtmosphereClick;
        // Load Settings
        var value = RegistryHelper.Read("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "dontdisplaylastusername");
        HideUsername.IsOn = value != null && (int)value == 1;
        ToggleHibernation.IsOn = PowerHelper.IsHibernationEnabled();
        VisualBasicScript.IsOn = RegistryHelper.Read("HKCR", ".vbs", "")?.ToString() == "VBSFile";
        var sid = RegistryHelper.GetCurrentUserSid();
        var notificationCenterValue = RegistryHelper.Read("HKU", $"{sid}\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "DisableNotificationCenter");
        NotificationCenter.IsOn = notificationCenterValue == null || notificationCenterValue.ToString() != "1";
        var notificationsValue = RegistryHelper.Read("HKU", $"{sid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", "ToastEnabled");
        Notifications.IsOn = notificationsValue == null || notificationsValue.ToString() == "1";
        // VerifyAtmos.Visibility = RegistryHelper.Read("HKLM", "SOFTWARE\\AME\\Playbooks\\Applied\\{8bbb362c-858b-41d9-a9ea-83a4b9669c43}", "SelectedOptions") is string[] arr && arr.Contains("ameliorated") ? Visibility.Visible : Visibility.Collapsed;
        // Resubscribe
        HideUsername.Toggled += HideUsernameToggle;
        ToggleHibernation.Toggled -= HibernationToggle;
        VisualBasicScript.Toggled += VisualBasicScriptToggle;
        NotificationCenter.Toggled += NotificationCenterToggle;
        Notifications.Toggled += NotificationsToggle;
        // VerifyAtmosphere.Click += VerifyAtmosphereClick;
    }
    private void LocalizeControls()
    {
        LogHelper.LogInfo("[WindowsSettingsPage] Localizing Controls");
        TopTitle.Text = _localizationHelper.GetLocalizedString("TopTitle");
        settingsCard.Header = _localizationHelper.GetLocalizedString("settingsCardHeader");
        settingsCard.Description = _localizationHelper.GetLocalizedString("settingsCardDescription");
        ChangeUserHeader.Header = _localizationHelper.GetLocalizedString("ChangeUserHeader");
        ChangeUser.Content = _localizationHelper.GetLocalizedString("ChangeUser");
        ChangeUPassword.Header = _localizationHelper.GetLocalizedString("ChangeUPassword");
        ChangePassword.Content = _localizationHelper.GetLocalizedString("ChangePassword");
        PasswordIBlock.Text = _localizationHelper.GetLocalizedString("PasswordIBlock");
        ChangeOtherPass.Content = _localizationHelper.GetLocalizedString("ChangeOtherPass");
        CUPFP.Header = _localizationHelper.GetLocalizedString("CUPFP");
        ChangePfp.Content = _localizationHelper.GetLocalizedString("ChangePfp");
        PfpInfoText.Text = _localizationHelper.GetLocalizedString("PfpInfoText");
        ExplorerButton.Content = _localizationHelper.GetLocalizedString("ExplorerButton");
        CreateNewHeader.Header = _localizationHelper.GetLocalizedString("CreateNewHeader");
        CreateUser.Content = _localizationHelper.GetLocalizedString("CreateUser");
        SystemExpander.Header = _localizationHelper.GetLocalizedString("SystemExpanderHeader");
        SystemExpander.Description = _localizationHelper.GetLocalizedString("SystemExpanderDescription");
        EnhancedSec.Header = _localizationHelper.GetLocalizedString("EnhancedSec");
        EnableEnchancedSec.Content = _localizationHelper.GetLocalizedString("EnableEnchancedSec");
        DisableEnchancedSec.Content = _localizationHelper.GetLocalizedString("DisableEnchancedSec");
        SecIText.Text = _localizationHelper.GetLocalizedString("SecIText");
        HideUser.Header = _localizationHelper.GetLocalizedString("HideUser");
        Hibernation.Header = _localizationHelper.GetLocalizedString("Hibernation");
        VBSHeader.Header = _localizationHelper.GetLocalizedString("VBSHeader");
        NotiCenter.Header = _localizationHelper.GetLocalizedString("NotiCenter");
        Notification.Header = _localizationHelper.GetLocalizedString("Notification");
        VerifyAtmos.Header = _localizationHelper.GetLocalizedString("VerifyAtmos");
        VerifyAtmosphere.Content = _localizationHelper.GetLocalizedString("VerifyAtmosphere");
    }
    private async void HibernationToggle(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Toggling Hibernation");
        var toggle = sender as ToggleSwitch;
        if (toggle == null) { return; }
        if (toggle.IsOn)
        {
            await CommandHelper.StartInCmd("powercfg /hibernate on");
        }
        else
        {
            await CommandHelper.StartInCmd("powercfg /hibernate off");
        }
    }
    private void VisualBasicScriptToggle(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Toggling VBS");
        var toggle = sender as ToggleSwitch;
        if (toggle == null) { return; }
        if (toggle.IsOn)
        {
            RegistryHelper.AddOrUpdate("HKCR", ".vbs", "", "VBSFile", "REG_SZ");
        }
        else
        {
            RegistryHelper.AddOrUpdate("HKCR", ".vbs", "", "", "REG_SZ");
        }
    }
    private void HideUsernameToggle(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Toggling username visablity during logon");
        var toggle = sender as ToggleSwitch;
        if (toggle == null) { return; }
        if (toggle.IsOn)
        {
            RegistryHelper.AddOrUpdate("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "dontdisplaylastusername", "1", "REG_DWORD");
        }
        else
        {
            RegistryHelper.Delete("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "dontdisplaylastusername");
        }
    }
    private void ChangeOtherPasswords(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Change other passwords clicked");
        Process.Start("netplwiz");
    }
    private void NotificationCenterToggle(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Toggling Notification Center");
        var toggle = sender as ToggleSwitch;
        if (toggle == null) { return; }
        var sid = RegistryHelper.GetCurrentUserSid();
        if (toggle.IsOn)
        {
            RegistryHelper.Delete("HKU", $"{sid}\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "DisableNotificationCenter");
        }
        else
        {
            RegistryHelper.AddOrUpdate("HKU", $"{sid}\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "DisableNotificationCenter", "1", "REG_DWORD");
        }
    }
    private void NotificationsToggle(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Toggling notifications");
        var toggle = sender as ToggleSwitch;
        if (toggle == null) { return; }
        var sid = RegistryHelper.GetCurrentUserSid();
        if (toggle.IsOn)
        {
            RegistryHelper.AddOrUpdate("HKU", $"{sid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", "ToastEnabled", "1", "REG_DWORD");
        }
        else
        {
            RegistryHelper.AddOrUpdate("HKU", $"{sid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", "ToastEnabled", "0", "REG_DWORD");
        }
    }
    private async void DisableEnchancedSecClick(object sender, RoutedEventArgs e)
    {
        // the names here suck
        LogHelper.LogInfo("Disable Enhanced Security button clicked");
        var disenchsecstart = await ShowConfirmationDialogAsync("DisEnhancedSecConfirm_Title", "DisEnhancedSecConfirm_Content", "DisEnhancedSecConfirm_Primary");
        if (!disenchsecstart)
        {
            return;
        }
        var (Success, Error) = await SecurityHelper.ElevateAsync();
        {
            if (Success)
            {
                LogHelper.LogInfo("Disabled Enhanced Security");
                var disenchseclog = await ShowConfirmationDialogAsync("DisEnhancedSecDone_Title", "Dialog_Content_LogOff", "Dialog_PrimaryText_LogOff");
                if (disenchseclog)
                {
                    NSudo.RunAsUser(() =>
                    {
                        Process.Start("cmd.exe /c logoff");
                    });
                    // CommandHelper.StartInCmd("logoff");
                }
            }
            else
            {
                ShowErrorDialog("Error Disabling Enhanced Security", Error ?? "An unknown error occurred.");
                LogHelper.LogError("Error Disabling Enhanced Security. " + Error ?? "An unknown error occurred.");
            }
        }
    }
    private async void EnableEnchancedSecClick(object sender, RoutedEventArgs e)
    {
        var enensecstart = await ShowConfirmationDialogAsync("EnEnhancedSecConfirm_Title", "EnEnhancedSecConfirm_Content", "EnEnhancedSecConfirm_Primary");
        if (!enensecstart)
        {
            return;
        }
        var (Success, Error) = await SecurityHelper.DeElevateAsync();

        if (Success)
        {
            var enenchseclog = await ShowConfirmationDialogAsync("EnEnhancedSecDone_Title", "Dialog_Content_LogOff", "Dialog_PrimaryText_LogOff");
            if (enenchseclog)
            {
                NSudo.RunAsUser(() =>
                {
                    Process.Start("cmd.exe /c logoff");
                });
                // CommandHelper.StartInCmd("logoff");
            }
        }
        else
        {
            // No point in localizing errors if the error they spit out is in english
            ShowErrorDialog("Error Enabling Enhanced Security", Error ?? "An unknown error occurred.");
            LogHelper.LogError("Error Enabling Enhanced Security. " + Error ?? "An unknown error occurred.");
        }
    }
    private void ExplorerClick(object sender, RoutedEventArgs e)
    {
        try
        {
            LogHelper.LogInfo("Launching explorer");
            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Failed to open Explorer: {ex.Message}");
            ShowErrorDialog("Error Opening Explorer", ex.Message);
        }
    }
    private void ChangePasswordClick(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Change Password button clicked");
        ShowInfoDialog("ChangePassword", "ChangePasswordInstructions");
    }
    private async void CreateUserClick(object sender, RoutedEventArgs e)
    {
        LogHelper.LogInfo("Create New User button pressed");
        var cnuconfirm = await ShowConfirmationDialogAsync("CreateNewHeader", "CreateNewUserInstructions", "CreateUser");
        if (cnuconfirm)
        {
            await CommandHelper.StartInCmd("netplwiz");
        }
    }
    private static async Task<bool> DoesUserExist(string username)
    {
        return (await CommandHelper.StartInPowershell($"net user \"{username}\"")).Success;
    }
    private async void ChangeUsernameClick(object sender, RoutedEventArgs e)
    {
        var usernameResult = await ShowUsernameChangeDialogAsync();
        if (usernameResult is not null)
        {
            var (oldUsername, newUsername) = usernameResult.Value;
            var pattern = @"^\w[\w.\- ]{0,19}$";

            var isOldValid = !string.IsNullOrWhiteSpace(oldUsername) && Regex.IsMatch(oldUsername, pattern);
            var isNewValid = !string.IsNullOrWhiteSpace(newUsername) && Regex.IsMatch(newUsername, pattern);

            if (isOldValid && isNewValid)
            {
                LogHelper.LogInfo($"Old: {oldUsername}, New: {newUsername}");
                // Shut up the compiler
                if (string.IsNullOrWhiteSpace(oldUsername) || string.IsNullOrWhiteSpace(newUsername))
                {
                    ShowInfoDialog("ChangeUsernInvalidIn", "ChangeUsernInvalidInEx");
                    LogHelper.LogError("[CHANGE USERNAME]: Empty input");
                    return;
                }
                if (!await DoesUserExist(oldUsername))
                {
                    var formatString = _localizationHelper.GetLocalizedString("ChangeUserNoUserEx");
                    var errorMessage = string.Format(formatString, oldUsername);
                    var title = _localizationHelper.GetLocalizedString("ChangeUserNoUser");
                    ShowErrorDialog(title, errorMessage);
                    LogHelper.LogWarning("[CHANGE USERNAME]: No user found to change name.");
                    return;
                }
                if (string.Equals(oldUsername, newUsername, StringComparison.OrdinalIgnoreCase))
                {
                    ShowInfoDialog("ChangeUsernInvalidOp", "ChangeUsernInvalidOpEx");
                    LogHelper.LogWarning("[CHANGE USERNAME]: Same username");
                    return;
                }
                if (await DoesUserExist(newUsername))
                {
                    var formatString = _localizationHelper.GetLocalizedString("ChangeUserDuplicateEx");
                    var errorMessage = string.Format(formatString, oldUsername);
                    var title = _localizationHelper.GetLocalizedString("ChangeUserDuplicate");
                    ShowErrorDialog(title, errorMessage);
                    LogHelper.LogWarning("[CHANGE USERNAME]: New username is in use");
                    return;
                }
                var command = $"Rename-LocalUser -Name \"{oldUsername}\" -NewName \"{newUsername}\"";
                var (Success, Error) = await CommandHelper.StartInPowershell(command);
                if (Success)
                {
                    LogHelper.LogInfo("[CHANGE USERNAME]: Completed");
                    var result = await ShowConfirmationDialogAsync("ChangeUserDone", "Dialog_Content_LogOff", "Dialog_PrimaryText_LogOff");
                    if (result)
                    {
                        await CommandHelper.StartInCmd("logoff");
                    }
                }
                else
                {
                    ShowErrorDialog("Error Changing Username", Error ?? "An unknown error occurred while changing the username.");
                    LogHelper.LogWarning("[CHANGE USERNAME]:Error: " + Error ?? "An unknown error occurred while changing the username.");
                }
            }
            else
            {
                ShowInfoDialog("ChangeUserBadName", "ChangeUserBadNameEx");
                LogHelper.LogInfo("[CHANGE USERNAME]: Bad username");
            }
        }
    }
    private async void OnChangeImageButtonClick(object sender, RoutedEventArgs e)
    {
        var userInput = await ShowInputDialogAsync("PfpChangeImg", "PfpChangeImgPlace");
        if (!string.IsNullOrEmpty(userInput))
        {
            userInput = userInput.Trim().Trim('"').Trim('\'');
            LogHelper.LogInfo($"User entered path: {userInput}");
            if (File.Exists(userInput))
            {
                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(userInput);
                    if (await ProfileImageHelper.ChangeImageAsync(file))
                    {
                        LogHelper.LogInfo("Changed pfp");
                        var result = await ShowConfirmationDialogAsync("PfpChangeDone", "Dialog_Content_LogOff", "Dialog_PrimaryText_LogOff");
                        if (result)
                        {
                            await CommandHelper.StartInCmd("logoff");
                        }
                    }
                    else
                    {
                        ShowErrorDialog("Error Changing Profile Image", "Failed to change profile image.");
                        LogHelper.LogError("Unknown error line 364 WindowsSettingsPage.xaml.cs");
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Error Changing Profile Image", ex.Message);
                    LogHelper.LogError($"[PFP CHANGER] LINE 371 catch Error: {ex.Message}");
                }
            }
            else
            {
                ShowInfoDialog("FileNotExist", "FileNotExistEx");
                LogHelper.LogWarning("[PFP CHANGER]: File does not exist.");
            }
        }
        else
        {
            LogHelper.LogInfo("User cancelled or entered nothing");
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
    private async Task<string?> ShowInputDialogAsync(string titleKey, string placeholderKey)
    {
        var title = _localizationHelper.GetLocalizedString(titleKey);
        var placeholder = _localizationHelper.GetLocalizedString(placeholderKey);
        var primaryText = _localizationHelper.GetLocalizedString("Dialog_PrimaryText");
        var closeText = _localizationHelper.GetLocalizedString("Dialog_CloseText");

        var inputTextBox = new TextBox
        {
            PlaceholderText = placeholder,
            MinWidth = 300
        };
        var dialog = new ContentDialog
        {
            Title = title,
            Content = inputTextBox,
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot // Important to set the XamlRoot for the dialog to display properly
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            return inputTextBox.Text;
        }
        else
        {
            return null; // User cancelled
        }
    }
    private async Task<(string? OldUsername, string? NewUsername)?> ShowUsernameChangeDialogAsync()
    {
        var oldUsernameBox = new TextBox
        {
            PlaceholderText = "Enter current username",
            Margin = new Thickness(0, 0, 0, 12),
            MinWidth = 300
        };
        var newUsernameBox = new TextBox
        {
            PlaceholderText = "Enter new username",
            MinWidth = 300
        };
        var dialogStack = new StackPanel();
        dialogStack.Children.Add(oldUsernameBox);
        dialogStack.Children.Add(newUsernameBox);
        var dialog = new ContentDialog
        {
            Title = "Change Username",
            Content = dialogStack,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            return (oldUsernameBox.Text, newUsernameBox.Text);
        }
        return null;
    }
    private void VerifyAtmosphereClick(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow.Content is ShellPage shellPage)
        {
            shellPage.RootFrame.Navigate(typeof(SubPages.UninstallProgressPage));
        }
    }
}