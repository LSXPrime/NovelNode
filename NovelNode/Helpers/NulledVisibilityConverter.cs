using System.Globalization;
using System.Windows.Data;

namespace NovelNode.Helpers;
public class NulledVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool nulled = value is null;
        bool exist = (string)parameter == "true";
        if (!exist && nulled)
            return Visibility.Visible;
        else if (exist && !nulled)
            return Visibility.Visible;
        else
            return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
