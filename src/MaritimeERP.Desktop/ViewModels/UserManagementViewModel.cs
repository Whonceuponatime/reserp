using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace MaritimeERP.Desktop.ViewModels
{
    public class UserManagementViewModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<UserManagementViewModel> _logger;

        // Collections
        public ObservableCollection<User> Users { get; } = new();
        public ObservableCollection<Role> Roles { get; } = new();

        // Selected items
        private User? _selectedUser;
        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    OnSelectedUserChanged();
                }
            }
        }

        // Form properties for new/edit user
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _confirmPassword = string.Empty;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        private bool _isPasswordValid = true;
        public bool IsPasswordValid
        {
            get => _isPasswordValid;
            set => SetProperty(ref _isPasswordValid, value);
        }

        private Role? _selectedRole;
        public Role? SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        // UI State
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Statistics
        private int _totalUsers;
        public int TotalUsers
        {
            get => _totalUsers;
            set => SetProperty(ref _totalUsers, value);
        }

        private int _activeUsers;
        public int ActiveUsers
        {
            get => _activeUsers;
            set => SetProperty(ref _activeUsers, value);
        }

        private int _inactiveUsers;
        public int InactiveUsers
        {
            get => _inactiveUsers;
            set => SetProperty(ref _inactiveUsers, value);
        }

        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ActivateUserCommand { get; }
        public ICommand DeactivateUserCommand { get; }
        public ICommand ResetPasswordCommand { get; }

        public UserManagementViewModel(
            IUserService userService,
            IAuthenticationService authenticationService,
            ILogger<UserManagementViewModel> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize commands
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            AddUserCommand = new RelayCommand(AddUser, () => !IsLoading && !IsEditing);
            EditUserCommand = new RelayCommand(EditUser, () => SelectedUser != null && !IsLoading && !IsEditing);
            SaveUserCommand = new RelayCommand(async () => await SaveUserAsync(), () => CanSaveUser());
            CancelEditCommand = new RelayCommand(CancelEdit, () => IsEditing);
            DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync(), () => SelectedUser != null && !IsLoading && !IsEditing);
            ActivateUserCommand = new RelayCommand(async () => await ActivateUserAsync(), () => SelectedUser != null && !SelectedUser.IsActive && !IsLoading);
            DeactivateUserCommand = new RelayCommand(async () => await DeactivateUserAsync(), () => SelectedUser != null && SelectedUser.IsActive && !IsLoading);
            ResetPasswordCommand = new RelayCommand(async () => await ResetPasswordAsync(), () => SelectedUser != null && !IsLoading);

            // Load initial data
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "Loading users...";
                });

                // Load users and roles in parallel
                var usersTask = _userService.GetAllUsersAsync();
                var rolesTask = _userService.GetAllRolesAsync();

                await Task.WhenAll(usersTask, rolesTask);

                var users = await usersTask;
                var roles = await rolesTask;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Update collections
                    Users.Clear();
                    foreach (var user in users)
                    {
                        Users.Add(user);
                    }

                    Roles.Clear();
                    foreach (var role in roles)
                    {
                        Roles.Add(role);
                    }

                    // Update statistics
                    TotalUsers = users.Count;
                    ActiveUsers = users.Count(u => u.IsActive);
                    InactiveUsers = users.Count(u => !u.IsActive);

                    StatusMessage = $"Loaded {users.Count} users successfully";
                });

                _logger.LogInformation("User management data loaded successfully");
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error loading user data";
                });
                _logger.LogError(ex, "Error loading user management data");
                MessageBox.Show($"Error loading user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private void OnSelectedUserChanged()
        {
            if (SelectedUser != null && !IsEditing)
            {
                PopulateFormFromSelectedUser();
            }
            RefreshCommands();
        }

        private void PopulateFormFromSelectedUser()
        {
            if (SelectedUser == null) return;

            Username = SelectedUser.Username;
            FullName = SelectedUser.FullName;
            Email = SelectedUser.Email;
            IsActive = SelectedUser.IsActive;
            SelectedRole = Roles.FirstOrDefault(r => r.Id == SelectedUser.RoleId);
            
            // Clear password fields when editing
            Password = string.Empty;
            ConfirmPassword = string.Empty;
        }

        private void AddUser()
        {
            SelectedUser = null;
            ClearForm();
            IsEditing = true;
        }

        private void EditUser()
        {
            if (SelectedUser != null)
            {
                PopulateFormFromSelectedUser();
                IsEditing = true;
            }
        }

        private async Task SaveUserAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "Saving user...";
                });

                if (SelectedUser == null)
                {
                    // Create new user
                    var newUser = new User
                    {
                        Username = Username,
                        FullName = FullName,
                        Email = Email,
                        RoleId = SelectedRole?.Id ?? 0,
                        IsActive = IsActive
                    };

                    var createdUser = await _userService.CreateUserAsync(newUser, Password);
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Users.Add(createdUser);
                        TotalUsers++;
                        if (createdUser.IsActive) ActiveUsers++;
                        else InactiveUsers++;
                        StatusMessage = "User created successfully";
                    });
                }
                else
                {
                    // Update existing user
                    SelectedUser.Username = Username;
                    SelectedUser.FullName = FullName;
                    SelectedUser.Email = Email;
                    SelectedUser.RoleId = SelectedRole?.Id ?? 0;
                    SelectedUser.IsActive = IsActive;

                    var updatedUser = await _userService.UpdateUserAsync(SelectedUser);
                    
                    // If password was provided, reset the password
                    if (!string.IsNullOrWhiteSpace(Password) && Password.Length >= 8)
                    {
                        var passwordSuccess = await _userService.ResetPasswordAsync(SelectedUser.Id, Password);
                        if (!passwordSuccess)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                StatusMessage = "User updated but password change failed";
                            });
                            MessageBox.Show("User was updated successfully, but password change failed.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // Update the user in the collection
                        var index = Users.ToList().FindIndex(u => u.Id == updatedUser.Id);
                        if (index >= 0)
                        {
                            Users[index] = updatedUser;
                            SelectedUser = updatedUser;
                        }
                        
                        // Update statistics
                        ActiveUsers = Users.Count(u => u.IsActive);
                        InactiveUsers = Users.Count(u => !u.IsActive);
                        
                        var message = "User updated successfully";
                        if (!string.IsNullOrWhiteSpace(Password))
                        {
                            message += " with new password";
                        }
                        StatusMessage = message;
                    });
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsEditing = false;
                    ClearForm();
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error saving user";
                });
                _logger.LogError(ex, "Error saving user");
                MessageBox.Show($"Error saving user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            ClearForm();
            if (SelectedUser != null)
            {
                PopulateFormFromSelectedUser();
            }
        }

        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete user '{SelectedUser.FullName}'?\n\nNote: This will deactivate the user account.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        IsLoading = true;
                        StatusMessage = "Deleting user...";
                    });

                    var success = await _userService.DeleteUserAsync(SelectedUser.Id);
                    
                    if (success)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            SelectedUser.IsActive = false;
                            ActiveUsers = Users.Count(u => u.IsActive);
                            InactiveUsers = Users.Count(u => !u.IsActive);
                            StatusMessage = "User deleted successfully";
                        });
                    }
                    else
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            StatusMessage = "Failed to delete user";
                        });
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = "Error deleting user";
                    });
                    _logger.LogError(ex, "Error deleting user");
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        IsLoading = false;
                    });
                }
            }
        }

        private async Task ActivateUserAsync()
        {
            if (SelectedUser == null) return;

            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "Activating user...";
                });

                var success = await _userService.ActivateUserAsync(SelectedUser.Id);
                
                if (success)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SelectedUser.IsActive = true;
                        ActiveUsers = Users.Count(u => u.IsActive);
                        InactiveUsers = Users.Count(u => !u.IsActive);
                        StatusMessage = "User activated successfully";
                    });
                }
                else
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = "Failed to activate user";
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error activating user";
                });
                _logger.LogError(ex, "Error activating user");
                MessageBox.Show($"Error activating user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private async Task DeactivateUserAsync()
        {
            if (SelectedUser == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to deactivate user '{SelectedUser.FullName}'?",
                "Confirm Deactivate",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        IsLoading = true;
                        StatusMessage = "Deactivating user...";
                    });

                    var success = await _userService.DeactivateUserAsync(SelectedUser.Id);
                    
                    if (success)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            SelectedUser.IsActive = false;
                            ActiveUsers = Users.Count(u => u.IsActive);
                            InactiveUsers = Users.Count(u => !u.IsActive);
                            StatusMessage = "User deactivated successfully";
                        });
                    }
                    else
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            StatusMessage = "Failed to deactivate user";
                        });
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = "Error deactivating user";
                    });
                    _logger.LogError(ex, "Error deactivating user");
                    MessageBox.Show($"Error deactivating user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        IsLoading = false;
                    });
                }
            }
        }

        private async Task ResetPasswordAsync()
        {
            if (SelectedUser == null) return;

            // Simple input dialog for new password
            var newPassword = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new password (minimum 8 characters):",
                "Reset Password",
                "");

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        IsLoading = true;
                        StatusMessage = "Resetting password...";
                    });

                    var success = await _userService.ResetPasswordAsync(SelectedUser.Id, newPassword);
                    
                    if (success)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            StatusMessage = "Password reset successfully";
                        });
                        MessageBox.Show($"Password reset successfully for {SelectedUser.FullName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            StatusMessage = "Failed to reset password";
                        });
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = "Error resetting password";
                    });
                    _logger.LogError(ex, "Error resetting password");
                    MessageBox.Show($"Error resetting password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        IsLoading = false;
                    });
                }
            }
        }

        private void ClearForm()
        {
            Username = string.Empty;
            FullName = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            SelectedRole = null;
            IsActive = true;
        }

        private bool CanSaveUser()
        {
            if (!IsEditing || IsLoading || !IsPasswordValid) return false;

            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(FullName) ||
                string.IsNullOrWhiteSpace(Email) ||
                SelectedRole == null)
                return false;

            // For new users, password is required
            if (SelectedUser == null)
            {
                return !string.IsNullOrWhiteSpace(Password) &&
                       Password.Length >= 8 &&
                       Password == ConfirmPassword;
            }

            // For existing users, password is optional
            if (!string.IsNullOrWhiteSpace(Password))
            {
                return Password.Length >= 8 && Password == ConfirmPassword;
            }

            return true;
        }

        private void RefreshCommands()
        {
            ((RelayCommand)AddUserCommand).RaiseCanExecuteChanged();
            ((RelayCommand)EditUserCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SaveUserCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelEditCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteUserCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ActivateUserCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeactivateUserCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ResetPasswordCommand).RaiseCanExecuteChanged();
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
            RefreshCommands();
            return true;
        }
    }
} 