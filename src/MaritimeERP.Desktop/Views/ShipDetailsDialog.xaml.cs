using System.Windows;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    /// <summary>
    /// Interaction logic for ShipDetailsDialog.xaml
    /// </summary>
    public partial class ShipDetailsDialog : Window
    {
        public ShipDetailsDialog()
        {
            InitializeComponent();
        }

        public ShipDetailsDialog(ShipDetailsViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 