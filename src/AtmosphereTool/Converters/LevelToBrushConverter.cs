using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AtmosphereTool.Converters;
public partial class LevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            "Info" => new SolidColorBrush(Color.FromArgb(255, 49, 120, 198)),
            "Warning" => new SolidColorBrush(Color.FromArgb(255, 225, 53, 0)),
            "Error" => new SolidColorBrush(Color.FromArgb(255, 220, 53, 69)),
            "Critical" => new SolidColorBrush(Color.FromArgb(255, 176, 0, 32)),
            _ => new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}