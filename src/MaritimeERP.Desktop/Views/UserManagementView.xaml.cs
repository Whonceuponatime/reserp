using System.Windows.Controls;
using System.Windows;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    /// <summary>
    /// Interaction logic for UserManagementView.xaml
    /// </summary>
    public partial class UserManagementView : UserControl
    {
        private UserManagementViewModel? ViewModel => DataContext as UserManagementViewModel;

        public UserManagementView()
        {
            InitializeComponent();
            
            // Handle password field changes
            PasswordField.PasswordChanged += PasswordField_PasswordChanged;
            ConfirmPasswordField.PasswordChanged += ConfirmPasswordField_PasswordChanged;
            
            // Handle DataContext changes to sync password fields
            DataContextChanged += UserManagementView_DataContextChanged;
        }

        private void UserManagementView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                // Subscribe to property changes to clear password fields when needed
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UserManagementViewModel.IsEditing))
            {
                if (ViewModel != null && !ViewModel.IsEditing)
                {
                    // Clear password fields when editing is cancelled or completed
                    PasswordField.Clear();
                    ConfirmPasswordField.Clear();
                    HidePasswordValidationMessage();
                }
            }
            else if (e.PropertyName == nameof(UserManagementViewModel.SelectedUser))
            {
                // Clear password fields when user selection changes
                PasswordField.Clear();
                ConfirmPasswordField.Clear();
                HidePasswordValidationMessage();
            }
        }

        private void PasswordField_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.Password = PasswordField.Password;
                ValidatePasswords();
            }
        }

        private void ConfirmPasswordField_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ConfirmPassword = ConfirmPasswordField.Password;
                ValidatePasswords();
            }
        }

        private void ValidatePasswords()
        {
            if (ViewModel == null) return;

            var password = PasswordField.Password;
            var confirmPassword = ConfirmPasswordField.Password;

            // Only validate if both fields have content
            if (!string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(confirmPassword))
            {
                if (password != confirmPassword)
                {
                    ShowPasswordValidationMessage("Passwords do not match");
                    ViewModel.IsPasswordValid = false;
                    return;
                }
                
                // Check minimum length for new users
                if (ViewModel.SelectedUser == null && password.Length < 8)
                {
                    ShowPasswordValidationMessage("Password must be at least 8 characters long");
                    ViewModel.IsPasswordValid = false;
                    return;
                }
                
                // Check minimum length for existing users when password is provided
                if (ViewModel.SelectedUser != null && !string.IsNullOrEmpty(password) && password.Length < 8)
                {
                    ShowPasswordValidationMessage("Password must be at least 8 characters long");
                    ViewModel.IsPasswordValid = false;
                    return;
                }
            }

            HidePasswordValidationMessage();
            ViewModel.IsPasswordValid = true;
        }

        private void ShowPasswordValidationMessage(string message)
        {
            PasswordValidationMessage.Text = message;
            PasswordValidationMessage.Visibility = Visibility.Visible;
        }

        private void HidePasswordValidationMessage()
        {
            PasswordValidationMessage.Visibility = Visibility.Collapsed;
        }
    }
} 