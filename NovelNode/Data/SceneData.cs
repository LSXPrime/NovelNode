using System.Collections.ObjectModel;

namespace NovelNode.Data;

public partial class SceneData : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    [ObservableProperty]
    private int _iD = 0;
    [ObservableProperty]
    private ObservableCollection<Node> _nodes = new();
    [ObservableProperty]
    private ObservableCollection<NodeConnection> _connections = new();
    [ObservableProperty]
    private ObservableCollection<KeyValue> _backgrounds = new();
    [ObservableProperty]
    private ObservableCollection<AudioData> _sounds = new();
    public int WordsCount
    {
        get
        {
            int count = 0;
            foreach (var node in Nodes)
            {
                if (node is NodeDialogue dialogue)
                    foreach (var line in dialogue.Lines)
                        count += line.Text.Split(" ").Length;

                if (node is NodeChoice choice)
                    foreach (var line in choice.Choices)
                        count += line.Text.Split(" ").Length;
            }

            return count;
        }
    }
}
