using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using AtmosphereTool.Contracts.Services;
using AtmosphereTool.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace AtmosphereTool.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService? _themeSelectorService;
    private readonly IBackdropService _backdropService;
    private DispatcherQueue? _dispatcherQueue;

    // [ObservableProperty]
    private ElementTheme? _elementTheme;
    public ElementTheme? ElementTheme
    {
        get => _elementTheme;
        set => SetProperty(ref _elementTheme, value);
    }

    // [ObservableProperty]
    private string? _versionDescription;
    public string? VersionDescription
    {
        get => _versionDescription;
        set => SetProperty(ref _versionDescription, value);
    }

    public ICommand? SwitchThemeCommand
    {
        get;
    }

    public ObservableCollection<BackdropItem> AvailableBackdrops { get; } = new();

    private BackdropItem? _selectedBackdropItem;
    public BackdropItem? SelectedBackdropItem
    {
        get => _selectedBackdropItem;
        set
        {
            if (SetProperty(ref _selectedBackdropItem, value) && value != null)
            {
                if (value.Type.ToString() == _backdropService.CurrentBackdrop) { return; }
                _backdropService.SetBackdrop(value.Type.ToString());
            }
        }
    }

    public void SetDispatcherQueue(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    public SettingsViewModel(IThemeSelectorService themeSelectorService, IBackdropService backdropService)
    {
        _themeSelectorService = themeSelectorService;
        _backdropService = backdropService;
        _elementTheme = _themeSelectorService.Theme;
        _versionDescription = GetVersionDescription();

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });
    }

    public void LoadBackdropOptions()
    {
        var CurrentBackdrop = _backdropService.CurrentBackdrop;
        try
        {
            AvailableBackdrops.Clear();

            if (MicaController.IsSupported())
            {
                AvailableBackdrops.Add(new BackdropItem { Type = BackdropType.Mica, DisplayName = "Mica" });
                AvailableBackdrops.Add(new BackdropItem { Type = BackdropType.MicaAlt, DisplayName = "Mica (Alt)" });
            }

            if (DesktopAcrylicController.IsSupported())
            {
                AvailableBackdrops.Add(new BackdropItem { Type = BackdropType.Acrylic, DisplayName = "Acrylic" });
                AvailableBackdrops.Add(new BackdropItem { Type = BackdropType.AcrylicThin, DisplayName = "Acrylic Thin" });
            }
            AvailableBackdrops.Add(new BackdropItem { Type = BackdropType.None, DisplayName = "None" });

            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (Enum.TryParse<BackdropType>(CurrentBackdrop, true, out var backdropType))
                {
                    SelectedBackdropItem = AvailableBackdrops.FirstOrDefault(x => x.Type == backdropType);
                }
                else
                {
                    SelectedBackdropItem = AvailableBackdrops.FirstOrDefault(x => x.Type == BackdropType.Acrylic);
                }
            });
        }
        catch (Exception e)
        {
            LogHelper.LogCritical(e.Message);
        }
    }


    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}";
    }
}
public enum BackdropType
{
    None,
    MicaAlt,
    Mica,
    AcrylicThin,
    Acrylic
}
public class BackdropItem
{
    public BackdropType Type
    {
        get; set;
    }
    public string DisplayName { get; set; } = string.Empty;
}
