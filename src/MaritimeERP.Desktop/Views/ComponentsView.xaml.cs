using System.Windows.Controls;
using System.Windows.Input;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    /// <summary>
    /// Interaction logic for ComponentsView.xaml
    /// </summary>
    public partial class ComponentsView : UserControl
    {
        public ComponentsView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem != null)
            {
                if (DataContext is ComponentsViewModel viewModel && viewModel.NavigateToSoftwareCommand.CanExecute(null))
                {
                    viewModel.NavigateToSoftwareCommand.Execute(null);
                }
            }
        }
    }
} 