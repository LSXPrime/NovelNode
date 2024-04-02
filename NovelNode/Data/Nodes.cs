using System.Collections.ObjectModel;
using NovelNode.ViewModels.Pages;
using NovelNode.Enums;
using Newtonsoft.Json;
using NetFabric.Hyperlinq;
using JsonSubTypes;
using NovelNode.Helpers;

namespace NovelNode.Data;

public partial class NodeConnectionPending : ObservableObject
{
    [ObservableProperty]
    private NodeConnector? _source;

    [RelayCommand]
    public void Start(NodeConnector source) => Source = source;

    [RelayCommand]
    public void Finish(NodeConnector target)
    {
        if (target != null && target.Flow != Source.Flow)
            App.GetService<HomeViewModel>().NodeConnect(Source, target);
    }
}

public partial class NodeConnection : ObservableObject
{
    [ObservableProperty]
    private Guid _sourceID;

    private NodeConnector? _source;
    [JsonIgnore]
    public NodeConnector Source
    {
        get
        {
            _source ??= HomeViewModel.Instance.SceneSelected?.Nodes
                .AsValueEnumerable()
                .Where(node => node.Output != null)
                .SelectMany(node => node.Output)
                .FirstOrDefault(connector => connector.ID == SourceID);

            return _source;
        }
    }
    [ObservableProperty]
    private Guid _targetID;

    private NodeConnector? _target;
    [JsonIgnore]
    public NodeConnector Target
    {
        get
        {
            _target ??= HomeViewModel.Instance.SceneSelected?.Nodes
                .AsValueEnumerable()
                .Where(node => node.Input != null)
                .SelectMany(node =>  node.Input)
                .FirstOrDefault(connector => connector.ID == TargetID);

            return _target;
        }
    }

    public NodeConnection(NodeConnector? source, NodeConnector? target)
    {
        if (source == null || target == null)
            return;

        _sourceID = source.ID;
        _targetID = target.ID;
        _source = source;
        _target = target;
        Source.IsConnected = Target.IsConnected = true;
    }

    [field: JsonIgnore]
    public void Swap()
    {
        (TargetID, SourceID) = (SourceID, TargetID);
        OnPropertyChanged(nameof(Source));
        OnPropertyChanged(nameof(Target));
    }
}

public partial class NodeConnector : ObservableObject
{
    public Guid ID = Guid.NewGuid();
    [ObservableProperty]
    private string _title = string.Empty;
    [ObservableProperty]
    private Point _anchor;
    [ObservableProperty]
    private bool _isConnected;
    [ObservableProperty]
    private NodeConnectorFlow _flow = NodeConnectorFlow.Input;
}

[JsonConverter(typeof(JsonSubtypes), "Type")]
[JsonSubtypes.KnownSubType(typeof(NodeState), "State")]
[JsonSubtypes.KnownSubType(typeof(NodeBackground), "Background")]
[JsonSubtypes.KnownSubType(typeof(NodeCharacter), "Character")]
[JsonSubtypes.KnownSubType(typeof(NodeDialogue), "Dialogue")]
[JsonSubtypes.KnownSubType(typeof(NodeChoice), "Choice")]
[JsonSubtypes.KnownSubType(typeof(NodeEvent), "Event")]
[JsonSubtypes.KnownSubType(typeof(NodeCondition), "Condition")]
[JsonSubtypes.KnownSubType(typeof(NodeScene), "Scene")]
[JsonSubtypes.KnownSubType(typeof(NodeCheckpoint), "Checkpoint")]
[JsonSubtypes.KnownSubType(typeof(NodeCombiner), "Combiner")]
public partial class Node : ObservableObject
{
    public virtual string Type { get; } = "Base";
    [ObservableProperty]
    private string _tooltip = "Basic purpose node, can be used as dummy or for building another nodes.";
    [ObservableProperty]
    private Guid _guid = Guid.NewGuid();
    [ObservableProperty]
    private Guid _previous;
    [ObservableProperty]
    private Guid _next;
    [ObservableProperty]
    private Point _location;
    [ObservableProperty]
    private ObservableCollection<NodeConnector>? _input;
    [ObservableProperty]
    private ObservableCollection<NodeConnector>? _output;
}

public partial class NodeState : Node
{
    public override string Type { get; } = "State";
    [ObservableProperty]
    private string _tooltip = "This node should represent a scene's state. \nwhether is the scene's state point is enter or exit.";
    private NodeSwitch _state = NodeSwitch.Enter;
    public NodeSwitch State
    {
        get => _state;
        set
        {
            if (value == NodeSwitch.Enter ? Input?.Any(x => x.IsConnected) == true : Output?.Any(x => x.IsConnected) == true)
                return;

            _state = value;
            Input = Output = null;

            Input = value == NodeSwitch.Enter ? null : [new()];
            Output = value == NodeSwitch.Enter ? [new() { Flow = Enums.NodeConnectorFlow.Output }] : null;

            // When project load,Input/Output somehow have additional empty entry, so this a workaround
            Input?.RemoveWhere(x => x.IsConnected == false);
            Output?.RemoveWhere(x => x.IsConnected == false);

            OnPropertyChanged(nameof(State));
        }
    }
}

