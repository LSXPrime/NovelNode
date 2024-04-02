using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace NovelNode.Data;

public partial class ProjectData : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    [ObservableProperty]
    private string _author = string.Empty;
    [ObservableProperty]
    private string _wordsCount = string.Empty;


    private ObservableCollection<CharacterData> _characters = new();
    [JsonIgnore]
    public ObservableCollection<CharacterData> Characters
    {
        get => _characters;
        set
        {
            _characters = value;
            OnPropertyChanged(nameof(Characters));
        }
    }
    private ObservableCollection<SceneData> _scenes = new();
    [JsonIgnore]
    public ObservableCollection<SceneData> Scenes
    {
        get => _scenes;
        set
        {
            _scenes = value;
            OnPropertyChanged(nameof(Scenes));
        }
    }
    private ObservableCollection<BlackboardData> _blackboards = new();
    [JsonIgnore]
    public ObservableCollection<BlackboardData> Blackboards
    {
        get => _blackboards;
        set
        {
            _blackboards = value;
            OnPropertyChanged(nameof(Blackboards));
        }
    }


    [JsonIgnore]
    public static ProjectData? Current { get; set; }
}
