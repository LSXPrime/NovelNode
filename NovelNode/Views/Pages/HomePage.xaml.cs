using System.Windows.Input;
using NovelNode.Helpers;
using NovelNode.Data;
using NovelNode.ViewModels.Pages;
using Wpf.Ui.Controls;
using System.Collections.ObjectModel;

namespace NovelNode.Views.Pages;
public partial class HomePage : INavigableView<HomeViewModel>
{
    public HomeViewModel ViewModel { get; }

    public HomePage(HomeViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        Extensions.NodeEditor = NodeEditor;

        InitializeComponent();
    }

    private void OnNodeDrop(object sender, DragEventArgs e)
    {
        if (e.Source is Nodify.NodifyEditor editor && e.Data.GetData(typeof(Node)) is Node node)
        {
            node.Location = editor.GetLocationInsideEditor(e);
            ViewModel.SceneSelected?.Nodes.Add(node);
            e.Handled = true;
        }
    }

    private void OnNodeDrag(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && ((FrameworkElement)sender).DataContext is Node node)
        {
            Node? newNode = null;
            switch (node)
            {
                case NodeState:
                    newNode = new NodeState();
                    break;
                case NodeBackground:
                    newNode = new NodeBackground() { Input = [new()], Output = [new() { Flow = Enums.NodeConnectorFlow.Output }] };
                    break;
                case NodeCharacter:
                    newNode = new NodeCharacter() { Input = [new()], Output = [new() { Flow = Enums.NodeConnectorFlow.Output }] };
                    break;
                case NodeDialogue:
                    newNode = new NodeDialogue() { Input = [new()], Output = [new() { Flow = Enums.NodeConnectorFlow.Output }] };
                    break;
                case NodeChoice:
                    newNode = new NodeChoice() { Input = [new()], Output = [] };
                    break;
                case NodeEvent:
                    newNode = new NodeEvent() { Input = [new()], Output = [new() { Flow = Enums.NodeConnectorFlow.Output }] };
                    break;
                case NodeCondition:
                    newNode = new NodeCondition() { Input = [new()], Output = [new() { Title = "Yes", Flow = Enums.NodeConnectorFlow.Output }, new() { Title = "No", Flow = Enums.NodeConnectorFlow.Output }] };
                    break;
                case NodeScene:
                    newNode = new NodeScene() { Input = [new()] };
                    break;
                case NodeCheckpoint:
                    newNode = new NodeCheckpoint() { Input = [new()], Output = [new() { Flow = Enums.NodeConnectorFlow.Output }] };
                    break;
                case NodeCombiner:
                    newNode = new NodeCombiner() { Input = [new(), new()], Output = [new() { Flow = Enums.NodeConnectorFlow.Output }] };
                    break;
            }

            var data = new DataObject(typeof(Node), newNode);
            DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
        }
    }
}
