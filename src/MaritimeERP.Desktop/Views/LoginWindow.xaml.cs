using System.Windows;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // Handle password field manually since PasswordBox doesn't support binding
            PasswordField.PasswordChanged += (s, e) =>
            {
                _viewModel.Password = PasswordField.Password;
            };
        }
    }
} 