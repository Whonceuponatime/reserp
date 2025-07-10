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
            
            // Subscribe to dialog events
            if (viewModel != null)
            {
                viewModel.RequestSave += (sender, e) => this.DialogResult = true;
                viewModel.RequestClose += (sender, e) => this.Close();
            }
        }
    }
} 