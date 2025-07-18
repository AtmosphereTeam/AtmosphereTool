using System.Diagnostics;
using System.Security;
using System.Text.RegularExpressions;
using AtmosphereTool.Helpers;
using Microsoft.Dism;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinResLoader = Windows.ApplicationModel.Resources.ResourceLoader;

namespace AtmosphereTool.Uninstall
{
    public class SelectWindowsImage
    {
        private readonly WinResLoader _resourceLoader = WinResLoader.GetForViewIndependentUse("Uninstall");

        public string GetLocalizedString(string key)
        {
            var localized = _resourceLoader.GetString(key);
            return string.IsNullOrEmpty(localized) ? key : localized;
        }
        private static string? _fileViolationTest;
        private static bool CheckFileViolation(string inputFile)
        {
            try
            {
                _fileViolationTest = inputFile;
            }
            catch (SecurityException e)
            {
                LogHelper.LogCritical("Security exception: " + e.Message);

                return true;
            }

            return false;
        }

        public static string GetWindowsVersion(float majorMinor, int isoBuild)
        {
            return (majorMinor, isoBuild) switch
            {
                (6, _) => "Windows Vista",
                (6.1f, _) => "Windows 7",
                (6.2f, _) => "Windows 8",
                (6.3f, _) => "Windows 8.1",
                (10, var a) when a < 19041 => "Windows 10 (Old)",
                (10, var a) when a >= 22000 => "Windows 11",
                (10, _) => "Windows 10",
                _ => "Unknown"
            };
        }

        public static bool DismountIso(string imagePath)
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                FileName = "PowerShell.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"-NoP -C \"Dismount-DiskImage '{imagePath}'\"",
                RedirectStandardOutput = true
            };

