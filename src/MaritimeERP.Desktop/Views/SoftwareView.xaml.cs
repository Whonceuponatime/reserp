using System.Windows.Controls;
using System.Windows.Input;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    public partial class SoftwareView : UserControl
    {
        private readonly SoftwareViewModel _viewModel;

        public SoftwareView(SoftwareViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedSoftware?.InstalledComponent != null)
            {
                _viewModel.NavigateToComponentCommand.Execute(null);
            }
        }
    }
} 