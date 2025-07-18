using CommunityToolkit.Mvvm.ComponentModel;
using WinResLoader = Windows.ApplicationModel.Resources.ResourceLoader;

namespace AtmosphereTool.ViewModels;

public partial class AtmosphereSettingsViewModel : ObservableRecipient
{
    public AtmosphereSettingsViewModel()
    {
    }
}
public class AtmosphereLocalizationHelper
{
    private readonly WinResLoader _resourceLoader = WinResLoader.GetForViewIndependentUse("AtmospherePage");

    public string GetLocalizedString(string key)
    {
        var localized = _resourceLoader.GetString(key);
        return string.IsNullOrEmpty(localized) ? key : localized;
    }
}