public partial class NodeBackground : Node
{
    public override string Type { get; } = "Background";
    [ObservableProperty]
    private string _tooltip = "This node should represent a scene's background. \nincluding fields for the state (enter or exit), the fade color and fade time.";
    [ObservableProperty]
    private AssetType _assetType = AssetType.Sprite;
    [ObservableProperty]
    private string _background = string.Empty;
    [ObservableProperty]
    private double _fadeTime = 1;
    [ObservableProperty]
    private bool _loop = false;
    [ObservableProperty]
    private NodeSwitch _state = NodeSwitch.Enter;
}

public partial class NodeCharacter : Node
{
    public override string Type { get; } = "Character";
    [ObservableProperty]
    private string _tooltip = "This node should represent a character on-screen. \nincluding fields for the state (enter or exit), the current sprite, position on screen view by percent and fade time.";
    [ObservableProperty]
    private string _character = string.Empty;
    private CharacterData? _characterData;
    [JsonIgnore]
    public CharacterData CharacterData
    {
        get => _characterData ??= ProjectData.Current.Characters.FirstOrDefault(x => x.Name == Character);
        set
        {
            _characterData = value;
            Character = value?.Name ?? string.Empty;
            OnPropertyChanged(nameof(CharacterData));
        }
    }
    [ObservableProperty]
    private string _sprite = string.Empty;
    [ObservableProperty]
    private int _position = 50;
    [ObservableProperty]
    private double _fadeTime = 0.25;
    [ObservableProperty]
    private NodeSwitch _state = NodeSwitch.Enter;
}

public partial class NodeDialogue : Node
{
    public override string Type { get; } = "Dialogue";
    [ObservableProperty]
    private string _tooltip = "This node should represent a character's dialogue. \nincluding fields for the character speaking, the actual dialogue text and emotion.";
    [ObservableProperty]
    private string _character = string.Empty;
    private CharacterData? _characterData;
    [JsonIgnore]
    public CharacterData CharacterData
    {
        get => _characterData ??= ProjectData.Current.Characters.FirstOrDefault(x => x.Name == Character);
        set
        {
            _characterData = value;
            Character = value?.Name ?? string.Empty;
            OnPropertyChanged(nameof(CharacterData));
        }
    }

    private bool _isPlayerDialogue;
    public bool IsPlayerDialogue
    {
        get => _isPlayerDialogue;
        set
        {
            _isPlayerDialogue = value;
            if (value == true)
                Character = "Player";
            OnPropertyChanged(nameof(IsPlayerDialogue));
        }
    }

    [ObservableProperty]
    private ObservableCollection<NodeDialogData> _lines = [];

    [RelayCommand]
    [property: JsonIgnore]
    private void AddDialogueLine()
    {
        Lines.Add(new());
    }
    [RelayCommand]
    [property: JsonIgnore]
    private void RemoveDialogueLine(object line)
    {
        Lines.Remove((NodeDialogData)line);
    }

    public partial class NodeDialogData : ObservableObject
    {
        [ObservableProperty]
        private string _text = string.Empty;
        [ObservableProperty]
        private string _characterSprite = string.Empty;
        [ObservableProperty]
        private string _characterAudio = string.Empty;
    }
}

public partial class NodeChoice : Node
{
    public override string Type { get; } = "Choice";
    [ObservableProperty]
    private string _tooltip = "Node that represent choices the player can make, for branching dialogues. \nInclude fields for the text of the choice and the next dialogue node linked to each choice output.";
    [ObservableProperty]
    private ObservableCollection<NodeChoiceData> _choices = [];

    [RelayCommand]
    [field: JsonIgnore]
    public void AddChoice()
    {
        Output?.Add(new() { Flow = NodeConnectorFlow.Output });
        Choices.Add(new() { Output = Output.Count - 1 });
    }
    [RelayCommand]
    [field: JsonIgnore]
    public void RemoveChoice(object choice)
    {
        if (choice is not NodeChoiceData choiceData)
            return;

        var index = Choices.IndexOf(choiceData);
        Output.RemoveAt(index);
        var choices = Choices.Skip(index);
        foreach (var choice2 in choices)
            choice2.Output -= 1;

        Choices.Remove(choiceData);
    }

