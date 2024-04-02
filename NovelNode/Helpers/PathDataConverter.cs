using System.Globalization;
using System.Windows.Data;
using NetFabric.Hyperlinq;
using NovelNode.Data;
using NovelNode.ViewModels.Pages;

namespace NovelNode.Helpers;
public class PathDataConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string type = (string)value;
        string target = (string)parameter;
        return target switch
        {
            "Character" => ProjectData.Current.Characters.First(x => x.Name == type),
            "Background" => HomeViewModel.Instance.SceneSelected.Backgrounds.First(x => x.Key == type),
            "BackgroundPath" => HomeViewModel.Instance.SceneSelected.Backgrounds.First(x => x.Key == type).Value,
            _ => null,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
