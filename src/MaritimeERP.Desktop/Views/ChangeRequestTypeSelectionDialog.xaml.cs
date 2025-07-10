using System.Windows;

namespace MaritimeERP.Desktop.Views
{
    /// <summary>
    /// Interaction logic for ChangeRequestTypeSelectionDialog.xaml
    /// </summary>
    public partial class ChangeRequestTypeSelectionDialog : Window
    {
        public int SelectedTypeId { get; private set; } = 0;

        public ChangeRequestTypeSelectionDialog()
        {
            InitializeComponent();
        }

        // Management of Change handlers
        private void HardwareButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTypeId = 1; // Hardware Change
            DialogResult = true;
        }

        private void SoftwareButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTypeId = 2; // Software Change
            DialogResult = true;
        }

        private void SystemPlanButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTypeId = 3; // System Plan
            DialogResult = true;
        }

        private void SecurityReviewButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTypeId = 4; // Security Review Statement
            DialogResult = true;
        }

        // Management of Firewalls handlers
        private void FirewallButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Firewall Policy Change Request module will be available in future updates.", 
                           "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Management of Malware Protection handlers
        private void MalwareButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Malware Protection Change Request module will be available in future updates.", 
                           "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Management of Access Control handlers
        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Account Create/Remove Request module will be available in future updates.", 
                           "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AccessControlButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Access Control Change Request module will be available in future updates.", 
                           "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChecklistButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Access Control Checklist module will be available in future updates.", 
                           "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
} 