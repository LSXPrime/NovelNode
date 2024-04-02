using NovelNode.Data;
using NovelNode.Enums;
using NovelNode.Helpers;
using NetFabric.Hyperlinq;
using System.Collections.ObjectModel;
using NovelNode.ViewModels.Pages;
using System.Windows.Controls;
using System.Windows.Input;

namespace NovelNode.ViewModels.UserControls;

public partial class PlayerViewViewModel : ObservableObject
{
    [ObservableProperty]
    public AssetPlayState _state = AssetPlayState.Stopped;
    public ObservableCollection<Node> Nodes => HomeViewModel.Instance.SceneSelected.Nodes;
    public ObservableCollection<NodeConnection> Connections => HomeViewModel.Instance.SceneSelected.Connections;

    [ObservableProperty]
    private string _background = string.Empty;

    [ObservableProperty]
    private string _dialogue = string.Empty;

    [ObservableProperty]
    private string _narrator = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PlayerViewCharacterData> _characters = new();

    [ObservableProperty]
    private ObservableCollection<NodeChoice.NodeChoiceData> _choices = new();

    public UIElement DialogueArea { get; set; }
    public ListView ChoicesArea { get; set; }

    private List<Node> parsedNodes = [];
    private Dictionary<Guid, Node>? nodesById { get; set; }
    private IEnumerable<IGrouping<Guid, NodeConnection>>? connectionsBySource { get; set; }
    private ObservableCollection<BlackboardData>? blackboards;

    [RelayCommand]
    public void Execute()
    {
        switch (State)
        {
            case AssetPlayState.Stopped:
                blackboards = ProjectData.Current.Blackboards.Copy();
                connectionsBySource = Connections.GroupBy(c => c.SourceID);
                nodesById = Nodes.AsValueEnumerable()
                     .Where(node => node.Input != null)
                     .SelectMany(node => node.Input.Select(input => (input.ID, node)))
                     .ToDictionary(pair => pair.ID, pair => pair.node);
                var startNode = Nodes.FirstOrDefault(x => x is NodeState node && node.State == NodeSwitch.Enter);
                if (startNode != null)
                {
                    nodesById.Add(startNode.Guid, startNode);
                    parsedNodes.Add(startNode);
                    _ = TraverseConnections(startNode);
                }
                State = AssetPlayState.Playing;
                break;
            case AssetPlayState.Playing:
                Choices = [];
                Characters.Clear();
                parsedNodes.Clear();
                blackboards = null;
                nodesById = null;
                connectionsBySource = null;
                Background = Dialogue = Narrator = string.Empty;
                State = AssetPlayState.Stopped;
                break;
        }
    }

