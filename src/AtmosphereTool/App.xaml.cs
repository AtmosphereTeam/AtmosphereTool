using AtmosphereTool.Activation;
using AtmosphereTool.Contracts.Services;
using AtmosphereTool.Core.Contracts.Services;
using AtmosphereTool.Core.Services;
using AtmosphereTool.Helpers;
using AtmosphereTool.Models;
using AtmosphereTool.Notifications;
using AtmosphereTool.Services;
using AtmosphereTool.ViewModels;
using AtmosphereTool.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AtmosphereTool;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar
    {
        get; set;
    }

    public App()
    {
        // ApplicationLanguages.PrimaryLanguageOverride = "en-US";
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();
            services.AddSingleton<IBackdropService>(provider => { return new BackdropService(MainWindow); });


            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<AtmosphereSettingsViewModel>();
            services.AddTransient<AtmosphereSettingsPage>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<WindowsSettingsViewModel>();
            services.AddTransient<WindowsSettingsPage>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        LogHelper.Initialize();

        App.GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        LogHelper.LogCritical($"Encountered an Unhandled Exception: {e.Exception}");
        LogHelper.LogCritical($"Exception Message: {e.Message}");
        var programData = Environment.ExpandEnvironmentVariables("%ProgramData%");
        File.WriteAllText(Path.Combine(programData, "AtmosphereTool\\Logs\\Crash.marker"), "Crash marker created at " + DateTime.Now + $"\n Crash trace: {e.Exception} \n\n{e.Message}");
    }

    public bool IsRunningAtmosphereOS = RegistryHelper.Read("HKLM", "SOFTWARE\\AME\\Playbooks\\Applied\\{8BBB362C-858B-41D9-A9EA-83A4B9669C43}", "Version") != null;

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        App.GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));

        await App.GetService<IActivationService>().ActivateAsync(args);

        var programData = Environment.ExpandEnvironmentVariables("%ProgramData%");
        if (File.Exists(Path.Combine(programData, "AtmosphereTool\\Logs\\Crash.marker")))
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = "Crash Detected",
                    Content = "Hey! It looks like Atmosphere Tool crashed last time. \nIf this continues then please contact our team. The logs are in ProgramData.",
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = App.MainWindow.Content.XamlRoot
                };
                await dialog.ShowAsync();
            });

        }
        _ = Task.Run(async () => { await Update.Update.UpdateTool(); });

    }
}