            var proc = Process.Start(startInfo);
            if (proc == null) return false;
            proc.WaitForExit();
            return true;
        }

        /// <summary>
        /// Asks user to select Windows installation media, mounts it if applicable, and checks its version
        /// </summary>
        /// <param name="winVersionsMustMatch">If true when ISO and host versions mismatch, prompts user that things can break if they continue</param>
        /// <param name="isoBuildMustBeReturned">If true and the ISO build can't be retrieved, prompts a user with an error</param>
        public async Task<(
            string? MountedPath, string? IsoPath, string? Winver, int? Build, bool? VersionsMatch
            )> GetMediaPath(bool winVersionsMustMatch = false, bool isoBuildMustBeReturned = false)
        {
            string? _mountedPath = null;
            string? _isoPath = null;
            string? _isoWinVer = null;
            var _isoBuild = -1;

            var error = ((string?)null, "none", (string?)null, (int?)null, (bool?)null);

            // Tell the user that they need a windows iso
            var title = GetLocalizedString("IsoMediaOption_Title");
            var content = GetLocalizedString("IsoMediaOption_Content");
            var primaryText = GetLocalizedString("Dialog_PrimaryText");
            var closeText = GetLocalizedString("Dialog_CloseText");

            var dialog1 = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryText,
                CloseButtonText = closeText,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.MainWindow.Content.XamlRoot
            };

            if (await dialog1.ShowAsync() != ContentDialogResult.Primary)
            {
                return error;
            }

            // Ask the user to give the path to iso
            var placeholderText = GetLocalizedString("FilePicker_Text");
            var filepickerBox = new TextBox
            {
                PlaceholderText = placeholderText,
                Margin = new Thickness(0, 0, 0, 12),
                MinWidth = 300
            };
            var dialogStack = new StackPanel();
            dialogStack.Children.Add(filepickerBox);
            var dialog2 = new ContentDialog
            {
                Title = "Iso Path",
                Content = dialogStack,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.MainWindow.Content.XamlRoot,
            };
            var result = await dialog2.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (filepickerBox.Text != string.Empty && (File.Exists(filepickerBox.Text.Trim().Trim('"').Trim('\''))))
                {
                    _isoPath = filepickerBox.Text;
                }
            }
            else
            {
                return error;
            }

            // Mount iso
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                FileName = "PowerShell.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"-NoP -C \"(Mount-DiskImage '{_isoPath}' -PassThru | Get-Volume).DriveLetter + ':\'\"",
                RedirectStandardOutput = true
            };

            var proc = Process.Start(startInfo);
            if (proc == null)
            {
                return error;
            }
            proc.WaitForExit();

            var output = await proc.StandardOutput.ReadLineAsync() ?? "";

            if (proc.ExitCode != 0 || string.IsNullOrWhiteSpace(output) || !Regex.IsMatch(output.Trim(), @"^[A-Z]:$"))
            {
                return error;
            }

            _mountedPath = output.Trim();

            // Check WIM version
            var wimOrEsdPath = new[] { $@"{_mountedPath}\sources\install.esd", $@"{_mountedPath}\sources\install.wim" }.FirstOrDefault(File.Exists);
            if (!string.IsNullOrEmpty(wimOrEsdPath))
            {
                try
                {
                    DismApi.Initialize(DismLogLevel.LogErrors);

                    string? previousIndexVersion = null;
                    string? isoFullVersion = null;
                    var multiVersion = false;

                    var imageInfos = DismApi.GetImageInfo(wimOrEsdPath);
                    foreach (var imageInfo in imageInfos)
                    {
                        isoFullVersion = imageInfo.ProductVersion.ToString();
                        if (isoFullVersion != previousIndexVersion && previousIndexVersion != null)
                        {
                            // If it's multi-version, WinVer will be "Unknown" as well
                            multiVersion = true;
                            isoFullVersion = "0.0.0.0";
                            break;
                        }
                        previousIndexVersion = isoFullVersion;
                    }

                    switch (multiVersion)
                    {
                        case true when isoBuildMustBeReturned:
                            LogHelper.LogError($"[WindowsIsoChcek]: Multi-Edition iso detected. Cancelling");
                            ShowErrorDialog("Multi-Iso", "Multiple Windows versions were found in the Windows image, can't determine which Windows build it is. \nPlease use an unmodified Windows ISO.");
                            return error;
                        case true when winVersionsMustMatch:
                            LogHelper.LogError($"[WindowsIsoChcek]: Multi-Edition iso detected. Cancelling");
                            ShowErrorDialog("Multi-Iso", "Multiple Windows versions were found in the Windows image, can't determine which Windows build it is. \nIf your Windows version doesn't match the ISO, there will be problems.");
                            break;
                    }

                    if (isoFullVersion == null)
                    {
                        return error;
                    }
                    try
                    {
                        var buildSplit = isoFullVersion.Split('.');
                        _isoBuild = int.Parse(buildSplit[2]);
                        _isoWinVer = GetWindowsVersion(float.Parse($"{buildSplit[0]}.{buildSplit[1]}"), _isoBuild);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogCritical($"[WindowsIsoChcek]: Error checking ISO version: {ex.Message.Trim()}");
                        ShowErrorDialog("ISO Error", $"Error checking ISO version: {ex.Message.Trim()}");
                        return error;
                    }
                    finally
                    {
                        try { DismApi.Shutdown(); } catch { }
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogCritical("Error checking ISO version: \n" + e.Message.TrimEnd('\n').TrimEnd('\r'));
                    ShowErrorDialog("Error", "Error checking ISO version: " + e.Message.TrimEnd('\n').TrimEnd('\r'));
                }

                // Check the current OS version
                var hostVersion = Environment.OSVersion.Version;
                var hostWinver = GetWindowsVersion(float.Parse($"{hostVersion.Major}.{hostVersion.Minor}"), hostVersion.Build);

                // If it all matches & winVersionsMustMatch
                if (hostWinver == _isoWinVer)
                {
                    return (_mountedPath, _isoPath, _isoWinVer, _isoBuild, true);
                }

                // If ISO version doesn't match host version & winVersionsMustMatch
                if (hostWinver != _isoWinVer && winVersionsMustMatch)
                {
                    if (!string.IsNullOrEmpty(_isoPath))
                    {
                        DismountIso(_isoPath);
                    }
                    LogHelper.LogError($"Version Mismatch host: {hostWinver}  iso: {_isoWinVer}");
                    ShowErrorDialog("Version Mismatch", $"You're on {hostWinver}, but the selected image is {_isoWinVer}. \nYou can only use an ISO that matches your Windows version.");
                    return error;
                }

                // If ISO version doesn't match host version, and winVersionsMustMatch is true 
                if (hostWinver != _isoWinVer)
                {
                    return (_mountedPath, _isoPath, _isoWinVer, _isoBuild, false);
                }
            }

            LogHelper.LogError("No version found");
            ShowErrorDialog("No version found", $"No Windows installation image was found inside the selected Windows media. \nNo version check can be done, things might break.");

            return isoBuildMustBeReturned ? error : (_mountedPath, _isoPath, null, null, null);
        }

        public void ShowErrorDialog(string title, string content)
        {
            var primaryText = GetLocalizedString("Dialog_PrimaryText");
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
    }
}
