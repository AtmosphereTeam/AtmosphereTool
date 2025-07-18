using System.Collections.ObjectModel;
using AtmosphereTool.Uninstall;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using WinResLoader = Windows.ApplicationModel.Resources.ResourceLoader;

namespace AtmosphereTool.Views;

public sealed partial class UninstallProgressPage : Page
{
    public ObservableCollection<string> StatusMessages { get; } = new();

    private readonly WinResLoader _resourceLoader = WinResLoader.GetForViewIndependentUse("Uninstall");

    public string GetLocalizedString(string key)
    {
        var localized = _resourceLoader.GetString(key);
        return string.IsNullOrEmpty(localized) ? key : localized;
    }

    public UninstallProgressPage()
    {
        InitializeComponent();
        StatusListView.ItemsSource = StatusMessages;
        PreUninstall();
    }

    public void SetProgress(double percent)
    {
        ProgressBar.IsIndeterminate = false;
        ProgressBar.Value = percent;
    }

    public void SetIndeterminate()
    {
        ProgressBar.IsIndeterminate = true;
    }

    public void AddStatus(string message)
    {
        StatusMessages.Add(message);
        SetMessage(message);
        StatusListView.ScrollIntoView(StatusMessages.LastOrDefault());
    }

    public void SetMessage(string message)
    {
        MessageText.Text = message;
    }

    private async void PreUninstall()
    {
        var title = GetLocalizedString("PreUninstall_Title");
        var content = GetLocalizedString("PreUninstall_Content");
        var primaryText = GetLocalizedString("Dialog_PrimaryText");
        var closeText = GetLocalizedString("Dialog_CloseText");

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.MainWindow.Content.XamlRoot  // Really important
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            StartUninstall();
        }
        else
        {
            if (App.MainWindow.Content is ShellPage shellPage)
            {
                shellPage.RootFrame.Navigate(typeof(AtmosphereSettingsPage));
            }
            return;
        }
    }
    private async void StartUninstall()
    {
        var deameliorate = new Deameliorate();
        var result = await deameliorate.DeameliorateCore(this);
        if (result == false)
        {
            ShowErrorDialog("Error", "Something went wrong");
        }
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