using System.Windows.Controls;
using NovelNode.ViewModels.UserControls;

namespace NovelNode.Views.UserControls;

public partial class PlayerView : UserControl
{
    public PlayerViewViewModel ViewModel { get; }

    public PlayerView()
    {
        ViewModel = PlayerViewViewModel.Instance;
        DataContext = this;

        InitializeComponent();

        ViewModel.DialogueArea = DialogueArea;
        ViewModel.ChoicesArea = ChoicesArea;
    }
}