    private async Task TraverseConnections(Node nodeId, bool overrideNode = false)
    {
        var targetNode = overrideNode ? nodeId : nodesById.GetValueOrDefault(connectionsBySource.FirstOrDefault(group => nodeId.Output.Any(x => x.ID == group.Key)).FirstOrDefault().TargetID);

        if (targetNode != null && !parsedNodes.Contains(targetNode))
        {
            parsedNodes.Add(targetNode);
            switch (targetNode)
            {
                case NodeState node:
                    if (node.State == NodeSwitch.Exit)
                    {
                        Execute();
                        return;
                    }
                    break;
                case NodeBackground node:
                    if (node.AssetType == AssetType.Sprite)
                    {
                        await Task.Delay((int)node.FadeTime * 1000);
                        Background = $"{AppConfig.Instance.ProjectsPath}//{ProjectData.Current.Name}//{HomeViewModel.Instance.SceneSelected.Backgrounds.FirstOrDefault(x => x.Key == node.Background).Value.String}";
                    }
                    break;
                case NodeCharacter node:
                    switch (node.State)
                    {
                        case NodeSwitch.Enter:
                            var charPath = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\{node.CharacterData.Sprites.FirstOrDefault(x => x.Key == node.Sprite).Value.String}";
                            var charImage = charPath.GetBitmapImage();
                                
                            double x = ((node.Position - 50) / 50.0) * SystemParameters.PrimaryScreenWidth;
                            double y = (SystemParameters.PrimaryScreenHeight - charImage.PixelHeight) / 2;
                            var data = new PlayerViewCharacterData
                            {
                                SpriteBG = charPath,
                                CharacterData = node.CharacterData,
                                Margin = new Thickness(x, Math.Abs(y), 0, 0)
                            };

                            Characters.Add(data);
                            break;
                        case NodeSwitch.Exit:
                            var character = Characters.FirstOrDefault(x => x.CharacterData.Name == node.Character);
                            if (character != null)
                                Characters.Remove(character);
                            break;
                    }

                    break;
                case NodeDialogue node:
                    Narrator = node.IsPlayerDialogue ? "Player" : node.Character;
                    var characterData = Characters.FirstOrDefault(x => x.CharacterData.Name == node.Character);
                    foreach (var line in node.Lines)
                    {

                        Dialogue = string.Empty;
                        var dialogueLine = line.Text;
                        if (!node.IsPlayerDialogue)
                        {
                            dialogueLine = dialogueLine.Replace("{Player}", "Player-kun");
                            characterData.SpriteBG = $"{AppConfig.Instance.ProjectsPath}\\{ProjectData.Current.Name}\\{node.CharacterData.Sprites.FirstOrDefault(x => x.Key == line.CharacterSprite).Value.String}";
                        }
                        var dialogueChars = dialogueLine.ToCharArray();

                        var cts = new CancellationTokenSource();
                        var tcsD = new TaskCompletionSource<bool>();
                        void handlerD(object sender, MouseButtonEventArgs e)
                        {
                            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
                            {
                                cts.Cancel();
                                ((Border)sender).MouseDown -= handlerD;
                                tcsD.TrySetResult(true);
                            }
                        }

                        DialogueArea.MouseDown += handlerD;

                        try
                        {
                            foreach (var character in dialogueChars)
                            {
                                Dialogue += character;

                                DialogueArea.MouseDown += handlerD;
                                await Task.Delay(125, cts.Token);
                                DialogueArea.MouseDown -= handlerD;
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            Dialogue = dialogueLine;
                            tcsD = new();
                        }


                        DialogueArea.MouseDown += handlerD;
                        await tcsD.Task;
                        DialogueArea.MouseDown -= handlerD;
                    }
                    break;
                case NodeChoice node:
                    ChoicesArea.Visibility = Visibility.Visible;
                    Choices = node.Choices;

                    var tcs = new TaskCompletionSource<object>();
                    SelectionChangedEventHandler handler = null;
                    handler = (sender, e) =>
                    {
                        var listView = (ListView)sender;
                        if (listView.SelectedItem != null)
                        {
                            tcs.TrySetResult(null);
                            listView.SelectionChanged -= handler;
                        }
                    };
                    ChoicesArea.SelectionChanged += handler;
                    await tcs.Task;
                    ChoicesArea.Visibility = Visibility.Collapsed;

                    var choice = (NodeChoice.NodeChoiceData)ChoicesArea.SelectedItem;
                    var target = node.Output[choice.Output];
                    var connection = connectionsBySource?.FirstOrDefault(x => x.Key == target.ID);
                    var choiceOutput = nodesById?.GetValueOrDefault(connection.First().TargetID);
                    _ = TraverseConnections(choiceOutput, true);
                    return;
                case NodeEvent node:
                    var board = blackboards.AsValueEnumerable().FirstOrDefault(x => x.Name == node.BlackboardName).All.AsValueEnumerable().FirstOrDefault(x => x.Type == node.BlackboardType && x.Key == node.BlackboardKey);
                    if (board == null)
                        break;

                    switch (node.BlackboardType)
                    {
                        case Enums.ValueType.Float:
                            if (node.Operator == EventTask.Set)
                                board.Value.Float = float.Parse(node.NewValue);
                            else
                                board.Value.Float += node.Operator == EventTask.Increase ? float.Parse(node.NewValue) : -float.Parse(node.NewValue);
                            break;
                        case Enums.ValueType.String:
                            board.Value.String = node.NewValue;
                            break;
                        case Enums.ValueType.Boolean:
                            board.Value.Boolean = bool.Parse(node.NewValue);
                            break;
                    }
                    break;
                case NodeCondition node:
                    var targetBoard = blackboards.AsValueEnumerable().FirstOrDefault(x => x.Name == node.BlackboardName).All.AsValueEnumerable().FirstOrDefault(x => x.Type == node.BlackboardType && x.Key == node.BlackboardKey);
                    if (targetBoard == null)
                        break;

                    var requirementsMet = false;
                    switch (node.BlackboardType)
                    {
                        case Enums.ValueType.Float:
                            var floatValue = float.Parse(node.CompareValue);
                            requirementsMet = node.Operator switch
                            {
                                ComparisonOperator.EqualTo => targetBoard.Value.Float == floatValue,
                                ComparisonOperator.NotEqualTo => targetBoard.Value.Float != floatValue,
                                ComparisonOperator.GreaterThan => targetBoard.Value.Float > floatValue,
                                ComparisonOperator.LessThan => targetBoard.Value.Float < floatValue,
                                ComparisonOperator.GreaterThanOrEqualTo => targetBoard.Value.Float >= floatValue,
                                ComparisonOperator.LessThanOrEqualTo => targetBoard.Value.Float <= floatValue,
                                _ => false
                            };
                            break;
                        case Enums.ValueType.String:
                            requirementsMet = node.Operator switch
                            {
                                ComparisonOperator.EqualTo => targetBoard.Value.String.Equals(node.CompareValue),
                                _ => !targetBoard.Value.String.Equals(node.CompareValue),
                            };
                            break;
                        case Enums.ValueType.Boolean:
                            requirementsMet = node.Operator switch
                            {
                                ComparisonOperator.EqualTo => targetBoard.Value.Boolean == bool.Parse(node.CompareValue),
                                _ => targetBoard.Value.Boolean != bool.Parse(node.CompareValue),
                            };
                            break;
                    }

                    var reqNode = node.Output[requirementsMet ? 0 : 1];
                    var reqConnection = connectionsBySource?.FirstOrDefault(x => x.Key == reqNode.ID);
                    var reqTargetNode = nodesById?.GetValueOrDefault(reqConnection.First().TargetID);
                    _ = TraverseConnections(reqTargetNode, true);
                    return;
                case NodeScene node:
                    Execute();
                    HomeViewModel.Instance.SceneSelected = ProjectData.Current.Scenes.FirstOrDefault(x => x.Name == node.SceneName);
                    State = AssetPlayState.Stopped;
                    Execute();
                    return;
            }
            _ = TraverseConnections(targetNode);
        }
    }

    #region Statics
    private static PlayerViewViewModel? instance;
    public static PlayerViewViewModel Instance => instance ??= new PlayerViewViewModel();
    #endregion
}


public partial class PlayerViewCharacterData : ObservableObject
{
    [ObservableProperty]
    private string _spriteBG = string.Empty;
    [ObservableProperty]
    private CharacterData? _characterData;
    [ObservableProperty]
    private Thickness _margin = new();
}