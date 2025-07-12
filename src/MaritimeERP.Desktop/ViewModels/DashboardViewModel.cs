using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Core.Interfaces;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Component = MaritimeERP.Core.Entities.Component;

namespace MaritimeERP.Desktop.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IShipService _shipService;
        private readonly ISystemService _systemService;
        private readonly IComponentService _componentService;
        private readonly IDocumentService _documentService;
        private readonly IChangeRequestService _changeRequestService;
        private readonly IAuthenticationService _authenticationService;
        private readonly INavigationService _navigationService;
        private readonly IDataChangeNotificationService _dataChangeNotificationService;
        private readonly ILogger<DashboardViewModel> _logger;
        private readonly System.Timers.Timer _refreshTimer;

        // Statistics Properties
        private int _totalShips = 0;
        private int _activeShips = 0;
        private int _totalSystems = 0;
        private int _totalComponents = 0;
        private decimal _totalTonnage = 0;
        private int _recentChanges = 0;
        private string _mostCommonShipType = "N/A";
        private string _newestShip = "N/A";
        private bool _isLoading = false;

        // Admin-specific properties
        private int _pendingDocuments = 0;
        private int _pendingChangeRequests = 0;
        private int _activeAlerts = 0;
        private bool _isAdmin = false;

        // System Status properties
        private int _activeSystems = 0;
        private int _systemHealth = 95; // Placeholder value

        // Recent Activity
        private ObservableCollection<DashboardActivity> _recentActivities = new();
        private ObservableCollection<Ship> _recentShips = new();
        private ObservableCollection<ShipSystem> _recentSystems = new();
        private ObservableCollection<Component> _recentComponents = new();

        // Chart Data
        private ObservableCollection<ShipTypeStatistic> _shipTypeStatistics = new();
        private ObservableCollection<FleetStatistic> _fleetStatistics = new();

        public DashboardViewModel(
            IShipService shipService, 
            ISystemService systemService, 
            IComponentService componentService,
            IDocumentService documentService,
            IChangeRequestService changeRequestService,
            IAuthenticationService authenticationService,
            INavigationService navigationService,
            IDataChangeNotificationService dataChangeNotificationService,
            ILogger<DashboardViewModel> logger)
        {
            _shipService = shipService;
            _systemService = systemService;
            _componentService = componentService;
            _documentService = documentService;
            _changeRequestService = changeRequestService;
            _authenticationService = authenticationService;
            _navigationService = navigationService;
            _dataChangeNotificationService = dataChangeNotificationService;
            _logger = logger;
            _refreshTimer = new System.Timers.Timer();

            // Check if current user is admin
            _isAdmin = _authenticationService.CurrentUser?.Role?.Name == "Administrator";

            InitializeCommands();
            SubscribeToDataChanges();
            InitializeRefreshTimer();
            
            // Load dashboard data asynchronously without blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadDashboardDataAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during initial dashboard data load");
                }
            });
        }

        // Properties
        public int TotalShips
        {
            get => _totalShips;
            set => SetProperty(ref _totalShips, value);
        }

        public int ActiveShips
        {
            get => _activeShips;
            set => SetProperty(ref _activeShips, value);
        }

        public int TotalSystems
        {
            get => _totalSystems;
            set => SetProperty(ref _totalSystems, value);
        }

        public int TotalComponents
        {
            get => _totalComponents;
            set => SetProperty(ref _totalComponents, value);
        }

        public decimal TotalTonnage
        {
            get => _totalTonnage;
            set => SetProperty(ref _totalTonnage, value);
        }

        public int RecentChanges
        {
            get => _recentChanges;
            set => SetProperty(ref _recentChanges, value);
        }

        public string MostCommonShipType
        {
            get => _mostCommonShipType;
            set => SetProperty(ref _mostCommonShipType, value);
        }

        public string NewestShip
        {
            get => _newestShip;
            set => SetProperty(ref _newestShip, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int PendingDocuments
        {
            get => _pendingDocuments;
            set => SetProperty(ref _pendingDocuments, value);
        }

        public int PendingChangeRequests
        {
            get => _pendingChangeRequests;
            set => SetProperty(ref _pendingChangeRequests, value);
        }

        public int ActiveAlerts
        {
            get => _activeAlerts;
            set => SetProperty(ref _activeAlerts, value);
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetProperty(ref _isAdmin, value);
        }

        public int ActiveSystems
        {
            get => _activeSystems;
            set => SetProperty(ref _activeSystems, value);
        }

        public int SystemHealth
        {
            get => _systemHealth;
            set => SetProperty(ref _systemHealth, value);
        }

        public ObservableCollection<DashboardActivity> RecentActivities
        {
            get => _recentActivities;
            set => SetProperty(ref _recentActivities, value);
        }

        public ObservableCollection<Ship> RecentShips
        {
            get => _recentShips;
            set => SetProperty(ref _recentShips, value);
        }

        public ObservableCollection<ShipSystem> RecentSystems
        {
            get => _recentSystems;
            set => SetProperty(ref _recentSystems, value);
        }

        public ObservableCollection<Component> RecentComponents
        {
            get => _recentComponents;
            set => SetProperty(ref _recentComponents, value);
        }

        public ObservableCollection<ShipTypeStatistic> ShipTypeStatistics
        {
            get => _shipTypeStatistics;
            set => SetProperty(ref _shipTypeStatistics, value);
        }

        public ObservableCollection<FleetStatistic> FleetStatistics
        {
            get => _fleetStatistics;
            set => SetProperty(ref _fleetStatistics, value);
        }

        // Commands
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand ViewShipsCommand { get; private set; } = null!;
        public ICommand ViewSystemsCommand { get; private set; } = null!;
        public ICommand ViewComponentsCommand { get; private set; } = null!;
        public ICommand AddShipCommand { get; private set; } = null!;
        public ICommand AddSystemCommand { get; private set; } = null!;
        public ICommand AddComponentCommand { get; private set; } = null!;
        public ICommand ViewPendingDocumentsCommand { get; private set; } = null!;
        public ICommand ViewPendingChangeRequestsCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            RefreshCommand = new AsyncRelayCommand(LoadDashboardDataAsync);
            ViewShipsCommand = new RelayCommand(() => _navigationService.NavigateToPage("Ships"));
            ViewSystemsCommand = new RelayCommand(() => _navigationService.NavigateToPage("Systems"));
            ViewComponentsCommand = new RelayCommand(() => _navigationService.NavigateToPage("Components"));
            AddShipCommand = new RelayCommand(() => _navigationService.NavigateToPage("Ships"));
            AddSystemCommand = new RelayCommand(() => _navigationService.NavigateToPage("Systems"));
            AddComponentCommand = new RelayCommand(() => _navigationService.NavigateToPage("Components"));
            ViewPendingDocumentsCommand = new RelayCommand(() => _navigationService.NavigateToPage("Documents"));
            ViewPendingChangeRequestsCommand = new RelayCommand(() => _navigationService.NavigateToPage("ChangeRequests"));
        }

        private void SubscribeToDataChanges()
        {
            _dataChangeNotificationService.DataChanged += OnDataChanged;
        }

        private void InitializeRefreshTimer()
        {
            // Refresh dashboard every 15 seconds for real-time updates
            _refreshTimer.Interval = 15000; // 15 seconds
            _refreshTimer.Elapsed += async (sender, e) =>
            {
                try
                {
                    await LoadDashboardDataAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during periodic dashboard refresh");
                }
            };
            _refreshTimer.Start();
        }

        private async void OnDataChanged(object? sender, MaritimeERP.Core.Interfaces.DataChangeEventArgs e)
        {
            try
            {
                _logger.LogInformation("Dashboard received data change notification: {DataType} - {Operation}", e.DataType, e.Operation);
                
                // Refresh dashboard data when any relevant data changes
                if (e.DataType == "Ship" || e.DataType == "System" || e.DataType == "Component" || 
                    e.DataType == "Document" || e.DataType == "ChangeRequest")
                {
                    await LoadDashboardDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling data change notification");
            }
        }

        public async Task LoadDashboardDataAsync()
        {
            try
            {
                IsLoading = true;
                _logger.LogInformation("Loading dashboard data...");

                // Load basic statistics
                await LoadBasicStatisticsAsync();
                
                // Load recent activities
                await LoadRecentActivitiesAsync();
                
                // Load chart data
                await LoadChartDataAsync();

                _logger.LogInformation("Dashboard data loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadBasicStatisticsAsync()
        {
            try
            {
                // Get all data on background thread
                var shipStats = await _shipService.GetShipStatisticsAsync();
                var activeShipsCount = await _shipService.GetActiveShipCountAsync();
                var allShips = await _shipService.GetAllShipsAsync();
                var allSystems = await _systemService.GetAllSystemsAsync();
                var allComponents = await _componentService.GetAllComponentsAsync();

                var shipsList = allShips.ToList();
                var systemsList = allSystems.ToList();
                
                // Process data on background thread
                string newestShipName = "N/A";
                string mostCommonType = "N/A";
                int recentChangesCount = 0;
                int activeSystemsCount = systemsList.Count(); // All systems are considered active for now
                int pendingDocsCount = 0;
                int pendingChangeReqCount = 0;
                int alertsCount = 0;

                if (shipsList.Any())
                {
                    // Find newest ship
                    var newest = shipsList.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
                    newestShipName = newest?.ShipName ?? "N/A";

                    // Find most common ship type
                    var typeGroups = shipsList
                        .Where(s => !string.IsNullOrEmpty(s.ShipType))
                        .GroupBy(s => s.ShipType)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault();
                    mostCommonType = typeGroups?.Key ?? "N/A";

                    // Calculate recent changes (last 30 days)
                    var recentDate = DateTime.UtcNow.AddDays(-30);
                    recentChangesCount = shipsList.Count(s => s.UpdatedAt >= recentDate || s.CreatedAt >= recentDate);
                }

                // Load admin-specific data if user is admin
                if (_isAdmin)
                {
                    try
                    {
                        // Get pending documents
                        var pendingDocuments = await _documentService.GetDocumentsForApprovalAsync();
                        pendingDocsCount = pendingDocuments.Count;

                        // Get pending change requests
                        var currentUser = _authenticationService.CurrentUser;
                        if (currentUser != null)
                        {
                            var changeRequestStats = await _changeRequestService.GetChangeRequestStatisticsAsync();
                            pendingChangeReqCount = changeRequestStats.PendingApproval + changeRequestStats.UnderReview;
                        }

                        // Calculate alerts (pending items that need attention)
                        alertsCount = pendingDocsCount + pendingChangeReqCount;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading admin-specific statistics");
                        // Continue with basic stats even if admin stats fail
                    }
                }

                // Update properties on UI thread
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    TotalShips = shipStats.TotalShips;
                    TotalTonnage = shipStats.TotalGrossTonnage;
                    ActiveShips = activeShipsCount;
                    NewestShip = newestShipName;
                    MostCommonShipType = mostCommonType;
                    TotalSystems = systemsList.Count;
                    TotalComponents = allComponents.Count();
                    RecentChanges = recentChangesCount;
                    ActiveSystems = activeSystemsCount;
                    PendingDocuments = pendingDocsCount;
                    PendingChangeRequests = pendingChangeReqCount;
                    ActiveAlerts = alertsCount;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading basic statistics");
            }
        }

        private async Task LoadRecentActivitiesAsync()
        {
            try
            {
                // Get data on background thread
                var allShips = await _shipService.GetAllShipsAsync();
                var recentShips = allShips
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(5)
                    .ToList();

                var allSystems = await _systemService.GetAllSystemsAsync();
                var recentSystems = allSystems
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(5)
                    .ToList();

                var allComponents = await _componentService.GetAllComponentsAsync();
                var recentComponents = allComponents
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToList();

                // Prepare activities list
                var activities = new List<DashboardActivity>();

                // Add ship activities
                foreach (var ship in recentShips)
                {
                    activities.Add(new DashboardActivity
                    {
                        Type = "Ship",
                        Title = $"Ship Added: {ship.ShipName}",
                        Description = $"IMO: {ship.ImoNumber}, Type: {ship.ShipType}",
                        Timestamp = ship.CreatedAt,
                        Icon = "🚢"
                    });
                }

                // Add system activities
                foreach (var system in recentSystems)
                {
                    activities.Add(new DashboardActivity
                    {
                        Type = "System",
                        Title = $"System Added: {system.Name}",
                        Description = $"{system.Manufacturer} {system.Model}",
                        Timestamp = system.CreatedAt,
                        Icon = "⚙️"
                    });
                }

                // Add component activities
                foreach (var component in recentComponents)
                {
                    activities.Add(new DashboardActivity
                    {
                        Type = "Component",
                        Title = $"Component Added: {component.Name}",
                        Description = $"{component.MakerModel}",
                        Timestamp = component.CreatedAt,
                        Icon = "🔧"
                    });
                }

                // Sort all activities by timestamp
                var sortedActivities = activities
                    .OrderByDescending(a => a.Timestamp)
                    .ToList();

                // Update UI collections on UI thread
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                RecentActivities.Clear();
                foreach (var activity in sortedActivities)
                {
                    RecentActivities.Add(activity);
                }

                    RecentShips.Clear();
                    foreach (var ship in recentShips)
                    {
                        RecentShips.Add(ship);
                    }

                    RecentSystems.Clear();
                    foreach (var system in recentSystems)
                    {
                        RecentSystems.Add(system);
                    }

                    RecentComponents.Clear();
                    foreach (var component in recentComponents)
                    {
                        RecentComponents.Add(component);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent activities");
            }
        }

        private async Task LoadChartDataAsync()
        {
            try
            {
                // Get data on background thread
                var allShips = await _shipService.GetAllShipsAsync();

                // Ship type statistics
                var shipTypeGroups = allShips
                    .Where(s => !string.IsNullOrEmpty(s.ShipType))
                    .GroupBy(s => s.ShipType)
                    .Select(g => new ShipTypeStatistic
                    {
                        ShipType = g.Key,
                        Count = g.Count(),
                        TotalTonnage = g.Sum(s => s.GrossTonnage ?? 0)
                    })
                    .OrderByDescending(s => s.Count)
                    .ToList();

                // Fleet statistics by status
                var activeCount = allShips.Count(s => s.IsActive);
                var inactiveCount = allShips.Count(s => !s.IsActive);

                var fleetStats = new List<FleetStatistic>
                {
                    new FleetStatistic { Status = "Active", Count = activeCount },
                    new FleetStatistic { Status = "Inactive", Count = inactiveCount }
                };

                // Update UI collections on UI thread
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ShipTypeStatistics.Clear();
                    foreach (var stat in shipTypeGroups)
                    {
                        ShipTypeStatistics.Add(stat);
                    }

                    FleetStatistics.Clear();
                    foreach (var stat in fleetStats)
                    {
                        FleetStatistics.Add(stat);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chart data");
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

        public void Dispose()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            _dataChangeNotificationService.DataChanged -= OnDataChanged;
        }
    }

    // Helper classes for dashboard data
    public class DashboardActivity
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; } = "📋";
        public string TimeAgo => GetTimeAgo(Timestamp);

        private static string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalDays > 1)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalHours > 1)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalMinutes > 1)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";

            return "Just now";
        }
    }

    public class ShipTypeStatistic
    {
        public string ShipType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalTonnage { get; set; }
    }

    public class FleetStatistic
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
} 