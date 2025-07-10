using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Component = MaritimeERP.Core.Entities.Component;

namespace MaritimeERP.Desktop.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IShipService _shipService;
        private readonly ISystemService _systemService;
        private readonly IComponentService _componentService;
        private readonly ILogger<DashboardViewModel> _logger;

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
            ILogger<DashboardViewModel> logger)
        {
            _shipService = shipService;
            _systemService = systemService;
            _componentService = componentService;
            _logger = logger;

            InitializeCommands();
            _ = LoadDashboardDataAsync();
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

        private void InitializeCommands()
        {
            RefreshCommand = new AsyncRelayCommand(LoadDashboardDataAsync);
            ViewShipsCommand = new RelayCommand(() => { /* Navigate to ships */ });
            ViewSystemsCommand = new RelayCommand(() => { /* Navigate to systems */ });
            ViewComponentsCommand = new RelayCommand(() => { /* Navigate to components */ });
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
                // Get ship statistics
                var shipStats = await _shipService.GetShipStatisticsAsync();
                TotalShips = shipStats.TotalShips;
                TotalTonnage = shipStats.TotalGrossTonnage;

                // Get active ships count
                ActiveShips = await _shipService.GetActiveShipCountAsync();

                // Get all ships to find newest and most common type
                var allShips = await _shipService.GetAllShipsAsync();
                var shipsList = allShips.ToList();

                if (shipsList.Any())
                {
                    // Find newest ship
                    var newest = shipsList.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
                    NewestShip = newest?.ShipName ?? "N/A";

                    // Find most common ship type
                    var typeGroups = shipsList
                        .Where(s => !string.IsNullOrEmpty(s.ShipType))
                        .GroupBy(s => s.ShipType)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault();
                    MostCommonShipType = typeGroups?.Key ?? "N/A";
                }

                // Get systems count
                var allSystems = await _systemService.GetAllSystemsAsync();
                TotalSystems = allSystems.Count();

                // Get components count
                var allComponents = await _componentService.GetAllComponentsAsync();
                TotalComponents = allComponents.Count();

                // Calculate recent changes (last 30 days)
                var recentDate = DateTime.UtcNow.AddDays(-30);
                RecentChanges = shipsList.Count(s => s.UpdatedAt >= recentDate || s.CreatedAt >= recentDate);
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
                RecentActivities.Clear();
                RecentShips.Clear();
                RecentSystems.Clear();
                RecentComponents.Clear();

                // Get recent ships (last 5)
                var allShips = await _shipService.GetAllShipsAsync();
                var recentShips = allShips
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(5)
                    .ToList();

                foreach (var ship in recentShips)
                {
                    RecentShips.Add(ship);
                    RecentActivities.Add(new DashboardActivity
                    {
                        Type = "Ship",
                        Title = $"Ship Added: {ship.ShipName}",
                        Description = $"IMO: {ship.ImoNumber}, Type: {ship.ShipType}",
                        Timestamp = ship.CreatedAt,
                        Icon = "🚢"
                    });
                }

                // Get recent systems (last 5)
                var allSystems = await _systemService.GetAllSystemsAsync();
                var recentSystems = allSystems
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(5)
                    .ToList();

                foreach (var system in recentSystems)
                {
                    RecentSystems.Add(system);
                    RecentActivities.Add(new DashboardActivity
                    {
                        Type = "System",
                        Title = $"System Added: {system.Name}",
                        Description = $"{system.Manufacturer} {system.Model}",
                        Timestamp = system.CreatedAt,
                        Icon = "⚙️"
                    });
                }

                // Get recent components (last 5)
                var allComponents = await _componentService.GetAllComponentsAsync();
                var recentComponents = allComponents
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToList();

                foreach (var component in recentComponents)
                {
                    RecentComponents.Add(component);
                    RecentActivities.Add(new DashboardActivity
                    {
                        Type = "Component",
                        Title = $"Component Added: {component.Name}",
                        Description = $"{component.MakerModel}",
                        Timestamp = component.CreatedAt,
                        Icon = "🔧"
                    });
                }

                // Sort all activities by timestamp
                var sortedActivities = RecentActivities
                    .OrderByDescending(a => a.Timestamp)
                    .ToList();

                RecentActivities.Clear();
                foreach (var activity in sortedActivities)
                {
                    RecentActivities.Add(activity);
                }
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
                ShipTypeStatistics.Clear();
                FleetStatistics.Clear();

                // Ship type statistics
                var allShips = await _shipService.GetAllShipsAsync();
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

                foreach (var stat in shipTypeGroups)
                {
                    ShipTypeStatistics.Add(stat);
                }

                // Fleet statistics by status
                var activeCount = allShips.Count(s => s.IsActive);
                var inactiveCount = allShips.Count(s => !s.IsActive);

                FleetStatistics.Add(new FleetStatistic { Status = "Active", Count = activeCount });
                FleetStatistics.Add(new FleetStatistic { Status = "Inactive", Count = inactiveCount });
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