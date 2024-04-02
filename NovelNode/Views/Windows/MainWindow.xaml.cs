using NovelNode.ViewModels.Windows;
using NovelNode.Helpers;
using Wpf.Ui.Controls;

namespace NovelNode.Views.Windows
{
    public partial class MainWindow
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService, IServiceProvider serviceProvider, ISnackbarService snackbarService, IContentDialogService contentDialogService)
        {
            Wpf.Ui.Appearance.Watcher.Watch(this);

            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            navigationService.SetNavigationControl(NavigationView);
            snackbarService.SetSnackbarPresenter(SnackbarArea);
            contentDialogService.SetContentPresenter(RootContentDialog);

            NavigationView.SetServiceProvider(serviceProvider);

            Extensions.SnackbarArea = SnackbarArea;
            Extensions.ContentArea = RootContentDialog;
        }
    }
}
