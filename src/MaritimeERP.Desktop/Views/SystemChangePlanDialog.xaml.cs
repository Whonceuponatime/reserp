using MaritimeERP.Desktop.ViewModels;
using System.Windows;

namespace MaritimeERP.Desktop.Views
{
    /// <summary>
    /// Interaction logic for SystemChangePlanDialog.xaml
    /// </summary>
    public partial class SystemChangePlanDialog : Window
    {
        public SystemChangePlanDialog(SystemChangePlanDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // Subscribe to the RequestClose event
            viewModel.RequestClose += () =>
            {
                DialogResult = true;
                Close();
            };
        }
    }
} 