    public partial class NodeChoiceData : ObservableObject
    {
        [ObservableProperty]
        private string _text = string.Empty;
        [ObservableProperty]
        private Guid _next;
        [ObservableProperty]
        private int _output;
    }
}

public partial class NodeEvent : Node
{
    [JsonProperty(Order = 1)]
    public override string Type { get; } = "Event";

    [ObservableProperty]
    private string _tooltip = "Node that represent events or triggers in the story to check using Condition Node. \nThis could include changes in the game state, unlocking new dialogues, or triggering special scenes.";

    [ObservableProperty]
    [property: JsonProperty(Order = 2)]
    private string _blackboardName = string.Empty;

    [ObservableProperty]
    [property: JsonProperty(Order = 3)]
    private string _blackboardKey = string.Empty;

    private Enums.ValueType _blackboardType;
    [property: JsonProperty(Order = 4)]
    public Enums.ValueType BlackboardType
    {
        get => _blackboardType;
        set
        {
            _blackboardType = value;
            var blackboard = ProjectData.Current?.Blackboards.FirstOrDefault(x => x.Name == BlackboardName);

            switch (_blackboardType)
            {
                case Enums.ValueType.String:
                    BlackboardData = blackboard.Strings;
                    break;
                case Enums.ValueType.Float:
                    BlackboardData = blackboard.Floats;
                    break;
                case Enums.ValueType.Boolean:
                    BlackboardData = blackboard.Booleans;
                    break;
            }

            OnPropertyChanged(nameof(BlackboardType));
        }
    }

    [ObservableProperty]
    private EventTask _operator;
    [ObservableProperty]
    private string _newValue = string.Empty;

    private ObservableCollection<KeyValue>? _blackboardData;
    [JsonIgnore]
    public ObservableCollection<KeyValue> BlackboardData
    {
        get => _blackboardData;
        set
        {
            _blackboardData = value;
            OnPropertyChanged(nameof(BlackboardData));
        }
    }
}

public partial class NodeCondition : Node
{
    [JsonProperty(Order = 1)]
    public override string Type { get; } = "Condition";
    [ObservableProperty]
    private string _tooltip = "This node represents a condition based on Blackboard data.";
    [ObservableProperty]
    [property: JsonProperty(Order = 2)]
    private string _blackboardName = string.Empty;
    [ObservableProperty]
    [property: JsonProperty(Order = 3)]
    private string _blackboardKey = string.Empty;

    private Enums.ValueType _blackboardType;
    [property: JsonProperty(Order = 4)]
    public Enums.ValueType BlackboardType
    {
        get => _blackboardType;
        set
        {
            _blackboardType = value;
            var blackboard = ProjectData.Current?.Blackboards.FirstOrDefault(x => x.Name == BlackboardName);
            switch (_blackboardType)
            {
                case Enums.ValueType.String:
                    BlackboardData = blackboard.Strings;
                    break;
                case Enums.ValueType.Float:
                    BlackboardData = blackboard.Floats;
                    break;
                case Enums.ValueType.Boolean:
                    BlackboardData = blackboard.Booleans;
                    break;
            }

            OnPropertyChanged(nameof(BlackboardType));
        }
    }
    [ObservableProperty]
    private ComparisonOperator _operator;
    [ObservableProperty]
    private string _compareValue = string.Empty;

    private ObservableCollection<KeyValue>? _blackboardData;
    [JsonIgnore]
    public ObservableCollection<KeyValue> BlackboardData
    {
        get => _blackboardData;
        set
        {
            _blackboardData = value;
            OnPropertyChanged(nameof(BlackboardData));
        }
    }
}

public partial class NodeScene : Node
{
    public override string Type { get; } = "Scene";

    [ObservableProperty]
    private string _tooltip = "This node represents switching scenes in the visual novel.";
    [ObservableProperty]
    private string _sceneName = string.Empty;
    [ObservableProperty]
    private double _fadeTime = 1;
}

public partial class NodeCheckpoint : Node
{
    public override string Type { get; } = "Checkpoint";
    [ObservableProperty]
    private string _tooltip = "Node that represents saving progress or resetting to a previous checkpoint.";
    [ObservableProperty]
    private CheckpointAction _action;
    [ObservableProperty]
    private int _checkpointID;
    [ObservableProperty]
    private int _targetCheckpointID;
}

public partial class NodeCombiner : Node
{
    public override string Type { get; } = "Combiner";
    [ObservableProperty]
    private string _tooltip = "This node should represent a nodes combiner. \nwhere you can combine multiply paths nodes into one.";

    [RelayCommand]
    [property: JsonIgnore]
    private void AddInput()
    {
        Input?.Add(new());
    }
    [RelayCommand]
    [property: JsonIgnore]
    private void RemoveInput()
    {
        Input?.Remove(Input.LastOrDefault());
    }
}