using AtmosphereTool.Helpers;
using AtmosphereTool.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace AtmosphereTool.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<SettingsViewModel>();
        ViewModel.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());
        ViewModel.LoadBackdropOptions();
    }

    private void ViewLogsClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (App.MainWindow.Content is ShellPage shellPage)
        {
            shellPage.RootFrame.Navigate(typeof(SubPages.LogsView));
        }
    }
    private async void CheckforUpdatesClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        LogHelper.LogInfo("CheckforUpdates button clicked");
        var button = (Button)sender;
        button.IsEnabled = false;
        var (available, _) = await Update.Update.CheckForUpdate();
        if (available)
        {
            var updatedialog = new ContentDialog
            {
                Title = "Update",
                Content = "A newer version of AtmosphereTool is out. \nWould you like to update?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = App.MainWindow.Content.XamlRoot
            };
            if (await updatedialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var updater = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update\\Updater.ps1");
                if (File.Exists(updater))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{updater}\"",
                        UseShellExecute = true,
                        Verb = AdminHelper.IsAdministrator ? "runas" : ""
                    };
                    Process.Start(psi);
                    Environment.Exit(0);
                }
            }
        }
        button.IsEnabled = true;
    }
}
