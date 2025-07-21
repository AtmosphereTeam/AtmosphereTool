using AtmosphereTool.Helpers;
using AtmosphereTool.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

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
        await Update.Update.UpdateTool();
        button.IsEnabled = true;
    }
}
