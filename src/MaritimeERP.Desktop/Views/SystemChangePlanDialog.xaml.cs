using MaritimeERP.Desktop.ViewModels;
using System.Windows;

namespace MaritimeERP.Desktop.Views
{
    public partial class SystemChangePlanDialog : Window
    {
        public SystemChangePlanDialog(SystemChangePlanDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            viewModel.RequestClose += () => Close();
        }
    }
} 