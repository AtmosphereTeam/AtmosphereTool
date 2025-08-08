using AtmosphereTool.FeaturePages;
using AtmosphereTool.Helpers;
using AtmosphereTool.Views;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AtmosphereTool.SubPages;

public sealed partial class DebloatSendTo : Page
{
    public ObservableCollection<SendToItems> items = [];
    public DebloatSendTo()
    {
        GetSendToItems();
        InitializeComponent();
    }
    private void GetSendToItems()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(RegistryHelper.Read("HKCU", $"{RegistryHelper.GetCurrentUserSid()}\\Volatile Environment", "APPDATA") as string ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\SendTo")))
        {
            items.Add(new() { FileName = Path.GetFileNameWithoutExtension(file), FilePath = file, NotHidden = File.GetAttributes(file) != FileAttributes.Hidden });
        }
    }
    private void SendToItemsApply(object sender, RoutedEventArgs e)
    {
        if (items == null) { return; }
        LogHelper.LogInfo("[DebloatSendTo]: Applying Properties");
        foreach (var item in items)
        {
            if (item.NotHidden == false)
            {
                File.SetAttributes(item.FilePath, ~FileAttributes.Hidden);
            }
            if (item.NotHidden == true)
            {
                File.SetAttributes(item.FilePath, FileAttributes.Hidden);
            }
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
        if (App.MainWindow.Content is ShellPage shellPage)
        {
            shellPage.RootFrame.Navigate(typeof(InterfaceTweaks));
        }
    }
}

public class SendToItems
{
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public required bool NotHidden { get; set; }
}