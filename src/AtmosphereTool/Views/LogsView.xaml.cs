using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using AtmosphereTool.Helpers;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AtmosphereTool.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>

public sealed partial class LogsView : Page
{
    public ObservableCollection<ParsedLogEntry> LogEntries { get; } = new();

    public LogsView()
    {
        InitializeComponent();
        LoadLogList();
        LoadParsedLog(LogHelper.LatestLog);
        LogSelector.ItemsSource = LogFiles;
        LogListView.ItemsSource = LogEntries;
    }

    public class LogEntry
    {
        public string DisplayName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    public ObservableCollection<LogEntry> LogFiles { get; } = [];

    private void LoadLogList()
    {
        var files = Directory.GetFiles(LogHelper.LogDirectory, "*.md");
        LogFiles.Clear();
        foreach (var file in files)
        {
            var filename = Path.GetFileName(file);
            LogFiles.Add(new LogEntry
            {
                DisplayName = filename.Equals("latest.md", StringComparison.OrdinalIgnoreCase)
                    ? "Latest Log"
                    : filename,
                FilePath = file
            });
        }
    }

    private void LogSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LogSelector.SelectedItem is LogEntry selectedEntry)
        {
            CurrentLog.Text = selectedEntry.DisplayName;
            var entries = LoadParsedLog(selectedEntry.FilePath);
            CurrentLog.Text = selectedEntry.DisplayName;
            LogEntries.Clear();
            foreach (var entry in entries)
            {
                LogEntries.Add(entry);
            }
        }
    }
    private List<ParsedLogEntry> LoadParsedLog(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var entries = new List<ParsedLogEntry>();
        var start = false;

        foreach (var line in lines)
        {
            if (line.Trim() == "## Logs")
            {
                start = true;
                continue;
            }

            if (!start || !line.StartsWith("- ["))
                continue;

            var match = Regex.Match(line, @"- \[(.*?) \| (.*?)\] (.+)");
            if (match.Success)
            {
                entries.Add(new ParsedLogEntry
                {
                    Level = match.Groups[1].Value,
                    Time = match.Groups[2].Value,
                    Message = match.Groups[3].Value
                });
            }
        }

        return entries;
    }

    private async void OpenLogsFolderClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await CommandHelper.RunProcess("explorer", LogHelper.LogDirectory);
    }
}
public class ParsedLogEntry
{
    public string? Level
    {
        get; set;
    }
    public string? Time
    {
        get; set;
    }
    public string? Message
    {
        get; set;
    }
}