using System.Collections.ObjectModel;
using Notification.Wpf;
using NovelNode.Data;
using Wpf.Ui.Controls;
using NovelNode.Helpers;
using NovelNode.Views.Windows;
using NovelNode.Views.Pages;
using System.IO;
using Newtonsoft.Json;
using NetFabric.Hyperlinq;

namespace NovelNode.ViewModels.Windows;
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "Novel Node - The Visual Novel Creator";
    [ObservableProperty]
    private bool _isInitialized;

    [ObservableProperty]
    private ObservableCollection<ProjectData> _projects = [];

    public void OnStartup()
    {
        var projects = Directory.EnumerateDirectories(AppConfig.Instance.ProjectsPath)
            .AsValueEnumerable()
            .Where(x => File.Exists($"{x}\\ProjectData.json"))
            .ToList();

        foreach (var projectPath in projects)
        {
            var project = $"{projectPath}\\ProjectData.json".ReadText();
            Projects.Add(JsonConvert.DeserializeObject<ProjectData>(project));
        }
    }

    [RelayCommand]
    private async Task ProjectCreate()
    {
        var projectName = new TextBox
        {
            Text = string.Empty,
            PlaceholderText = "Project Name...",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var projectAuthor = new TextBox
        {
            Text = string.Empty,
            PlaceholderText = "Author / Creator...",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var importBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Projects",
            Content = new System.Windows.Controls.StackPanel { Width = 400, Children = { new System.Windows.Controls.TextBlock { Text = "Create Project", FontWeight = FontWeights.Bold, FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 50) }, projectName, projectAuthor } },
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };


        var result = await importBox.ShowDialogAsync();

        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary || string.IsNullOrEmpty(projectName.Text) || string.IsNullOrEmpty(projectAuthor.Text))
            return;

        var project = new ProjectData
        {
            Name = projectName.Text,
            Author = projectAuthor.Text,
            WordsCount = "0 Words"
        };

        var projectPath = Directory.CreateDirectory($"{AppConfig.Instance.ProjectsPath}\\{project.Name}").FullName;
        Directory.CreateDirectory($"{projectPath}\\Scenes");
        Directory.CreateDirectory($"{projectPath}\\Characters");
        Directory.CreateDirectory($"{projectPath}\\Blackboards");
        var projectData = JsonConvert.SerializeObject(project, Formatting.Indented);
        $"{projectPath}\\ProjectData.json".WriteText(projectData);
        
        Projects.Add(project);
        Extensions.Notify("Projects", $"Project {projectName.Text} created", NotificationType.None);
    }

    [RelayCommand]
    public async Task ProjectLoad(ProjectData project)
    {
        IsInitialized = true;
        ProjectData.Current = project;
        Extensions.ProjectLoad();

        var characters = Directory.EnumerateDirectories($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Characters")
            .AsValueEnumerable()
            .Where(x => File.Exists($"{x}\\Character.json"));

        foreach (var characterPath in characters)
        {
            var character = $"{characterPath}\\Character.json".ReadText();
            ProjectData.Current.Characters.Add(JsonConvert.DeserializeObject<CharacterData>(character));
        }

        var blackboards = Directory.EnumerateDirectories($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Blackboards")
            .AsValueEnumerable()
            .Where(x => File.Exists($"{x}\\Blackboard.json"));

        foreach (var blackboardPath in blackboards)
        {
            var blackboard = $"{blackboardPath}\\Blackboard.json".ReadText();
            ProjectData.Current.Blackboards.Add(JsonConvert.DeserializeObject<BlackboardData>(blackboard));
        }

        var scenes = Directory.EnumerateDirectories($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Scenes")
            .AsValueEnumerable()
            .Where(x => File.Exists($"{x}\\Scene.json"))
            .ToList();

        var scenesUnordered = new List<SceneData>();
        foreach (var scenePath in scenes)
        {
            var sceneData = $"{scenePath}\\Scene.json".ReadText();
            var scene = JsonConvert.DeserializeObject<SceneData>(sceneData);
            scenesUnordered.Add(scene);
        }
        ProjectData.Current.Scenes = new(scenesUnordered.OrderBy(x => x.ID));

        await Task.Delay(200);
        Extensions.Notify("Projects", $"Project {project.Name} loaded", NotificationType.None);
        App.GetService<MainWindow>().NavigationView.Navigate(typeof(HomePage));
    }


    [RelayCommand]
    public void ProjectDelete(ProjectData project)
    {
        Projects.Remove(project);
        Directory.Delete($"{AppConfig.Instance.ProjectsPath}\\{project.Name}", true);
        Extensions.Notify("Projects", $"Project {project.Name} deleted", NotificationType.None);
    }


}
