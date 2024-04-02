using System.Globalization;
using System.Windows.Data;

namespace NovelNode.Helpers;
public class EnumVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || value is not Enum || parameter is not string)
            return DependencyProperty.UnsetValue;

        bool invert = ((string)parameter).StartsWith("!");
        var enumValue = (Enum)value;
        var paramValue = Enum.Parse(enumValue.GetType(), invert ? ((string)parameter)[1..] : (string)parameter, true);

        return (invert ? enumValue.ToString() != paramValue.ToString() : enumValue.ToString() == paramValue.ToString()) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
