using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace NovelNode.Data;

public partial class CharacterData : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    [ObservableProperty]
    private string _avatar = string.Empty;
    [ObservableProperty]
    private ObservableCollection<KeyValue> _sprites = new();
    [ObservableProperty]
    private ObservableCollection<AudioData> _sounds = new();
    [ObservableProperty]
    private ObservableCollection<string> _tags = new();

    public static CharacterData FromJson(string json) => JsonConvert.DeserializeObject<CharacterData>(json);
    public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);
}

public partial class AudioData : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    [ObservableProperty]
    private string _path = string.Empty;
    [JsonIgnore, NonSerialized]
    public NAudio.Wave.WaveOut? Audio;
    [ObservableProperty]
    [property: JsonIgnore]
    private NAudio.Wave.AudioFileReader? _audioFile;
    [ObservableProperty]
    [property: JsonIgnore]
    private AssetPlayState _state = AssetPlayState.Stopped;
}

public enum AssetPlayState : short
{
    Stopped = 0,
    Playing = 1,
    Paused = 2
}