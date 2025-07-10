using MaritimeERP.Desktop.ViewModels;
using System.Windows;

namespace MaritimeERP.Desktop.Views
{
    /// <summary>
    /// Interaction logic for SecurityReviewStatementDialog.xaml
    /// </summary>
    public partial class SecurityReviewStatementDialog : Window
    {
        public SecurityReviewStatementDialog(SecurityReviewStatementDialogViewModel viewModel)
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