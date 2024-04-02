using System.Globalization;
using System.Windows.Data;
using NovelNode.Data;

namespace NovelNode.Helpers;
public class KeyValueVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = (KeyValue)value;
        var type = (Enums.ValueType)Enum.Parse(typeof(Enums.ValueType), (string)parameter);

        return key?.Type == type ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
