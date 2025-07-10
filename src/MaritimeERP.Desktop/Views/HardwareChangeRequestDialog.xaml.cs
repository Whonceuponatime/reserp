using System.Windows;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    /// <summary>
    /// Interaction logic for HardwareChangeRequestDialog.xaml
    /// </summary>
    public partial class HardwareChangeRequestDialog : Window
    {
        public HardwareChangeRequestDialog()
        {
            InitializeComponent();
        }

        public HardwareChangeRequestDialog(HardwareChangeRequestDialogViewModel viewModel) : this()
        {
            DataContext = viewModel;
            
            // Subscribe to close events
            if (viewModel != null)
            {
                viewModel.RequestClose += (sender, e) => Close();
                viewModel.RequestSave += (sender, e) => DialogResult = true;
            }
        }
    }
} 