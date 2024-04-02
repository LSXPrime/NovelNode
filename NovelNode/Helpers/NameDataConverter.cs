using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using NovelNode.Data;
using NovelNode.ViewModels.Pages;

namespace NovelNode.Helpers
{
    public class NameDataConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;

            string name = value.ToString();
            string target = parameter.ToString();
            string projectPath = AppConfig.Instance.ProjectsPath;
            string currentProjectName = ProjectData.Current?.Name;

            switch (target)
            {
                case "AbsolutePathSprite":
                    return LoadBitmapImage(name);
                case "RelativePathSprite":
                    string relativePathSprite = Path.Combine(projectPath, currentProjectName, name);
                    return LoadBitmapImage(relativePathSprite);
                case "Background":
                    var selectedScene = HomeViewModel.Instance.SceneSelected;
                    if (selectedScene != null)
                    {
                        var background = selectedScene.Backgrounds.FirstOrDefault(x => x.Key == name);
                        if (background != null)
                        {
                            string backgroundPath = Path.Combine(projectPath, currentProjectName, background.Value.String);
                            return LoadBitmapImage(backgroundPath);
                        }
                    }
                    return null;
                case "SceneAudioPath":
                    var selectedSceneAudio = HomeViewModel.Instance.SceneSelected?.Sounds.FirstOrDefault(x => x.Name == name);
                    return selectedSceneAudio != null ? Path.Combine(projectPath, currentProjectName, selectedSceneAudio.Path) : null;
                case "Character":
                    return ProjectData.Current?.Characters.FirstOrDefault(x => x.Name == name);
                case "CharacterSpritePath":
                    var selectedCharacter = HomeViewModel.Instance.CharacterSelected;
                    if (selectedCharacter != null)
                    {
                        var charSprite = selectedCharacter.Sprites.FirstOrDefault(x => x.Key == name);
                        if (charSprite != null)
                        {
                            string charSpritePath = Path.Combine(projectPath, currentProjectName, charSprite.Value.String);
                            return LoadBitmapImage(charSpritePath);
                        }
                    }
                    return null;
                case "CharacterAudioPath":
                    var selectedCharacterAudio = HomeViewModel.Instance.CharacterSelected?.Sounds.FirstOrDefault(x => x.Name == name);
                    return selectedCharacterAudio != null ? Path.Combine(projectPath, currentProjectName, selectedCharacterAudio.Path) : null;
                case "NodeCharacterSpritePath":
                    if (value is NodeCharacter nodeSprite)
                    {
                        var sprite = nodeSprite.CharacterData?.Sprites.FirstOrDefault(x => x.Key == nodeSprite.Sprite);
                        if (sprite != null)
                        {
                            string nodeSpritePath = Path.Combine(projectPath, currentProjectName, sprite.Value.String);
                            return LoadBitmapImage(nodeSpritePath);
                        }
                    }
                    return null;
                default:
                    return null;
            }
        }

        private BitmapImage LoadBitmapImage(string imagePath)
        {
            if (File.Exists(imagePath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                bitmap.EndInit();
                return bitmap;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
