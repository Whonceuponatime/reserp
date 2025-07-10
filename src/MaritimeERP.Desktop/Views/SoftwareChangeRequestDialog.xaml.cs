using System.Windows;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    public partial class SoftwareChangeRequestDialog : Window
    {
        public SoftwareChangeRequestDialog()
        {
            InitializeComponent();
        }
        
        public SoftwareChangeRequestDialog(SoftwareChangeRequestDialogViewModel viewModel) : this()
        {
            DataContext = viewModel;
            
            // Subscribe to dialog close events
            if (viewModel != null)
            {
                viewModel.RequestClose += (sender, e) => this.Close();
            }
        }
    }
} 