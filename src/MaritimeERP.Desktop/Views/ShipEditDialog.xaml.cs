using System.Windows;

namespace MaritimeERP.Desktop.Views
{
    public partial class ShipEditDialog : Window
    {
        public ShipEditDialog()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 