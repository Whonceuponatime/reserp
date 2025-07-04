using System.Windows;

namespace MaritimeERP.Desktop.Views
{
    /// <summary>
    /// Interaction logic for SystemEditDialog.xaml
    /// </summary>
    public partial class SystemEditDialog : Window
    {
        public SystemEditDialog()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 