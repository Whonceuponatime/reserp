using System.Windows;

namespace MaritimeERP.Desktop.Views
{
    public partial class ShipDetailsDialog : Window
    {
        public ShipDetailsDialog()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 