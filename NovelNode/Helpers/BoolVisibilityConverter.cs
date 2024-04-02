using System.Globalization;
using System.Windows.Data;

namespace NovelNode.Helpers;
public class BoolVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool current = (bool)value;
        bool required = (string)parameter == "true";
        if (current == required)
            return Visibility.Visible;
        else
            return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
