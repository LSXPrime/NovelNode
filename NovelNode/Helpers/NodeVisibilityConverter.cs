using System.Globalization;
using System.Windows.Data;

namespace NovelNode.Helpers;
public class NodeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string type = (string)value;
        string target = (string)parameter;
        if (type == target)
            return Visibility.Visible;
        else
            return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
