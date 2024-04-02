using System.IO;
using NovelNode.Helpers;

namespace NovelNode.Data;

public partial class AppConfig : ObservableObject
{
    #region API
    [ObservableProperty]
    private string _dataSecretKey = "USER_SECRET_KEY_TO_DECRYPT_DATA";
    #endregion
    #region Paths
    [ObservableProperty]
    private string _projectsPath = string.Empty;
    private static readonly string filePath = $"{Directory.GetCurrentDirectory()}\\Config.json";
    #endregion

    public AppConfig()
    {
        ProjectsPath = $"{Directory.GetCurrentDirectory()}\\Resources\\Projects";
    }

    // Load settings from a JSON file
    public void LoadSelf()
    {
        if (File.Exists(filePath))
        {
            AppConfig self = new();
            var json = filePath.ReadText();
            self = Newtonsoft.Json.JsonConvert.DeserializeObject<AppConfig>(json);
            ProjectsPath = self.ProjectsPath;

        }

        CheckPaths();
        Save();
    }

    private void CheckPaths()
    {
        Directory.CreateDirectory(ProjectsPath);
    }

    // Save settings to a JSON file
    public void Save()
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        filePath.WriteText(json);
        CheckPaths();
        ConfigSavedEvent?.Invoke();
    }

    public void Reset()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);

        ProjectsPath = $"{Directory.GetCurrentDirectory()}\\Resources\\Projects";
        Save();
    }

    public delegate void ConfigSavedDelegate();
    public event ConfigSavedDelegate ConfigSavedEvent;

    private static AppConfig? instance;
    public static AppConfig Instance => instance ??= new AppConfig();
}
