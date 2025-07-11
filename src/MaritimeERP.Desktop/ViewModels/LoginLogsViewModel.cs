using System.Collections.ObjectModel;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Services.Interfaces;
using MaritimeERP.Desktop.Commands;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace MaritimeERP.Desktop.ViewModels
{
    public class LoginLogsViewModel : ViewModelBase
    {
        private readonly ILoginLogService _loginLogService;
        private readonly ILogger<LoginLogsViewModel> _logger;

        public LoginLogsViewModel(ILoginLogService loginLogService, ILogger<LoginLogsViewModel> logger)
        {
            _loginLogService = loginLogService;
            _logger = logger;

            LoginLogs = new ObservableCollection<LoginLog>();
            FilteredLoginLogs = new ObservableCollection<LoginLog>();

            // Initialize filter options
            Actions = new ObservableCollection<string> { "All Actions", "LOGIN", "LOGOUT", "LOGIN_FAILED", "PASSWORD_RESET", "PASSWORD_CHANGED", "ACCOUNT_LOCKED", "ACCOUNT_UNLOCKED" };
            SuccessOptions = new ObservableCollection<string> { "All", "Success", "Failed" };
            SecurityEventOptions = new ObservableCollection<string> { "All", "Security Events", "Normal Events" };

            // Set default filters
            SelectedAction = "All Actions";
            SelectedSuccessOption = "All";
            SelectedSecurityOption = "All";
            StartDate = DateTime.Now.AddDays(-30); // Last 30 days
            EndDate = DateTime.Now;

            // Commands
            LoadLogsCommand = new RelayCommand(async () => await LoadLogsAsync());
            FilterLogsCommand = new RelayCommand(async () => await FilterLogsAsync());
            ClearFiltersCommand = new RelayCommand(() => ClearFilters());
            ExportLogsCommand = new RelayCommand(async () => await ExportLogsAsync());
            DeleteLogCommand = new RelayCommand<LoginLog>(async (log) => await DeleteLogAsync(log));

            // Initialize
            Task.Run(async () => await InitializeAsync());
        }

        #region Properties

        public ObservableCollection<LoginLog> LoginLogs { get; }
        public ObservableCollection<LoginLog> FilteredLoginLogs { get; }
        public ObservableCollection<string> Actions { get; }
        public ObservableCollection<string> SuccessOptions { get; }
        public ObservableCollection<string> SecurityEventOptions { get; }

        private LoginLog? _selectedLoginLog;
        public LoginLog? SelectedLoginLog
        {
            get => _selectedLoginLog;
            set
            {
                _selectedLoginLog = value;
                OnPropertyChanged();
            }
        }

        private string _selectedAction = "All Actions";
        public string SelectedAction
        {
            get => _selectedAction;
            set
            {
                _selectedAction = value;
                OnPropertyChanged();
            }
        }

        private string _selectedSuccessOption = "All";
        public string SelectedSuccessOption
        {
            get => _selectedSuccessOption;
            set
            {
                _selectedSuccessOption = value;
                OnPropertyChanged();
            }
        }

        private string _selectedSecurityOption = "All";
        public string SelectedSecurityOption
        {
            get => _selectedSecurityOption;
            set
            {
                _selectedSecurityOption = value;
                OnPropertyChanged();
            }
        }

        private string _usernameFilter = string.Empty;
        public string UsernameFilter
        {
            get => _usernameFilter;
            set
            {
                _usernameFilter = value;
                OnPropertyChanged();
            }
        }

        private DateTime _startDate = DateTime.Now.AddDays(-30);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        // Statistics
        private int _totalLogins;
        public int TotalLogins
        {
            get => _totalLogins;
            set
            {
                _totalLogins = value;
                OnPropertyChanged();
            }
        }

        private int _successfulLogins;
        public int SuccessfulLogins
        {
            get => _successfulLogins;
            set
            {
                _successfulLogins = value;
                OnPropertyChanged();
            }
        }

        private int _failedLogins;
        public int FailedLogins
        {
            get => _failedLogins;
            set
            {
                _failedLogins = value;
                OnPropertyChanged();
            }
        }

        private int _securityEvents;
        public int SecurityEvents
        {
            get => _securityEvents;
            set
            {
                _securityEvents = value;
                OnPropertyChanged();
            }
        }

        private double _successRate;
        public double SuccessRate
        {
            get => _successRate;
            set
            {
                _successRate = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand LoadLogsCommand { get; }
        public ICommand FilterLogsCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ExportLogsCommand { get; }
        public ICommand DeleteLogCommand { get; }

        #endregion

        #region Methods

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading login logs...";

                await LoadLogsAsync();
                await LoadStatisticsAsync();

                StatusMessage = "Login logs loaded successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing login logs view");
                StatusMessage = "Error loading login logs";
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error loading login logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadLogsAsync()
        {
            try
            {
                var logs = await _loginLogService.GetAllLoginLogsAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    LoginLogs.Clear();
                    foreach (var log in logs)
                    {
                        LoginLogs.Add(log);
                    }
                });

                await FilterLogsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading login logs");
                throw;
            }
        }

        private async Task FilterLogsAsync()
        {
            try
            {
                string? action = SelectedAction == "All Actions" ? null : SelectedAction;
                bool? isSuccessful = SelectedSuccessOption switch
                {
                    "Success" => true,
                    "Failed" => false,
                    _ => null
                };
                bool? isSecurityEvent = SelectedSecurityOption switch
                {
                    "Security Events" => true,
                    "Normal Events" => false,
                    _ => null
                };

                var username = string.IsNullOrWhiteSpace(UsernameFilter) ? null : UsernameFilter.Trim();

                var filteredLogs = await _loginLogService.GetFilteredLoginLogsAsync(
                    username, action, isSuccessful, isSecurityEvent, StartDate, EndDate);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredLoginLogs.Clear();
                    foreach (var log in filteredLogs)
                    {
                        FilteredLoginLogs.Add(log);
                    }
                });

                StatusMessage = $"Showing {filteredLogs.Count} login logs";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering login logs");
                StatusMessage = "Error filtering login logs";
            }
        }

        private void ClearFilters()
        {
            SelectedAction = "All Actions";
            SelectedSuccessOption = "All";
            SelectedSecurityOption = "All";
            UsernameFilter = string.Empty;
            StartDate = DateTime.Now.AddDays(-30);
            EndDate = DateTime.Now;
            
            Task.Run(async () => await FilterLogsAsync());
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                TotalLogins = await _loginLogService.GetTotalLoginAttemptsAsync(30);
                SuccessfulLogins = await _loginLogService.GetSuccessfulLoginsAsync(30);
                FailedLogins = TotalLogins - SuccessfulLogins;
                
                var securityEventsList = await _loginLogService.GetSecurityEventsAsync(30);
                SecurityEvents = securityEventsList.Count;
                
                SuccessRate = await _loginLogService.GetLoginSuccessRateAsync(30);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading login statistics");
            }
        }

        private async Task ExportLogsAsync()
        {
            try
            {
                StatusMessage = "Exporting login logs...";

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Login Logs",
                    Filter = "CSV Files|*.csv|All Files|*.*",
                    FileName = $"LoginLogs_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await ExportToCsvAsync(saveDialog.FileName);
                    StatusMessage = "Login logs exported successfully";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting login logs");
                StatusMessage = "Error exporting login logs";
                MessageBox.Show($"Error exporting login logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportToCsvAsync(string filePath)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Timestamp,Username,Action,Success,IP Address,User Agent,Failure Reason,Security Event");

            foreach (var log in FilteredLoginLogs)
            {
                csv.AppendLine($"\"{log.TimestampDisplay}\",\"{log.Username}\",\"{log.Action}\"," +
                              $"\"{log.StatusDisplay}\",\"{log.IpAddress ?? ""}\",\"{log.UserAgent ?? ""}\"," +
                              $"\"{log.FailureReason ?? ""}\",\"{log.SecurityDisplay}\"");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
        }

        private async Task DeleteLogAsync(LoginLog log)
        {
            try
            {
                if (log == null) return;

                var result = MessageBox.Show($"Are you sure you want to delete this login log entry?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _loginLogService.DeleteLoginLogAsync(log.Id);
                    await LoadLogsAsync();
                    await LoadStatisticsAsync();
                    StatusMessage = "Login log deleted successfully";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting login log");
                StatusMessage = "Error deleting login log";
                MessageBox.Show($"Error deleting login log: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
} 