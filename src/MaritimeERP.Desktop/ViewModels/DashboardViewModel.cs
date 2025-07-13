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
using MaritimeERP.Desktop.Views;
using MaritimeERP.Desktop.ViewModels;

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
        private readonly IHardwareChangeRequestService _hardwareChangeRequestService;
        private readonly ISoftwareChangeRequestService _softwareChangeRequestService;
        private readonly ISystemChangePlanService _systemChangePlanService;
        private readonly ISecurityReviewStatementService _securityReviewStatementService;
        // Timer removed to prevent automatic refresh and visible screen flickering

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

        // Admin-specific data
        private ObservableCollection<Document> _pendingDocumentsList = new();
        private ObservableCollection<ChangeRequest> _pendingChangeRequestsList = new();

        public DashboardViewModel(
            IShipService shipService, 
            ISystemService systemService, 
            IComponentService componentService,
            IDocumentService documentService,
            IChangeRequestService changeRequestService,
            IAuthenticationService authenticationService,
            INavigationService navigationService,
            IDataChangeNotificationService dataChangeNotificationService,
            ILogger<DashboardViewModel> logger,
            IHardwareChangeRequestService hardwareChangeRequestService,
            ISoftwareChangeRequestService softwareChangeRequestService,
            ISystemChangePlanService systemChangePlanService,
            ISecurityReviewStatementService securityReviewStatementService)
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
            _hardwareChangeRequestService = hardwareChangeRequestService;
            _softwareChangeRequestService = softwareChangeRequestService;
            _systemChangePlanService = systemChangePlanService;
            _securityReviewStatementService = securityReviewStatementService;
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

        public ObservableCollection<Document> PendingDocumentsList
        {
            get => _pendingDocumentsList;
            set => SetProperty(ref _pendingDocumentsList, value);
        }

        public ObservableCollection<ChangeRequest> PendingChangeRequestsList
        {
            get => _pendingChangeRequestsList;
            set => SetProperty(ref _pendingChangeRequestsList, value);
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
        public ICommand ViewDocumentCommand { get; private set; } = null!;
        public ICommand ApproveDocumentCommand { get; private set; } = null!;
        public ICommand RejectDocumentCommand { get; private set; } = null!;
        public ICommand ViewChangeRequestCommand { get; private set; } = null!;
        public ICommand ApproveChangeRequestCommand { get; private set; } = null!;
        public ICommand RejectChangeRequestCommand { get; private set; } = null!;

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
            ViewDocumentCommand = new RelayCommand<Document>(ViewDocument);
            ApproveDocumentCommand = new RelayCommand<Document>(async (doc) => await ApproveDocumentAsync(doc));
            RejectDocumentCommand = new RelayCommand<Document>(async (doc) => await RejectDocumentAsync(doc));
            ViewChangeRequestCommand = new RelayCommand<ChangeRequest>(ViewChangeRequest);
            ApproveChangeRequestCommand = new RelayCommand<ChangeRequest>(async (cr) => await ApproveChangeRequestAsync(cr));
            RejectChangeRequestCommand = new RelayCommand<ChangeRequest>(async (cr) => await RejectChangeRequestAsync(cr));
        }

        private void SubscribeToDataChanges()
        {
            _dataChangeNotificationService.DataChanged += OnDataChanged;
        }

        private void InitializeRefreshTimer()
        {
            // Automatic refresh timer disabled to prevent visual flickering
            // Dashboard will still refresh when data changes through data change notifications
            // Users can manually refresh using the refresh button
            _logger.LogInformation("Dashboard automatic refresh timer disabled - manual refresh and data change notifications still active");
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
                            pendingChangeReqCount = changeRequestStats.PendingApproval;
                            
                            // Get pending change requests for admin action (to-do list behavior - exclude approved/completed items)
                            var allChangeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                            var pendingAdminChangeRequests = allChangeRequests.Where(cr => cr.StatusId == 2 || cr.StatusId == 3).ToList(); // Only Submitted or Under Review
                        
                            // Update change requests collection on UI thread
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                PendingChangeRequestsList.Clear();
                                foreach (var cr in pendingAdminChangeRequests.Take(10)) // Show only first 10 for space
                                {
                                    PendingChangeRequestsList.Add(cr);
                                }
                                
                                _logger.LogInformation("Dashboard loaded {PendingChangeReqCount} pending change requests for admin (to-do list behavior)", pendingAdminChangeRequests.Count);
                            });
                        }

                        // Calculate alerts (pending items that need attention)
                        alertsCount = pendingDocsCount + pendingChangeReqCount;

                        // Update admin-specific collections on UI thread
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            PendingDocumentsList.Clear();
                            foreach (var doc in pendingDocuments.Take(10)) // Show only first 10 for space
                            {
                                PendingDocumentsList.Add(doc);
                            }
                            
                            _logger.LogInformation("Dashboard loaded {PendingDocsCount} pending documents for admin", pendingDocuments.Count);
                        });
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

        private async void ViewDocument(Document? document)
        {
            if (document == null) return;

            try
            {
                // Create and show document preview dialog with authentication service
                var previewDialog = new DocumentPreviewDialog(document, _documentService, _authenticationService);
                
                // Only set owner if current window is not the same as the dialog
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null && mainWindow != previewDialog)
                {
                    previewDialog.Owner = mainWindow;
                }
                
                previewDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening document preview");
                
                // Fallback to showing basic details
                var details = $"Document: {document.Name}\n" +
                             $"Category: {document.Category?.Name}\n" +
                             $"Ship: {document.Ship?.ShipName ?? "Not assigned"}\n" +
                             $"File Size: {document.FileSizeDisplay}\n" +
                             $"Uploaded: {document.UploadedAtDisplay}\n" +
                             $"Uploaded by: {document.UploaderDisplay}\n" +
                             $"Status: {document.StatusDisplay}\n" +
                             $"Version: {document.Version}\n";

                if (!string.IsNullOrEmpty(document.Description))
                {
                    details += $"Description: {document.Description}\n";
                }

                if (!string.IsNullOrEmpty(document.Comments))
                {
                    details += $"Comments: {document.Comments}\n";
                }

                System.Windows.MessageBox.Show(details, "Document Details", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private async Task ApproveDocumentAsync(Document? document)
        {
            if (document == null) return;

            try
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to approve the document '{document.Name}'?",
                    "Approve Document",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // Get approval comments from user
                    var comments = Views.InputDialog.ShowDialog(
                        "Enter approval comments (optional):",
                        "Approve Document",
                        "Approved from dashboard");

                    if (comments != null) // User didn't cancel
                    {
                        await _documentService.ApproveDocumentAsync(document.Id, _authenticationService.CurrentUser!.Id, comments);
                        
                        // Notify other views that document data has changed
                        _dataChangeNotificationService.NotifyDataChanged("Document", "APPROVE", document);
                        
                        await LoadDashboardDataAsync(); // Refresh the data
                        System.Windows.MessageBox.Show("Document approved successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving document {DocumentId}", document.Id);
                System.Windows.MessageBox.Show($"Error approving document: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task RejectDocumentAsync(Document? document)
        {
            if (document == null) return;

            try
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to reject the document '{document.Name}'?",
                    "Reject Document",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // Get rejection comments from user
                    var comments = Views.InputDialog.ShowDialog(
                        "Enter rejection reason:",
                        "Reject Document",
                        "Rejected from dashboard");

                    if (comments != null) // User didn't cancel
                    {
                        await _documentService.RejectDocumentAsync(document.Id, _authenticationService.CurrentUser!.Id, comments);
                        
                        // Notify other views that document data has changed
                        _dataChangeNotificationService.NotifyDataChanged("Document", "REJECT", document);
                        
                        await LoadDashboardDataAsync(); // Refresh the data
                        System.Windows.MessageBox.Show("Document rejected successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting document {DocumentId}", document.Id);
                System.Windows.MessageBox.Show($"Error rejecting document: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void ViewChangeRequest(ChangeRequest? changeRequest)
        {
            if (changeRequest == null) return;

            try
            {
                // Open the appropriate dialog based on request type
                switch (changeRequest.RequestTypeId)
                {
                    case 1: // Hardware Change
                        await OpenHardwareChangeRequestViewAsync(changeRequest);
                        break;
                    case 2: // Software Change
                        await OpenSoftwareChangeRequestViewAsync(changeRequest);
                        break;
                    case 3: // System Plan
                        await OpenSystemChangePlanViewAsync(changeRequest);
                        break;
                    case 4: // Security Review Statement
                        await OpenSecurityReviewStatementViewAsync(changeRequest);
                        break;
                    default:
                        // Fallback to basic details
                        ShowBasicChangeRequestDetails(changeRequest);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening change request view for {RequestNo}", changeRequest.RequestNo);
                ShowBasicChangeRequestDetails(changeRequest);
            }
        }

        private void ShowBasicChangeRequestDetails(ChangeRequest changeRequest)
        {
            var details = $"Request No: {changeRequest.RequestNo}\n" +
                         $"Type: {changeRequest.RequestType?.Name ?? "Unknown"}\n" +
                         $"Ship: {changeRequest.Ship?.ShipName ?? "Not assigned"}\n" +
                         $"Purpose: {changeRequest.Purpose ?? "No purpose specified"}\n" +
                         $"Description: {changeRequest.Description ?? "No description"}\n" +
                         $"Status: {changeRequest.Status?.Name ?? "Unknown"}\n" +
                         $"Requested by: {changeRequest.RequestedBy?.FullName ?? "Unknown"}\n" +
                         $"Requested at: {changeRequest.RequestedAt:yyyy-MM-dd HH:mm}\n";

            if (!string.IsNullOrEmpty(changeRequest.Description))
            {
                details += $"Additional Details: {changeRequest.Description}\n";
            }

            System.Windows.MessageBox.Show(details, "Change Request Details", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private async Task ApproveChangeRequestAsync(ChangeRequest? changeRequest)
        {
            if (changeRequest == null) return;

            try
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to approve the change request '{changeRequest.RequestNo}'?",
                    "Approve Change Request",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // Get approval comments from user
                    var comments = Views.InputDialog.ShowDialog(
                        "Enter approval comments (optional):",
                        "Approve Change Request",
                        "Approved from dashboard");

                    if (comments != null) // User didn't cancel
                    {
                        var currentUserId = _authenticationService.CurrentUser?.Id ?? 0;
                        
                        // Approve the change request
                        await _changeRequestService.ApproveChangeRequestAsync(changeRequest.Id, currentUserId, comments);
                    
                    // Also approve the underlying form based on request type
                    switch (changeRequest.RequestTypeId)
                    {
                        case 1: // Hardware Change
                            var hardwareRequests = await _hardwareChangeRequestService.GetAllAsync();
                            var hardwareRequest = hardwareRequests.FirstOrDefault(hr => hr.RequestNumber == changeRequest.RequestNo);
                            if (hardwareRequest != null)
                            {
                                await _hardwareChangeRequestService.ApproveAsync(hardwareRequest.Id, currentUserId);
                            }
                            break;
                            
                        case 2: // Software Change
                            var softwareRequests = await _softwareChangeRequestService.GetAllAsync();
                            var softwareRequest = softwareRequests.FirstOrDefault(sr => sr.RequestNumber == changeRequest.RequestNo);
                            if (softwareRequest != null)
                            {
                                await _softwareChangeRequestService.ApproveAsync(softwareRequest.Id, currentUserId);
                            }
                            break;
                            
                        case 3: // System Plan
                            var systemChangePlans = await _systemChangePlanService.GetAllSystemChangePlansAsync();
                            var systemChangePlan = systemChangePlans.FirstOrDefault(scp => scp.RequestNumber == changeRequest.RequestNo);
                            if (systemChangePlan != null)
                            {
                                systemChangePlan.IsApproved = true;
                                systemChangePlan.IsUnderReview = false;
                                await _systemChangePlanService.UpdateSystemChangePlanAsync(systemChangePlan);
                            }
                            break;
                            
                        case 4: // Security Review Statement
                            var securityReviewStatements = await _securityReviewStatementService.GetAllSecurityReviewStatementsAsync();
                            var securityReviewStatement = securityReviewStatements.FirstOrDefault(srs => srs.RequestNumber == changeRequest.RequestNo);
                            if (securityReviewStatement != null)
                            {
                                await _securityReviewStatementService.ApproveAsync(securityReviewStatement.Id, currentUserId);
                            }
                            break;
                    }
                    
                        // Notify other views that change request data has changed
                        _dataChangeNotificationService.NotifyDataChanged("ChangeRequest", "APPROVE", changeRequest);
                        
                        await LoadDashboardDataAsync(); // Refresh the data
                        System.Windows.MessageBox.Show("Change request approved successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving change request {ChangeRequestId}", changeRequest.Id);
                System.Windows.MessageBox.Show($"Error approving change request: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task RejectChangeRequestAsync(ChangeRequest? changeRequest)
        {
            if (changeRequest == null) return;

            try
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to reject the change request '{changeRequest.RequestNo}'?",
                    "Reject Change Request",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // Get rejection comments from user
                    var comments = Views.InputDialog.ShowDialog(
                        "Enter rejection reason:",
                        "Reject Change Request",
                        "Rejected from dashboard");

                    if (comments != null) // User didn't cancel
                    {
                        var currentUserId = _authenticationService.CurrentUser?.Id ?? 0;
                        
                        // Reject the change request
                        await _changeRequestService.RejectChangeRequestAsync(changeRequest.Id, currentUserId, comments);
                    
                    // Also reject the underlying form based on request type
                    switch (changeRequest.RequestTypeId)
                    {
                        case 1: // Hardware Change
                            var hardwareRequests = await _hardwareChangeRequestService.GetAllAsync();
                            var hardwareRequest = hardwareRequests.FirstOrDefault(hr => hr.RequestNumber == changeRequest.RequestNo);
                            if (hardwareRequest != null)
                            {
                                await _hardwareChangeRequestService.RejectAsync(hardwareRequest.Id, currentUserId, comments);
                            }
                            break;
                            
                        case 2: // Software Change
                            var softwareRequests = await _softwareChangeRequestService.GetAllAsync();
                            var softwareRequest = softwareRequests.FirstOrDefault(sr => sr.RequestNumber == changeRequest.RequestNo);
                            if (softwareRequest != null)
                            {
                                await _softwareChangeRequestService.RejectAsync(softwareRequest.Id, currentUserId, comments);
                            }
                            break;
                            
                        case 3: // System Plan
                            var systemChangePlans = await _systemChangePlanService.GetAllSystemChangePlansAsync();
                            var systemChangePlan = systemChangePlans.FirstOrDefault(scp => scp.RequestNumber == changeRequest.RequestNo);
                            if (systemChangePlan != null)
                            {
                                systemChangePlan.IsApproved = false;
                                systemChangePlan.IsUnderReview = false;
                                systemChangePlan.SecurityReviewComments = comments;
                                await _systemChangePlanService.UpdateSystemChangePlanAsync(systemChangePlan);
                            }
                            break;
                            
                        case 4: // Security Review Statement
                            var securityReviewStatements = await _securityReviewStatementService.GetAllSecurityReviewStatementsAsync();
                            var securityReviewStatement = securityReviewStatements.FirstOrDefault(srs => srs.RequestNumber == changeRequest.RequestNo);
                            if (securityReviewStatement != null)
                            {
                                await _securityReviewStatementService.RejectAsync(securityReviewStatement.Id, currentUserId, comments);
                            }
                            break;
                    }
                    
                        // Notify other views that change request data has changed
                        _dataChangeNotificationService.NotifyDataChanged("ChangeRequest", "REJECT", changeRequest);
                        
                        await LoadDashboardDataAsync(); // Refresh the data
                        System.Windows.MessageBox.Show("Change request rejected successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting change request {ChangeRequestId}", changeRequest.Id);
                System.Windows.MessageBox.Show($"Error rejecting change request: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task OpenHardwareChangeRequestViewAsync(ChangeRequest changeRequest)
        {
            try
            {
                // Load the detailed hardware change request data
                var hardwareRequests = await _hardwareChangeRequestService.GetAllAsync();
                var hardwareRequest = hardwareRequests.FirstOrDefault(hr => hr.RequestNumber == changeRequest.RequestNo);
                
                var viewModel = new HardwareChangeRequestDialogViewModel(_authenticationService, _hardwareChangeRequestService, _changeRequestService, _shipService);
                
                // Set view mode for read-only viewing
                viewModel.IsViewMode = true;
                
                // Pre-populate with existing data
                viewModel.RequestNumber = changeRequest.RequestNo;
                viewModel.Reason = changeRequest.Purpose ?? "";
                
                // Set the selected ship from the change request
                if (changeRequest.ShipId.HasValue)
                {
                    await Task.Delay(500); // Give time for ships to load
                    var selectedShip = viewModel.Ships.FirstOrDefault(s => s.Id == changeRequest.ShipId.Value);
                    if (selectedShip != null)
                    {
                        viewModel.SelectedShip = selectedShip;
                    }
                }
                
                // Load hardware-specific data if found
                if (hardwareRequest != null)
                {
                    viewModel.SetExistingHardwareChangeRequest(hardwareRequest);
                }
                
                var dialog = new HardwareChangeRequestDialog(viewModel);
                
                // Set owner safely
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null && mainWindow != dialog)
                {
                    dialog.Owner = mainWindow;
                }
                
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening hardware change request view");
                // Fallback to basic details if form fails
                ShowBasicChangeRequestDetails(changeRequest);
            }
        }

        private async Task OpenSoftwareChangeRequestViewAsync(ChangeRequest changeRequest)
        {
            try
            {
                // Load the detailed software change request data
                var softwareRequests = await _softwareChangeRequestService.GetAllAsync();
                var softwareRequest = softwareRequests.FirstOrDefault(sr => sr.RequestNumber == changeRequest.RequestNo);
                
                var viewModel = new SoftwareChangeRequestDialogViewModel(_softwareChangeRequestService, _authenticationService, _changeRequestService, _shipService);
                
                // Set view mode for read-only viewing
                viewModel.IsViewMode = true;
                
                // Pre-populate with existing data
                viewModel.RequestNumber = changeRequest.RequestNo;
                viewModel.Reason = changeRequest.Purpose ?? "";
                
                // Set the selected ship from the change request
                if (changeRequest.ShipId.HasValue)
                {
                    await Task.Delay(500); // Give time for ships to load
                    var selectedShip = viewModel.Ships.FirstOrDefault(s => s.Id == changeRequest.ShipId.Value);
                    if (selectedShip != null)
                    {
                        viewModel.SelectedShip = selectedShip;
                    }
                }
                
                // Load software-specific data if found
                if (softwareRequest != null)
                {
                    viewModel.SetExistingSoftwareChangeRequest(softwareRequest);
                }
                
                var dialog = new SoftwareChangeRequestDialog(viewModel);
                
                // Set owner safely
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null && mainWindow != dialog)
                {
                    dialog.Owner = mainWindow;
                }
                
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening software change request view");
                // Fallback to basic details if form fails
                ShowBasicChangeRequestDetails(changeRequest);
            }
        }

        private async Task OpenSystemChangePlanViewAsync(ChangeRequest changeRequest)
        {
            try
            {
                // Load the detailed system change plan data
                var systemChangePlans = await _systemChangePlanService.GetAllSystemChangePlansAsync();
                var systemChangePlan = systemChangePlans.FirstOrDefault(scp => scp.RequestNumber == changeRequest.RequestNo);
                
                var viewModel = new SystemChangePlanDialogViewModel(_systemChangePlanService, _authenticationService, _shipService, _changeRequestService);
                
                // Set view mode for read-only viewing
                viewModel.IsViewMode = true;
                
                // Pre-populate with existing data
                viewModel.RequestNumber = changeRequest.RequestNo;
                viewModel.Reason = changeRequest.Purpose ?? "";
                
                // Set the selected ship from the change request
                if (changeRequest.ShipId.HasValue)
                {
                    await Task.Delay(500); // Give time for ships to load
                    var selectedShip = viewModel.Ships.FirstOrDefault(s => s.Id == changeRequest.ShipId.Value);
                    if (selectedShip != null)
                    {
                        viewModel.SelectedShip = selectedShip;
                    }
                }
                
                // Load system-specific data if found
                if (systemChangePlan != null)
                {
                    viewModel.SetExistingSystemChangePlan(systemChangePlan);
                }
                
                var dialog = new SystemChangePlanDialog(viewModel);
                
                // Set owner safely
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null && mainWindow != dialog)
                {
                    dialog.Owner = mainWindow;
                }
                
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening system change plan view");
                // Fallback to basic details if form fails
                ShowBasicChangeRequestDetails(changeRequest);
            }
        }

        private async Task OpenSecurityReviewStatementViewAsync(ChangeRequest changeRequest)
        {
            try
            {
                // Load the detailed security review statement data
                var securityReviewStatements = await _securityReviewStatementService.GetAllSecurityReviewStatementsAsync();
                var securityReviewStatement = securityReviewStatements.FirstOrDefault(srs => srs.RequestNumber == changeRequest.RequestNo);
                
                var viewModel = new SecurityReviewStatementDialogViewModel(_securityReviewStatementService, _authenticationService, _shipService, _changeRequestService);
                
                // Set view mode for read-only viewing
                viewModel.IsViewMode = true;
                
                // Pre-populate with existing data
                viewModel.RequestNumber = changeRequest.RequestNo;
                
                // Set the selected ship from the change request
                if (changeRequest.ShipId.HasValue)
                {
                    await Task.Delay(500); // Give time for ships to load
                    var selectedShip = viewModel.Ships.FirstOrDefault(s => s.Id == changeRequest.ShipId.Value);
                    if (selectedShip != null)
                    {
                        viewModel.SelectedShip = selectedShip;
                    }
                }
                
                // Load security-specific data if found
                if (securityReviewStatement != null)
                {
                    viewModel.SetExistingSecurityReviewStatement(securityReviewStatement);
                }
                
                var dialog = new SecurityReviewStatementDialog(viewModel);
                
                // Set owner safely
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null && mainWindow != dialog)
                {
                    dialog.Owner = mainWindow;
                }
                
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening security review statement view");
                // Fallback to basic details if form fails
                ShowBasicChangeRequestDetails(changeRequest);
            }
        }

        public void Dispose()
        {
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