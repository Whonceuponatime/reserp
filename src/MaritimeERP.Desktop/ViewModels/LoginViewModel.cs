using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MaritimeERP.Services.Interfaces;
using MaritimeERP.Desktop.Commands;

namespace MaritimeERP.Desktop.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthenticationService _authenticationService;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _isLoading = false;
        private string _errorMessage = string.Empty;
        private bool _rememberMe = false;

        public LoginViewModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
            LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
            ExitCommand = new RelayCommand(Exit);
        }

        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                ((AsyncRelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                ((AsyncRelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set 
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand ExitCommand { get; }

        public event EventHandler<LoginSuccessEventArgs>? LoginSuccess;

        private bool CanLogin()
        {
            return !IsLoading && 
                   !string.IsNullOrWhiteSpace(Username) && 
                   !string.IsNullOrWhiteSpace(Password);
        }

        private async Task LoginAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var user = await _authenticationService.AuthenticateAsync(Username, Password);
                
                if (user != null)
                {
                    LoginSuccess?.Invoke(this, new LoginSuccessEventArgs(user));
                }
                else
                {
                    ErrorMessage = "Invalid username or password. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred during login: {ex.Message}";
                // Log the exception if you have logging configured
                System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
            }
            finally
            {
                IsLoading = false;
                Password = string.Empty; // Clear password for security
            }
        }

        private void Exit()
        {
            Application.Current.Shutdown();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class LoginSuccessEventArgs : EventArgs
    {
        public LoginSuccessEventArgs(MaritimeERP.Core.Entities.User user)
        {
            User = user;
        }

        public MaritimeERP.Core.Entities.User User { get; }
    }
} 