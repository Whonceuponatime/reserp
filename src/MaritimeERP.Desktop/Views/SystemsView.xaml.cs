using System.Windows.Controls;
using System.Windows.Input;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    public partial class SystemsView : UserControl
    {
        public SystemsView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem != null)
            {
                if (DataContext is SystemsViewModel viewModel && viewModel.NavigateToComponentsCommand.CanExecute(null))
                {
                    viewModel.NavigateToComponentsCommand.Execute(null);
                }
            }
        }
    }
} 