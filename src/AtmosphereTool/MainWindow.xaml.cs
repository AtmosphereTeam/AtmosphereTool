using AtmosphereTool.Helpers;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using Windows.UI.ViewManagement;
using WinRT;

namespace AtmosphereTool;

public sealed partial class MainWindow : WindowEx
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;
    private WindowsSystemDispatcherQueueHelper? _queueHelper;
    private DesktopAcrylicController? _acrylicController;
    private MicaController? _micaController;
    private SystemBackdropConfiguration? _backdropConfiguration;
    public string currentBackdrop;


    public MainWindow()
    {
        InitializeComponent();


        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event
        Closed += MainWindow_Closed;
        Activated += MainWindow_Activated;
        _ = AcrylicStatusAsync(); // Easier not to use await
        currentBackdrop = RegistryHelper.Read("HKCU", "SOFTWARE\\AtmosphereTool\\Settings", "Backdrop") as string ?? "Acrylic";
        SetBackdrop(currentBackdrop);

    }

    private async Task<bool> AcrylicStatusAsync()
    {
        var value = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "EnableTransparency",
            null
        );
        if (value is int intValue && intValue == 1)
        {
            return true;
        }
        else
        {
            var dialog = new ContentDialog
            {
                Title = "Hey!",
                Content = "It looks like window backdrops are disabled.\nWould you like to enable them for a more immersive experience?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.MainWindow.Content.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    Registry.SetValue(
                        @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                        "EnableTransparency",
                        1,
                        RegistryValueKind.DWord
                    );
                    return true;
                }
                catch (Exception ex)
                {
                    await ShowMessageDialogAsync($"Failed to enable transparency. Please enable it manually in Settings.\n {ex}", "Error");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }


    private void TrySetAcrylicBackdrop()
    {
        _queueHelper = new WindowsSystemDispatcherQueueHelper();
        _queueHelper.EnsureWindowsSystemDispatcherQueueController();

        if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
        {
            _backdropConfiguration = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Default
            };
            _acrylicController = new Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController
            {
                Kind = DesktopAcrylicKind.Thin
            };
            _acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
            _acrylicController.SetSystemBackdropConfiguration(_backdropConfiguration);
        }
        else
        {
            // I have no idea
        }
    }

    public void SetBackdrop(string tag)
    {
        // Dispose existing controllers
        _micaController?.Dispose();
        _micaController = null;
        _acrylicController?.Dispose();
        _acrylicController = null;

        switch (tag)
        {
            case "MicaAlt":
            case "Mica":
                if (MicaController.IsSupported())
                {
                    _backdropConfiguration = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration
                    {
                        IsInputActive = true,
                        Theme = SystemBackdropTheme.Default
                    };
                    _micaController = new MicaController
                    {
                        Kind = tag == "MicaAlt" ? MicaKind.BaseAlt : MicaKind.Base
                    };
                    _micaController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
                    _micaController.SetSystemBackdropConfiguration(_backdropConfiguration);
                    currentBackdrop = tag;
                }
                break;

            case "AcrylicThin":
            case "Acrylic":
                if (DesktopAcrylicController.IsSupported())
                {
                    _backdropConfiguration = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration
                    {
                        IsInputActive = true,
                        Theme = SystemBackdropTheme.Default
                    };
                    _acrylicController = new DesktopAcrylicController
                    {
                        Kind = tag == "AcrylicThin" ? DesktopAcrylicKind.Thin : DesktopAcrylicKind.Base
                    };
                    _acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
                    _acrylicController.SetSystemBackdropConfiguration(_backdropConfiguration);
                    currentBackdrop = tag;

                }
                break;

            default:
                currentBackdrop = "None";
                break;
        }
    }
    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {

        _acrylicController?.Dispose();
        _acrylicController = null;
        _micaController?.Dispose();
        _micaController = null;

        _backdropConfiguration = null;
        _queueHelper = null; // Safe unless it wraps unmanaged resources

        RegistryHelper.AddOrUpdate("HKCU", "SOFTWARE\\AtmosphereTool\\Settings", "Backdrop", currentBackdrop, "REG_SZ");

        var programData = Environment.ExpandEnvironmentVariables("%ProgramData%");
        var crashmarker = Path.Combine(programData, "AtmosphereTool\\Logs\\Crash.marker");
        if (File.Exists(crashmarker))
        {
            try
            {
                File.Delete(crashmarker);
            }
            catch
            {
                // ignore
            }
        }

        //GC.Collect(); // helps finalize unmanaged COM stuff
        //GC.WaitForPendingFinalizers();
    }


    // this handles updating the caption button colors correctly when indows system theme is changed
    // while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.CodeActivated)
        {
            if (!string.IsNullOrEmpty(currentBackdrop))
            {
                SetBackdrop(currentBackdrop);
            }
        }
    }

}
