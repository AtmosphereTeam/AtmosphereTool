using Microsoft.Windows.ApplicationModel.Resources;

namespace AtmosphereTool.Helpers;

public static class ResourceExtensions
{
    // With my current setup. This is useless
    private static readonly ResourceLoader _resourceLoader = new();

    public static string GetLocalized(this string resourceKey) => _resourceLoader.GetString(resourceKey);
}
