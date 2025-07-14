using System.Windows.Controls;
using System.Windows.Input;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    public partial class SoftwareView : UserControl
    {
        private SoftwareViewModel? _viewModel;

        // Parameterless constructor for DataTemplate usage
        public SoftwareView()
        {
            InitializeComponent();
        }

        // Constructor with dependency injection
        public SoftwareView(SoftwareViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Get the view model from DataContext if not injected
            var viewModel = _viewModel ?? DataContext as SoftwareViewModel;
            if (viewModel?.SelectedSoftware?.InstalledComponent != null)
            {
                viewModel.NavigateToComponentCommand.Execute(null);
            }
        }
    }
} 