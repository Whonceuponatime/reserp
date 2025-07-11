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
    public class AuditLogsViewModel : INotifyPropertyChanged
    {
        private readonly IAuditLogService _auditLogService;
        private readonly IUserService _userService;
        private readonly ILogger<AuditLogsViewModel> _logger;

        // Collections
        public ObservableCollection<AuditLog> AuditLogs { get; } = new();
        public ObservableCollection<AuditLog> FilteredAuditLogs { get; } = new();
        public ObservableCollection<string> EntityTypes { get; } = new();
        public ObservableCollection<string> Actions { get; } = new();
        public ObservableCollection<User> Users { get; } = new();

        // Selected items
        private AuditLog? _selectedAuditLog;
        public AuditLog? SelectedAuditLog
        {
            get => _selectedAuditLog;
            set => SetProperty(ref _selectedAuditLog, value);
        }

        // Filter properties
        private string? _selectedEntityType;
        public string? SelectedEntityType
        {
            get => _selectedEntityType;
            set
            {
                if (SetProperty(ref _selectedEntityType, value) && _isInitialized)
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        private string? _selectedAction;
        public string? SelectedAction
        {
            get => _selectedAction;
            set
            {
                if (SetProperty(ref _selectedAction, value) && _isInitialized)
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        private User? _selectedUser;
        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value) && _isInitialized)
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value) && _isInitialized)
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value) && _isInitialized)
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        // UI State
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isInitialized = false;

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Statistics
        private int _totalLogs;
        public int TotalLogs
        {
            get => _totalLogs;
            set => SetProperty(ref _totalLogs, value);
        }

        private int _filteredLogCount;
        public int FilteredLogCount
        {
            get => _filteredLogCount;
            set => SetProperty(ref _filteredLogCount, value);
        }

        private int _todaysLogs;
        public int TodaysLogs
        {
            get => _todaysLogs;
            set => SetProperty(ref _todaysLogs, value);
        }

        // Log type filter presets
        public ObservableCollection<LogTypeFilter> LogTypeFilters { get; } = new();

        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ExportLogsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ApplyLogTypeFilterCommand { get; }

        public AuditLogsViewModel(
            IAuditLogService auditLogService,
            IUserService userService,
            ILogger<AuditLogsViewModel> logger)
        {
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize commands
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExportLogsCommand = new RelayCommand(async () => await ExportLogsAsync(), () => !IsLoading);
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            ApplyLogTypeFilterCommand = new RelayCommand<LogTypeFilter>(ApplyLogTypeFilter);

            // Initialize filter presets
            InitializeLogTypeFilters();

            // Load initial data
            _ = LoadDataAsync();
        }

        private void InitializeLogTypeFilters()
        {
            LogTypeFilters.Clear();
            
            LogTypeFilters.Add(new LogTypeFilter 
            { 
                Name = "All Logs", 
                Description = "Show all audit logs",
                EntityType = null,
                Action = null,
                Icon = "📋"
            });
            
            LogTypeFilters.Add(new LogTypeFilter 
            { 
                Name = "Hardware Changes", 
                Description = "Hardware change requests and modifications",
                EntityType = "HardwareChangeRequest",
                Action = null,
                Icon = "🔧"
            });
            
            LogTypeFilters.Add(new LogTypeFilter 
            { 
                Name = "Software Changes", 
                Description = "Software change requests and updates",
                EntityType = "SoftwareChangeRequest",
                Action = null,
                Icon = "💾"
            });
            
            LogTypeFilters.Add(new LogTypeFilter 
            { 
                Name = "System Plans", 
                Description = "System change plans and planning",
                EntityType = "SystemChangePlan",
                Action = null,
                Icon = "🏗️"
            });
            
            LogTypeFilters.Add(new LogTypeFilter 
            { 
                Name = "Security Reviews", 
                Description = "Security review statements",
                EntityType = "SecurityReviewStatement",
                Action = null,
                Icon = "🛡️"
            });
            
            LogTypeFilters.Add(new LogTypeFilter 
            { 
                Name = "User Management", 
                Description = "User account changes and activities",
                EntityType = "User",
                Action = null,
                Icon = "👥"
            });
            
            LogTypeFilters.Add(new LogTypeFilter 
            { 
                Name = "Approvals", 
                Description = "Approval and rejection activities",
                EntityType = null,
                Action = "APPROVE",
                Icon = "✅"
            });
            
            LogTypeFilters.Add(new LogTypeFilter 
            { 
                Name = "Rejections", 
                Description = "Rejection activities",
                EntityType = null,
                Action = "REJECT",
                Icon = "❌"
            });
            
            LogTypeFilters.Add(new LogTypeFilter 
            { 
                Name = "Submissions", 
                Description = "Form submissions and changes",
                EntityType = null,
                Action = "SUBMIT",
                Icon = "📤"
            });
            
            LogTypeFilters.Add(new LogTypeFilter 
            { 
                Name = "Today's Activity", 
                Description = "All activities from today",
                EntityType = null,
                Action = null,
                Icon = "📅",
                IsDateFilter = true
            });
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "Loading audit logs...";
                });

                // Load all data in parallel
                var auditLogsTask = _auditLogService.GetAllAuditLogsAsync();
                var entityTypesTask = _auditLogService.GetDistinctEntityTypesAsync();
                var actionsTask = _auditLogService.GetDistinctActionsAsync();
                var usersTask = _userService.GetAllUsersAsync();

                await Task.WhenAll(auditLogsTask, entityTypesTask, actionsTask, usersTask);

                var auditLogs = await auditLogsTask;
                var entityTypes = await entityTypesTask;
                var actions = await actionsTask;
                var users = await usersTask;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Update collections
                    AuditLogs.Clear();
                    FilteredAuditLogs.Clear();
                    EntityTypes.Clear();
                    Actions.Clear();
                    Users.Clear();

                    // Add "All" options
                    EntityTypes.Add("All");
                    Actions.Add("All");
                    Users.Add(new User { Id = 0, FullName = "All Users", Username = "all" });

                    // Add actual data
                    foreach (var log in auditLogs)
                    {
                        AuditLogs.Add(log);
                        FilteredAuditLogs.Add(log);
                    }

                    foreach (var entityType in entityTypes)
                    {
                        EntityTypes.Add(entityType);
                    }

                    foreach (var action in actions)
                    {
                        Actions.Add(action);
                    }

                    foreach (var user in users)
                    {
                        Users.Add(user);
                    }

                    // Set default filter values to "All" to show all logs by default
                    SelectedEntityType = "All";
                    SelectedAction = "All";
                    SelectedUser = Users.FirstOrDefault(u => u.Id == 0); // "All Users"

                    // Update statistics
                    TotalLogs = auditLogs.Count;
                    FilteredLogCount = auditLogs.Count;
                    TodaysLogs = auditLogs.Count(l => l.Timestamp.Date == DateTime.Today);

                    StatusMessage = $"Loaded {auditLogs.Count} audit logs successfully";
                    
                    // Mark as initialized to enable filter change handling
                    _isInitialized = true;
                });

                _logger.LogInformation("Audit logs data loaded successfully");
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error loading audit logs data";
                });
                _logger.LogError(ex, "Error loading audit logs data");
                MessageBox.Show($"Error loading audit logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private async Task ApplyFiltersAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "Applying filters...";
                });

                var entityType = SelectedEntityType == "All" ? null : SelectedEntityType;
                var action = SelectedAction == "All" ? null : SelectedAction;
                var userId = SelectedUser?.Id == 0 ? null : SelectedUser?.Id;

                var filteredLogs = await _auditLogService.GetFilteredAuditLogsAsync(
                    entityType, action, userId, StartDate, EndDate);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FilteredAuditLogs.Clear();
                    foreach (var log in filteredLogs)
                    {
                        FilteredAuditLogs.Add(log);
                    }

                    FilteredLogCount = filteredLogs.Count;
                    StatusMessage = $"Showing {filteredLogs.Count} filtered audit logs";
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error applying filters";
                });
                _logger.LogError(ex, "Error applying filters");
                MessageBox.Show($"Error applying filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private void ClearFilters()
        {
            SelectedEntityType = "All";
            SelectedAction = "All";
            SelectedUser = Users.FirstOrDefault(u => u.Id == 0);
            StartDate = null;
            EndDate = null;
        }

        private void ApplyLogTypeFilter(LogTypeFilter? filter)
        {
            if (filter == null) return;

            if (filter.IsDateFilter && filter.Name == "Today's Activity")
            {
                StartDate = DateTime.Today;
                EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                SelectedEntityType = "All";
                SelectedAction = "All";
            }
            else
            {
                SelectedEntityType = filter.EntityType ?? "All";
                SelectedAction = filter.Action ?? "All";
                StartDate = null;
                EndDate = null;
            }
        }

        private async Task ExportLogsAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "Exporting audit logs...";
                });

                // Simple export logic - could be enhanced to save to CSV/Excel
                var logs = FilteredAuditLogs.ToList();
                var exportData = string.Join("\n", logs.Select(log => 
                    $"{log.TimestampDisplay}\t{log.EntityType}\t{log.ActionDisplay}\t{log.UserDisplay}\t{log.EntityDisplay}"));

                // For now, just copy to clipboard
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Clipboard.SetText(exportData);
                    StatusMessage = $"Exported {logs.Count} logs to clipboard";
                });

                MessageBox.Show($"Exported {logs.Count} audit logs to clipboard", "Export Complete", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error exporting audit logs";
                });
                _logger.LogError(ex, "Error exporting audit logs");
                MessageBox.Show($"Error exporting audit logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
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

    public class LogTypeFilter
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? Action { get; set; }
        public string Icon { get; set; } = "📋";
        public bool IsDateFilter { get; set; } = false;
    }
} 