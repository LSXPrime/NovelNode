using System.Collections.ObjectModel;
using NovelNode.Data;
using NovelNode.Enums;
using NovelNode.Views.Pages;
using NovelNode.Views.Windows;
using NovelNode.Helpers;
using NetFabric.Hyperlinq;
using Newtonsoft.Json;
using Notification.Wpf;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using NAudio.Wave;
using NovelNodePlayer.Data;

namespace NovelNode.ViewModels.Pages;

public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Node> _toolbox = [
            new NodeState(),
            new NodeBackground(),
            new NodeCharacter(),
            new NodeDialogue(),
            new NodeChoice(),
            new NodeEvent(),
            new NodeCondition(),
            new NodeScene(),
            new NodeCheckpoint(),
            new NodeCombiner()
        ];

    [ObservableProperty]
    private ObservableCollection<Node> _selectedNodes = [];
    public NodeConnectionPending PendingConnection { get; } = new();

    [ObservableProperty]
    private CharacterData? _characterSelected;
    [ObservableProperty]
    private SceneData? _sceneSelected;
    [ObservableProperty]
    private BlackboardData? _blackboardSelected;


    public HomeViewModel() 
    {
        instance = this;
        SceneSelected = ProjectData.Current?.Scenes.Count > 0 ? ProjectData.Current.Scenes[0] : null;
    }

    #region Nodes

    public void NodeConnect(NodeConnector source, NodeConnector target)
    {
        if (source == null || target == null || source.IsConnected || target.IsConnected)
            return;

        if (source.Flow == NodeConnectorFlow.Input)
            (target, source) = (source, target);

        var sourceNode = SceneSelected.Nodes.AsValueEnumerable().FirstOrDefault(x => x.Output != null && x.Output.Contains(source));
        var targetNode = SceneSelected.Nodes.AsValueEnumerable().FirstOrDefault(x => x.Input != null && x.Input.Contains(target));
        targetNode.Previous = sourceNode.Guid;
        if (sourceNode is NodeChoice sourceChoice)
            sourceChoice.Choices[sourceNode.Output.IndexOf(source)].Next = targetNode.Guid;
        else
            sourceNode.Next = targetNode.Guid;

        SceneSelected.Connections.Add(new NodeConnection(source, target));
    }

    [RelayCommand]
    public void NodeDisconnect(NodeConnector connector)
    {
        try
        {
            var connection = SceneSelected?.Connections.AsValueEnumerable().First(x => x.Source == connector || x.Target == connector);
            if (connection != null)
            {
                connection.Source.IsConnected = false;  // This is not correct if there are multiple connections to the same connector
                connection.Target.IsConnected = false;

                if (connection.Source.Flow == NodeConnectorFlow.Input)
                    connection.Swap();

                var sourceNode = SceneSelected?.Nodes.AsValueEnumerable().FirstOrDefault(x => x.Output != null && x.Output.Contains(connection.Source));
                var targetNode = SceneSelected?.Nodes.AsValueEnumerable().FirstOrDefault(x => x.Input != null && x.Input.Contains(connection.Target));
                targetNode.Previous = Guid.Empty;

                if (sourceNode is NodeChoice sourceChoice)
                    sourceChoice.Choices[sourceNode.Output.IndexOf(connection.Source)].Next = Guid.Empty;
                else if (sourceNode != null)
                    sourceNode.Next = Guid.Empty;

                SceneSelected?.Connections.Remove(connection);
            }

            // Cleanup
            var invalidConnections = SceneSelected?.Connections.AsValueEnumerable().Where(x => x.Source == null || x.Target == null);
            foreach (var invalid in invalidConnections)
                SceneSelected?.Connections.Remove(invalid);
        }
        catch (Exception ex) 
        {
            Extensions.Notify("Scenes", $"Node Disconnection Failed, Exception: {ex.Message}", NotificationType.Error);
        }
    }

    [RelayCommand]
    private void NodeSelectionDelete()
    {
        for (int i = SelectedNodes.Count - 1; i >= 0; i--)
        {
            Node? node = SelectedNodes[i];
            var connection = SceneSelected.Connections.AsValueEnumerable().FirstOrDefault(x => (node.Input != null && node.Input.Contains(x.Target)) || (node.Output != null && node.Output.Contains(x.Source)));
            if (connection != null)
            {
                NodeDisconnect(connection.Source);
                SceneSelected.Connections.Remove(connection);
            }

            SceneSelected.Nodes.Remove(node);
        }
        SelectedNodes.Clear();
    }

    #endregion

    #region Characters

    private static async Task AddCharacter(string title, Func<string, string, Task> importAction, string filter = null)
    {
        var avatarPath = filter == null ? await ShowImagePickerDialogAsync(title) : await BrowseFileDialogAsync(filter);
        if (avatarPath == null)
            return;

        var characterName = await ShowInputDialogAsync(title, "Entity name...");
        if (characterName == null)
            return;

        await importAction(avatarPath, characterName);
    }

    [RelayCommand]
    private static async Task CharacterAdd()
    {
        static async Task ImportAction(string avatarPath, string characterName)
        {
            var characterPath = Directory.CreateDirectory($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Characters\\{characterName}").FullName;
            var newAvatarPath = $"{characterPath}\\Avatar{Path.GetExtension(avatarPath)}";
            await avatarPath.CopyStreamToAsync(newAvatarPath);

            var character = new CharacterData
            {
                Avatar = $"Characters\\{characterName}\\Avatar{Path.GetExtension(avatarPath)}",
                Name = characterName
            };

            var characterData = JsonConvert.SerializeObject(character, Formatting.Indented);
            await $"{characterPath}\\Character.json".WriteTextAsync(characterData);

            ProjectData.Current.Characters.Add(character);
            Extensions.Notify("Characters", $"Character {characterName} added", NotificationType.None);
        }

        await AddCharacter("Add Character", ImportAction);
    }

    [RelayCommand]
    private void CharacterDelete()
    {
        if (CharacterSelected == null)
            return;

        var characterName = CharacterSelected.Name;
        ProjectData.Current.Characters.Remove(CharacterSelected);
        Directory.Delete($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Characters\\{characterName}", true);
        Extensions.Notify("Characters", $"Character {characterName} deleted", NotificationType.None);
    }

    [RelayCommand]
    private async Task CharacterSpriteAdd()
    {
        async Task ImportAction(string spritePath, string emotion)
        {
            var characterPath = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Characters\\{CharacterSelected.Name}";
            var newSpritePath = $"{characterPath}\\Sprites\\{emotion}{Path.GetExtension(spritePath)}";
            Directory.CreateDirectory($"{characterPath}\\Sprites");
            await spritePath.CopyStreamToAsync(newSpritePath);

            CharacterSelected.Sprites.Add(new KeyValue { Key = emotion, Value = new KeyValue.ValueData { String = $"Characters\\{CharacterSelected.Name}\\Sprites\\{emotion}{Path.GetExtension(spritePath)}" }, Type = Enums.ValueType.String } );

            var characterData = JsonConvert.SerializeObject(CharacterSelected, Formatting.Indented);
            await $"{characterPath}\\Character.json".WriteTextAsync(characterData);
        }

        await AddCharacter("Add Emotion", ImportAction);
    }

    [RelayCommand]
    private void CharacterSpritePreview(KeyValue emotion)
    {
        var bitmapImage = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\{emotion.Value.String}".GetBitmapImage();

        async Task PreviewAction(string filename)
        {
            bitmapImage.StreamSource.Dispose();

            CharacterSelected.Sprites.Remove(emotion);
            if (File.Exists($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\{emotion.Value.String}"))
                File.Delete($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\{emotion.Value.String}");

            var characterPath = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Characters\\{CharacterSelected.Name}";
            var characterData = JsonConvert.SerializeObject(CharacterSelected, Formatting.Indented);
            await $"{characterPath}\\Character.json".WriteTextAsync(characterData);
            Extensions.Notify("Characters", $"Emotion Sprite {emotion.Key} deleted", NotificationType.None);
        };


        var window = new Window
        {
            Title = $"Preview: {emotion.Key}",
            SizeToContent = SizeToContent.Manual,
            Width = 640,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false
        };

        window.Content = CreatePreviewGrid(bitmapImage, emotion, PreviewAction, window);

        window.ShowDialog();
    }

    [RelayCommand]
    private async Task CharacterAudioAdd()
    {
        async Task ImportAction(string soundPath, string audio)
        {
            var characterPath = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Characters\\{CharacterSelected.Name}";
            var newSoundPath = $"{characterPath}\\Sounds\\{audio}{Path.GetExtension(soundPath)}";
            Directory.CreateDirectory($"{characterPath}\\Sounds");
            await soundPath.CopyStreamToAsync(newSoundPath);
            

            var audioData = new AudioData
            {
                Name = audio,
                Path = $"Characters\\{CharacterSelected.Name}\\Sounds\\{audio}{Path.GetExtension(soundPath)}",
                Audio = new()
            };

            CharacterSelected.Sounds.Add(audioData);
            var characterData = JsonConvert.SerializeObject(CharacterSelected, Formatting.Indented);
            await $"{characterPath}\\Character.json".WriteTextAsync(characterData);
        }

        await AddCharacter("Add Audio", ImportAction, "Sounds|*.mp3;*.wav;*.m4a");
    }

    [RelayCommand]
    private async Task CharacterAudioDelete(AudioData audio)
    {
        var importBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Characters",
            Content = new StackPanel { Width = 400, Children = { new Wpf.Ui.Controls.TextBlock { Text = "Delete Sound", FontWeight = FontWeights.Bold, FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 50) }, new System.Windows.Controls.TextBlock { Text = $"Audio {audio.Name} will get deleted", FontWeight = FontWeights.Bold, FontSize = 18, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5) } } },
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        var result = await importBox.ShowDialogAsync();

        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary)
            return;

        CharacterSelected.Sounds.Remove(audio);
        if (File.Exists($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\{audio.Path}"))
            File.Delete($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\{audio.Path}");

        var characterPath = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Characters\\{CharacterSelected.Name}";
        var characterData = JsonConvert.SerializeObject(CharacterSelected, Formatting.Indented);
        await $"{characterPath}\\Character.json".WriteTextAsync(characterData);
    }

    #endregion

    #region Scenes

    private static async Task AddScene(string title, Func<string, Task> importAction)
    {
        var sceneName = await ShowInputDialogAsync(title, "Scene name...");
        if (sceneName == null)
            return;

        await importAction(sceneName);
    }

    [RelayCommand]
    private static async Task ScenesAdd()
    {
        static async Task ImportAction(string sceneName)
        {
            var scenePath = Directory.CreateDirectory($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Scenes\\{sceneName}").FullName;

            var scene = new SceneData
            {
                Name = sceneName,
                ID = ProjectData.Current.Scenes.Count
            };

            var sceneData = JsonConvert.SerializeObject(scene, Formatting.Indented);
            await $"{scenePath}\\Scene.json".WriteTextAsync(sceneData);

            ProjectData.Current.Scenes.Add(scene);
            Extensions.Notify("Scenes", $"Scene {sceneName} created", NotificationType.None);
        }

        await AddScene("Create Scene", ImportAction);
    }

    [RelayCommand]
    private void ScenesDelete()
    {
        if (SceneSelected == null)
            return;

        var Name = SceneSelected.Name;
        ProjectData.Current.Scenes.Remove(SceneSelected);
        Directory.Delete($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Scenes\\{Name}", true);
        Extensions.Notify("Scenes", $"Scene {Name} deleted", NotificationType.None);
    }

    [RelayCommand]
    private async Task ScenesSpriteAdd()
    {
        async Task ImportAction(string backgroundPath, string backgroundName)
        {
            var scenePath = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Scenes\\{SceneSelected.Name}";
            var newBackgroundPath = $"{scenePath}\\Backgrounds\\{backgroundName}{Path.GetExtension(backgroundPath)}";
            Directory.CreateDirectory($"{scenePath}\\Backgrounds");
            await backgroundPath.CopyStreamToAsync(newBackgroundPath);

            SceneSelected.Backgrounds.Add(new KeyValue() { Key = backgroundName, Value = new() { String = $"Scenes\\{SceneSelected?.Name}\\Backgrounds\\{backgroundName}{Path.GetExtension(backgroundPath)}" } });

            var characterData = JsonConvert.SerializeObject(SceneSelected, Formatting.Indented);
            await $"{scenePath}\\Scene.json".WriteTextAsync(characterData);
        }

        await AddCharacter("Add Background", ImportAction);
    }

    [RelayCommand]
    private void ScenesSpritePreview(KeyValue sprite)
    {
        var bitmapImage = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current?.Name}\\{sprite.Value.String}".GetBitmapImage();

        async Task PreviewAction(string filename)
        {
            // Delete sprite logic
            SceneSelected?.Backgrounds.Remove(sprite);

            bitmapImage.StreamSource.Dispose();

            var scenePath = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Scenes\\{SceneSelected.Name}";
            var sceneData = JsonConvert.SerializeObject(SceneSelected, Formatting.Indented);
            await $"{scenePath}\\Scene.json".WriteTextAsync(sceneData);

            var filePath = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\{filename}";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Extensions.Notify("Scenes", $"Background Sprite {sprite.Key} deleted", NotificationType.None);
            }
        };

        var window = new Window
        {
            Title = $"Preview: {sprite.Key}",
            SizeToContent = SizeToContent.Manual,
            Width = 640,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false
        };

        window.Content = CreatePreviewGrid(bitmapImage, sprite, PreviewAction, window);

        window.ShowDialog();
    }

    [RelayCommand]
    private async Task ScenesAudioAdd()
    {
        async Task ImportAction(string audioPath, string audioName)
        {
            var scenePath = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Scenes\\{SceneSelected.Name}";
            var newAudioPath = $"{scenePath}\\Sounds\\{audioName}{Path.GetExtension(audioPath)}";
            Directory.CreateDirectory($"{scenePath}\\Sounds");
            await audioPath.CopyStreamToAsync(newAudioPath);

            var audioData = new AudioData
            {
                Name = audioName,
                Path = $"Scenes\\{SceneSelected.Name}\\Sounds\\{audioName}{Path.GetExtension(audioPath)}",
            };

            SceneSelected.Sounds.Add(audioData);

            var characterData = JsonConvert.SerializeObject(SceneSelected, Formatting.Indented);
            await $"{scenePath}\\Scene.json".WriteTextAsync(characterData);
        }

        await AddCharacter("Add Sound", ImportAction, "Sounds|*.mp3;*.wav;*.m4a");
    }

    [RelayCommand]
    private async Task ScenesAudioDelete(AudioData audio)
    {
        var importBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Scenes",
            Content = new StackPanel { Width = 400, Children = { new System.Windows.Controls.TextBlock { Text = "Delete Sound", FontWeight = FontWeights.Bold, FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 50) }, new System.Windows.Controls.TextBlock { Text = $"Audio {audio.Name} will get deleted", FontWeight = FontWeights.Bold, FontSize = 18, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(5) } } },
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        var result = await importBox.ShowDialogAsync();

        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary)
            return;

        SceneSelected?.Sounds.Remove(audio);
        if (File.Exists($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current?.Name}\\{audio.Path}"))
            File.Delete($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current?.Name}\\{audio.Path}");

        var characterPath = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current?.Name}\\Scenes\\{SceneSelected?.Name}";
        var characterData = JsonConvert.SerializeObject(SceneSelected, Formatting.Indented);
        await $"{characterPath}\\Scene.json".WriteTextAsync(characterData);
    }

    #endregion

    #region Blackboards
    [RelayCommand]
    private static async Task BlackboardsAdd()
    {
        static async Task ImportAction(string blackboardName)
        {
            var blackboardPath = Directory.CreateDirectory($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Blackboards\\{blackboardName}").FullName;

            var blackboard = new BlackboardData
            {
                Name = blackboardName
            };

            var blackboardData = JsonConvert.SerializeObject(blackboard, Formatting.Indented);
            await $"{blackboardPath}\\Blackboard.json".WriteTextAsync(blackboardData);

            ProjectData.Current.Blackboards.Add(blackboard);
            Extensions.Notify("Blackboards", $"Blackboard {blackboardName} created", NotificationType.None);
        }

        await AddScene("Create Blackboard", ImportAction);
    }

    [RelayCommand]
    private void BlackboardsDelete()
    {
        var Name = BlackboardSelected.Name;
        ProjectData.Current.Blackboards.Remove(BlackboardSelected);
        Directory.Delete($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\Blackboards\\{Name}", true);
        Extensions.Notify("Scenes", $"Blackboard {Name} deleted", NotificationType.None);
    }

    [RelayCommand]
    private void BlackboardInsert(object type)
    {
        switch ((string)type)
        {
            case "String":
                BlackboardSelected.Strings.Add(new() { Type = Enums.ValueType.String });
                break;
            case "Float":
                BlackboardSelected.Floats.Add(new() { Type = Enums.ValueType.Float });
                break;
            case "Boolean":
                BlackboardSelected.Booleans.Add(new() { Type = Enums.ValueType.Boolean });
                break;
        }
    }

    [RelayCommand]
    private void BlackboardRemove(object boardData)
    {
        var board = (KeyValue)boardData;
        switch (board.Type)
        {
            case Enums.ValueType.String:
                BlackboardSelected.Strings.Remove(board);
                break;
            case Enums.ValueType.Float:
                BlackboardSelected.Floats.Remove(board);
                break;
            case Enums.ValueType.Boolean:
                BlackboardSelected.Booleans.Remove(board);
                break;
        }
    }

    #endregion

    #region UI

    private static async Task<string> BrowseFileDialogAsync(string filter)
    {
        var openFileDialog = new System.Windows.Forms.OpenFileDialog { Filter = filter };
        return openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? openFileDialog.FileName : null;
    }

    private static async Task<string> ShowInputDialogAsync(string title, string placeholderText)
    {
        var inputBox = new Wpf.Ui.Controls.TextBox
        {
            Text = string.Empty,
            PlaceholderText = placeholderText,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var importBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = new StackPanel { Width = 400, Children = { new System.Windows.Controls.TextBlock { Text = title, FontWeight = FontWeights.Bold, FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 50) }, inputBox } },
            PrimaryButtonText = "Confirm",
            CloseButtonText = "Cancel",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        var result = await importBox.ShowDialogAsync();

        return result == Wpf.Ui.Controls.MessageBoxResult.Primary ? inputBox.Text : null;
    }

    private static async Task<string> ShowImagePickerDialogAsync(string title)
    {
        var avatar = new Image();

        var imagePathText = await BrowseFileDialogAsync("Images|*.jpg;*.jpeg;*.png;*.tiff;*.webp");

        if (imagePathText == null)
            return null;

        var bitmapImage = imagePathText.GetBitmapImage();

        avatar.Source = bitmapImage;
        avatar.Height = 300;
        avatar.Width = 250;

        var importBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = title,
            Content = new StackPanel { Width = 400, Children = { new TextBlock { Text = title, FontWeight = FontWeights.Bold, FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 50) }, avatar } },
            PrimaryButtonText = "Select",
            CloseButtonText = "Cancel",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        var result = await importBox.ShowDialogAsync();
        bitmapImage.StreamSource.Dispose();
        return result == Wpf.Ui.Controls.MessageBoxResult.Primary ? imagePathText : null;
    }

    private static Grid CreatePreviewGrid(BitmapImage bitmapImage, KeyValue sprite, Func<string, Task> previewAction, Window parent = null)
    {
        var image = new Wpf.Ui.Controls.Image { Source = bitmapImage, Stretch = System.Windows.Media.Stretch.Uniform, Margin = new(10) };
        var filenameTextBlock = new Wpf.Ui.Controls.TextBlock { Text = $"Filename: {sprite.Key}" };
        var widthTextBlock = new Wpf.Ui.Controls.TextBlock { Text = $"Width: {bitmapImage.PixelWidth}px" };
        var heightTextBlock = new Wpf.Ui.Controls.TextBlock { Text = $"Height: {bitmapImage.PixelHeight}px" };
        var dateTextBlock = new Wpf.Ui.Controls.TextBlock { Text = $"Date: {DateTime.Now:yyyy-MM-dd}" };

        var stackPanel = new StackPanel
        {
            Margin = new(10),
            Children = { filenameTextBlock, widthTextBlock, heightTextBlock, dateTextBlock }
        };

        var backButton = new Wpf.Ui.Controls.Button { Content = "Back", Margin = new Thickness(5) };
        backButton.Click += (sender, e) => 
        {
            parent?.Close();
        };

        var deleteButton = new Wpf.Ui.Controls.Button { Content = "Delete", Margin = new Thickness(5) };
        deleteButton.Click += async (sender, e) =>
        {
            await previewAction(sprite.Value.String);
            parent?.Close();
        };

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Children = { deleteButton, backButton } };

        var grid = new Grid
        {
            RowDefinitions = { new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, new RowDefinition { Height = GridLength.Auto } },
            Children = { image, stackPanel, buttonPanel }
        };

        Grid.SetRow(image, 0);
        Grid.SetRow(stackPanel, 0);
        Grid.SetRow(buttonPanel, 1);

        return grid;
    }

    [RelayCommand]
    private static void AudioPlay(AudioData audio)
    {
        var media = audio.Audio ??= audio.Audio = new();

        if (audio.State == AssetPlayState.Stopped)
        {
            audio.State = AssetPlayState.Playing;
            audio.AudioFile ??= audio.AudioFile = new AudioFileReader($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current?.Name}\\{audio.Path}");
            media.Init(audio.AudioFile);
            media.Play();
        }
        else
        {
            audio.State = AssetPlayState.Stopped;
            media.Stop();
            audio.Audio.Dispose();
            audio.AudioFile?.Dispose();
            audio.Audio = null;
            audio.AudioFile = null;
        }
    }

    [RelayCommand]
    public static void NavigateTo(string navTarget)
    {
        var mainWindow = App.GetService<MainWindow>();
        switch (navTarget)
        {
            case "Home":
                mainWindow.NavigationView.Navigate(typeof(HomePage));
                break;
            case "Settings":
                mainWindow.NavigationView.Navigate(typeof(SettingsPage));
                break;
        }
    }

    [RelayCommand]
    public static async Task SaveData()
    {
        var projectPath = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}";
        var wordsCount = 0;
        foreach (var character in ProjectData.Current.Characters)
        {
            var data = JsonConvert.SerializeObject(character, Formatting.Indented);
            await $"{projectPath}\\Characters\\{character.Name}\\Character.json".WriteTextAsync(data);
        }
        foreach (var scene in ProjectData.Current.Scenes)
        {
            wordsCount += scene.WordsCount;
            Extensions.Notify("Save", $"Words Count {wordsCount} per scene {scene.Name} ");

            var data = JsonConvert.SerializeObject(scene, Formatting.Indented);
            await $"{projectPath}\\Scenes\\{scene.Name}\\Scene.json".WriteTextAsync(data);
        }
        foreach (var blackboard in ProjectData.Current.Blackboards)
        {
            var data = JsonConvert.SerializeObject(blackboard, Formatting.Indented);
            await $"{projectPath}\\Blackboards\\{blackboard.Name}\\Blackboard.json".WriteTextAsync(data);
        }

        ProjectData.Current.WordsCount = $"{wordsCount} Words";
        var projectData = JsonConvert.SerializeObject(ProjectData.Current, Formatting.Indented);
        await $"{projectPath}\\ProjectData.json".WriteTextAsync(projectData);
    }

    [RelayCommand]
    private static async Task Export()
    {
        var exportType = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 20),
            Items = { "Package / Player", "Project / Unity" }
        };

        var exportPathTextBox = new Wpf.Ui.Controls.TextBox
        {
            Text = string.Empty,
            PlaceholderText = "Export path...",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var exportPathDialog = new Wpf.Ui.Controls.Button
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Common.SymbolRegular.DocumentAdd24 },
                    new Wpf.Ui.Controls.TextBlock { Text = "Browse" }
                }
            }
        };

        exportPathDialog.Click += (sender, e) =>
        {
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                exportPathTextBox.Text = folderBrowserDialog.SelectedPath;
        };

        var importBox = new Wpf.Ui.Controls.MessageBox
        {
            Title = "Projects",
            Content = new StackPanel { Width = 400, Children = { new TextBlock { Text = "Export Project", FontWeight = FontWeights.Bold, FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 50) }, exportType, exportPathTextBox, exportPathDialog } },
            PrimaryButtonText = "Export",
            CloseButtonText = "Cancel",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        var result = await importBox.ShowDialogAsync();

        if (result != Wpf.Ui.Controls.MessageBoxResult.Primary || string.IsNullOrEmpty(exportPathTextBox.Text))
            return;

        try
        {
            var memoryProgress = new Wpf.Ui.Controls.Snackbar(Extensions.SnackbarArea)
            {
                Title = "Exporting Project",
                Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Common.SymbolRegular.ArrowExportUp24 },
                Timeout = TimeSpan.FromDays(1),
                Content = new StackPanel
                {
                    Children =
                    {
                        new Wpf.Ui.Controls.TextBlock { Text = $"Project {ProjectData.Current.Name} is exporting now" },
                        new ProgressBar { Margin = new Thickness(5,15,5,5), IsIndeterminate = true }
                    }
                }
            };

            Extensions.SnackbarArea.AddToQue(memoryProgress);
            await GameResources.Instance.PackDirectory($"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}", $"{exportPathTextBox.Text}", exportType.SelectedIndex == 1);
            await Extensions.SnackbarArea.HideCurrent();
        }
        catch (Exception ex)
        {
            Extensions.Notify(new NotificationContent { Title = "Error", Message = $"Error exporting project: {ex.Message}, {ex.StackTrace}", Type = NotificationType.Error });
            return;
        }

        Extensions.Notify(new NotificationContent { Title = "Projects", Message = $"Project {ProjectData.Current.Name} has Exported", Type = NotificationType.None });
    }

    #endregion

    #region Statics
    private static HomeViewModel? instance;
    public static HomeViewModel Instance => instance ??= new HomeViewModel();
    #endregion
}
