using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Core.Interfaces;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Desktop.Views;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace MaritimeERP.Desktop.ViewModels
{
    public class ChangeRequestsViewModel : INotifyPropertyChanged
    {
        private readonly IChangeRequestService _changeRequestService;
        private readonly IShipService _shipService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ISoftwareService _softwareService;
        private readonly ISystemChangePlanService _systemChangePlanService;
        private readonly IHardwareChangeRequestService _hardwareChangeRequestService;
        private readonly ISoftwareChangeRequestService _softwareChangeRequestService;
        private readonly ISecurityReviewStatementService _securityReviewStatementService;
        private readonly IDataChangeNotificationService _dataChangeNotificationService;
        private readonly ILogger<ChangeRequestsViewModel> _logger;

        // Collections
        public ObservableCollection<ChangeRequest> ChangeRequests { get; } = new();
        public ObservableCollection<Ship> Ships { get; } = new();
        public ObservableCollection<ChangeType> ChangeTypes { get; } = new();
        public ObservableCollection<ChangeStatus> ChangeStatuses { get; } = new();

        // Selected items
        private ChangeRequest? _selectedChangeRequest;
        public ChangeRequest? SelectedChangeRequest
        {
            get => _selectedChangeRequest;
            set
            {
                if (SetProperty(ref _selectedChangeRequest, value))
                {
                    OnSelectedChangeRequestChanged();
                }
            }
        }

        private Ship? _selectedShip;
        public Ship? SelectedShip
        {
            get => _selectedShip;
            set => SetProperty(ref _selectedShip, value);
        }

        private ChangeType? _selectedChangeType;
        public ChangeType? SelectedChangeType
        {
            get => _selectedChangeType;
            set => SetProperty(ref _selectedChangeType, value);
        }

        private ChangeStatus? _selectedStatus;
        public ChangeStatus? SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        // Form properties
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        private string _requestNo = string.Empty;
        public string RequestNo
        {
            get => _requestNo;
            set => SetProperty(ref _requestNo, value);
        }

        private string _purpose = string.Empty;
        public string Purpose
        {
            get => _purpose;
            set => SetProperty(ref _purpose, value);
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        // Hardware Change Request properties
        private string _installedCbs = string.Empty;
        public string InstalledCbs
        {
            get => _installedCbs;
            set => SetProperty(ref _installedCbs, value);
        }

        private string _installedComponent = string.Empty;
        public string InstalledComponent
        {
            get => _installedComponent;
            set => SetProperty(ref _installedComponent, value);
        }

        private string _beforeHwModel = string.Empty;
        public string BeforeHwModel
        {
            get => _beforeHwModel;
            set => SetProperty(ref _beforeHwModel, value);
        }

        private string _beforeHwName = string.Empty;
        public string BeforeHwName
        {
            get => _beforeHwName;
            set => SetProperty(ref _beforeHwName, value);
        }

        private string _beforeHwOs = string.Empty;
        public string BeforeHwOs
        {
            get => _beforeHwOs;
            set => SetProperty(ref _beforeHwOs, value);
        }

        private string _afterHwModel = string.Empty;
        public string AfterHwModel
        {
            get => _afterHwModel;
            set => SetProperty(ref _afterHwModel, value);
        }

        private string _afterHwName = string.Empty;
        public string AfterHwName
        {
            get => _afterHwName;
            set => SetProperty(ref _afterHwName, value);
        }

        private string _afterHwOs = string.Empty;
        public string AfterHwOs
        {
            get => _afterHwOs;
            set => SetProperty(ref _afterHwOs, value);
        }

        private string _workDescription = string.Empty;
        public string WorkDescription
        {
            get => _workDescription;
            set => SetProperty(ref _workDescription, value);
        }

        private string _securityReviewComment = string.Empty;
        public string SecurityReviewComment
        {
            get => _securityReviewComment;
            set => SetProperty(ref _securityReviewComment, value);
        }

        // Filter properties
        private string _searchTerm = string.Empty;
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (SetProperty(ref _searchTerm, value))
                {
                    ApplyFilters();
                }
            }
        }

        private Ship? _filterShip;
        public Ship? FilterShip
        {
            get => _filterShip;
            set
            {
                if (SetProperty(ref _filterShip, value))
                {
                    ApplyFilters();
                }
            }
        }

        private ChangeStatus? _filterStatus;
        public ChangeStatus? FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (SetProperty(ref _filterStatus, value))
                {
                    ApplyFilters();
                }
            }
        }

        // Status properties
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
        private int _totalRequests;
        public int TotalRequests
        {
            get => _totalRequests;
            set => SetProperty(ref _totalRequests, value);
        }

        private int _pendingApprovals;
        public int PendingApprovals
        {
            get => _pendingApprovals;
            set => SetProperty(ref _pendingApprovals, value);
        }

        private int _myRequests;
        public int MyRequests
        {
            get => _myRequests;
            set => SetProperty(ref _myRequests, value);
        }

        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand AddChangeRequestCommand { get; }
        public ICommand EditChangeRequestCommand { get; }
        public ICommand DeleteChangeRequestCommand { get; }
        public ICommand SaveChangeRequestCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand SubmitForApprovalCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand ImplementCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand OpenHardwareChangeRequestCommand { get; }
        public ICommand ViewChangeRequestCommand { get; }

        public ChangeRequestsViewModel(
            IChangeRequestService changeRequestService,
            IShipService shipService,
            IAuthenticationService authenticationService,
            ISoftwareService softwareService,
            ISystemChangePlanService systemChangePlanService,
            IHardwareChangeRequestService hardwareChangeRequestService,
            ISoftwareChangeRequestService softwareChangeRequestService,
            ISecurityReviewStatementService securityReviewStatementService,
            IDataChangeNotificationService dataChangeNotificationService,
            ILogger<ChangeRequestsViewModel> logger)
        {
            _changeRequestService = changeRequestService ?? throw new ArgumentNullException(nameof(changeRequestService));
            _shipService = shipService ?? throw new ArgumentNullException(nameof(shipService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _softwareService = softwareService ?? throw new ArgumentNullException(nameof(softwareService));
            _systemChangePlanService = systemChangePlanService ?? throw new ArgumentNullException(nameof(systemChangePlanService));
            _hardwareChangeRequestService = hardwareChangeRequestService ?? throw new ArgumentNullException(nameof(hardwareChangeRequestService));
            _softwareChangeRequestService = softwareChangeRequestService ?? throw new ArgumentNullException(nameof(softwareChangeRequestService));
            _securityReviewStatementService = securityReviewStatementService ?? throw new ArgumentNullException(nameof(securityReviewStatementService));
            _dataChangeNotificationService = dataChangeNotificationService ?? throw new ArgumentNullException(nameof(dataChangeNotificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize commands
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            AddChangeRequestCommand = new RelayCommand(AddChangeRequest, () => !IsLoading && !IsEditing);
            EditChangeRequestCommand = new RelayCommand<ChangeRequest>(EditChangeRequest, (cr) => cr != null && !IsLoading && !IsEditing && CanEditChangeRequest(cr));
            DeleteChangeRequestCommand = new RelayCommand(async () => await DeleteChangeRequestAsync(), () => CanDeleteChangeRequest());
            SaveChangeRequestCommand = new RelayCommand(async () => await SaveChangeRequestAsync(), () => CanSaveChangeRequest());
            CancelEditCommand = new RelayCommand(CancelEdit);
            SubmitForApprovalCommand = new RelayCommand(async () => await SubmitForApprovalAsync(), () => CanSubmitForApproval());
            ApproveCommand = new RelayCommand(async () => await ApproveAsync(), () => CanApprove());
            RejectCommand = new RelayCommand(async () => await RejectAsync(), () => CanReject());
            ImplementCommand = new RelayCommand(async () => await ImplementAsync(), () => CanImplement());
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ViewDetailsCommand = new RelayCommand(ViewDetails, () => SelectedChangeRequest != null);
            OpenHardwareChangeRequestCommand = new RelayCommand(OpenHardwareChangeRequest, () => SelectedChangeRequest != null && SelectedChangeRequest.RequestTypeId == 1); // Only for Hardware Change
            ViewChangeRequestCommand = new RelayCommand<ChangeRequest>(ViewChangeRequest, (cr) => cr != null && !IsLoading);

            // Subscribe to ship data changes
            SubscribeToShipDataChanges();

            // Load initial data
            _ = LoadDataAsync();
        }

        private void SubscribeToShipDataChanges()
        {
            _dataChangeNotificationService.DataChanged += OnDataChanged;
        }

        private async void OnDataChanged(object? sender, MaritimeERP.Core.Interfaces.DataChangeEventArgs e)
        {
            try
            {
                // Refresh ship data when ships are created, updated, or deleted
                if (e.DataType == "Ship" && (e.Operation == "CREATE" || e.Operation == "UPDATE" || e.Operation == "DELETE"))
                {
                    _logger.LogInformation("ChangeRequestsViewModel received ship data change notification: {DataType} - {Operation}", e.DataType, e.Operation);
                    await RefreshShipDataAsync();
                }
                
                // Refresh change request data when change requests are created, updated, or deleted
                if (e.DataType == "ChangeRequest" && (e.Operation == "CREATE" || e.Operation == "UPDATE" || e.Operation == "DELETE" || e.Operation == "APPROVE" || e.Operation == "REJECT"))
                {
                    _logger.LogInformation("ChangeRequestsViewModel received change request data change notification: {DataType} - {Operation}", e.DataType, e.Operation);
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling data change notification in ChangeRequestsViewModel");
            }
        }

        private async Task RefreshShipDataAsync()
        {
            try
            {
                var ships = await _shipService.GetAllShipsAsync();
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Ships.Clear();
                    foreach (var ship in ships)
                    {
                        Ships.Add(ship);
                    }
                    
                    _logger.LogInformation("ChangeRequestsViewModel refreshed ship data - {ShipCount} ships loaded", Ships.Count);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing ship data in ChangeRequestsViewModel");
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "Loading change requests...";
                });

                // Load data in parallel - use role-based filtering for change requests
                var currentUser = _authenticationService.CurrentUser;
                if (currentUser == null)
                {
                    StatusMessage = "User not authenticated";
                    return;
                }

                var changeRequestsTask = _changeRequestService.GetChangeRequestsForUserAsync(currentUser.Id, currentUser.Role?.Name ?? "");
                var shipsTask = _shipService.GetAllShipsAsync();
                var statisticsTask = _changeRequestService.GetChangeRequestStatisticsAsync();

                await Task.WhenAll(changeRequestsTask, shipsTask, statisticsTask);

                var changeRequests = await changeRequestsTask;
                var ships = await shipsTask;
                var statistics = await statisticsTask;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Update collections
                    ChangeRequests.Clear();
                    foreach (var request in changeRequests)
                    {
                        ChangeRequests.Add(request);
                    }

                    Ships.Clear();
                    foreach (var ship in ships)
                    {
                        Ships.Add(ship);
                    }

                    // Update statistics based on user role
                    if (currentUser.Role?.Name == "Administrator")
                    {
                        // Admins see global statistics
                        TotalRequests = statistics.TotalRequests;
                        PendingApprovals = statistics.PendingApproval;
                    }
                    else
                    {
                        // Normal users see only their own statistics
                        TotalRequests = changeRequests.Count();
                        PendingApprovals = changeRequests.Count(cr => cr.StatusId == 2 || cr.StatusId == 3); // Submitted or Under Review
                    }

                    // Calculate my requests (for normal users, this is the same as total since they only see their own)
                    var currentUserId = _authenticationService.CurrentUser?.Id ?? 0;
                    if (currentUser.Role?.Name == "Administrator")
                    {
                        // Admins see all requests, so calculate their own requests separately
                        MyRequests = changeRequests.Count(cr => cr.RequestedById == currentUserId);
                    }
                    else
                    {
                        // Normal users only see their own requests, so MyRequests = TotalRequests for them
                        MyRequests = changeRequests.Count();
                    }

                    StatusMessage = $"Loaded {ChangeRequests.Count} change requests";
                });

                // Load lookup data
                await LoadLookupDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading change requests data");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error loading data";
                });
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private async Task LoadLookupDataAsync()
        {
            try
            {
                // For now, we'll create the lookup data manually
                // In a real implementation, you might have services for these
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ChangeTypes.Clear();
                    ChangeTypes.Add(new ChangeType { Id = 1, Name = "Hardware Change", Description = "Hardware modification or replacement" });
                    ChangeTypes.Add(new ChangeType { Id = 2, Name = "Software Change", Description = "Software update or configuration change" });
                    ChangeTypes.Add(new ChangeType { Id = 3, Name = "System Plan", Description = "System planning and design change" });
                    ChangeTypes.Add(new ChangeType { Id = 4, Name = "Security Review Statement", Description = "Security review statement" });

                    ChangeStatuses.Clear();
                    ChangeStatuses.Add(new ChangeStatus { Id = 1, Name = "Draft", Description = "Change request being prepared" });
                    ChangeStatuses.Add(new ChangeStatus { Id = 2, Name = "Submitted", Description = "Change request submitted for review" });
                    ChangeStatuses.Add(new ChangeStatus { Id = 3, Name = "Under Review", Description = "Change request under review" });
                    ChangeStatuses.Add(new ChangeStatus { Id = 4, Name = "Approved", Description = "Change request approved" });
                    ChangeStatuses.Add(new ChangeStatus { Id = 5, Name = "Rejected", Description = "Change request rejected" });
                    ChangeStatuses.Add(new ChangeStatus { Id = 6, Name = "Completed", Description = "Change request completed" });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lookup data");
            }
        }

        private void OnSelectedChangeRequestChanged()
        {
            if (SelectedChangeRequest != null && !IsEditing)
            {
                PopulateFormFromSelectedRequest();
            }
            RefreshCommands();
        }

        private void PopulateFormFromSelectedRequest()
        {
            if (SelectedChangeRequest == null) return;

            RequestNo = SelectedChangeRequest.RequestNo;
            Purpose = SelectedChangeRequest.Purpose;
            Description = SelectedChangeRequest.Description ?? string.Empty;
            SelectedShip = Ships.FirstOrDefault(s => s.Id == SelectedChangeRequest.ShipId);
            SelectedChangeType = ChangeTypes.FirstOrDefault(ct => ct.Id == SelectedChangeRequest.RequestTypeId);
            SelectedStatus = ChangeStatuses.FirstOrDefault(cs => cs.Id == SelectedChangeRequest.StatusId);
        }

        private void AddChangeRequest()
        {
            // Show type selection dialog
            var typeDialog = new ChangeRequestTypeSelectionDialog();
            var result = typeDialog.ShowDialog();
            
            if (result == true)
            {
                var selectedTypeId = typeDialog.SelectedTypeId;
                
                switch (selectedTypeId)
                {
                    case 1: // Hardware Change
                        OpenHardwareChangeRequestForm();
                        break;
                    case 2: // Software Change
                        OpenSoftwareChangeRequestForm();
                        break;
                    case 3: // System Plan
                        OpenSystemPlanChangeRequestForm();
                        break;
                    case 4: // Security Review Statement
                        OpenSecurityReviewStatementForm();
                        break;
                }
            }
        }

        private void OpenHardwareChangeRequestForm()
        {
            var viewModel = new HardwareChangeRequestDialogViewModel(_authenticationService, _hardwareChangeRequestService, _changeRequestService, _shipService);
            var dialog = new HardwareChangeRequestDialog(viewModel);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                // Refresh the change requests list to show the new entry
                _ = LoadDataAsync();
            }
        }

        private void OpenSoftwareChangeRequestForm()
        {
            var viewModel = new SoftwareChangeRequestDialogViewModel(_softwareChangeRequestService, _authenticationService, _changeRequestService, _shipService);
            var dialog = new SoftwareChangeRequestDialog(viewModel);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                // Refresh the change requests list to show the new entry
                _ = LoadDataAsync();
            }
        }

        private void OpenSystemPlanChangeRequestForm()
        {
            var viewModel = new SystemChangePlanDialogViewModel(_systemChangePlanService, _authenticationService, _shipService, _changeRequestService);
            var dialog = new SystemChangePlanDialog(viewModel);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                // Refresh the change requests list to show the new entry
                _ = LoadDataAsync();
            }
        }

        private void OpenSecurityReviewStatementForm()
        {
            var viewModel = new SecurityReviewStatementDialogViewModel(_securityReviewStatementService, _authenticationService, _shipService, _changeRequestService);
            var dialog = new SecurityReviewStatementDialog(viewModel);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                // Refresh the change requests list to show the new entry
                _ = LoadDataAsync();
            }
        }

        private async Task SaveNewChangeRequestAsync(ChangeRequest changeRequest)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Saving change request...";

                var created = await _changeRequestService.CreateChangeRequestAsync(changeRequest);
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ChangeRequests.Add(created);
                    TotalRequests++;
                    MyRequests++;
                    StatusMessage = "Change request created successfully";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving new change request");
                StatusMessage = "Error saving change request";
                MessageBox.Show($"Error saving change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                RefreshCommands();
            }
        }

        private async Task SaveChangeRequestAsync()
        {
            if (!CanSaveChangeRequest()) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Saving change request...";

                var changeRequest = new ChangeRequest
                {
                    Id = SelectedChangeRequest?.Id ?? 0,
                    RequestNo = RequestNo,
                    ShipId = SelectedShip?.Id,
                    RequestTypeId = SelectedChangeType?.Id ?? 1,
                    StatusId = SelectedStatus?.Id ?? 1,
                    RequestedById = _authenticationService.CurrentUser?.Id ?? 1,
                    Purpose = Purpose,
                    Description = Description,
                    RequestedAt = DateTime.UtcNow
                };

                if (SelectedChangeRequest == null)
                {
                    // Create new
                    var created = await _changeRequestService.CreateChangeRequestAsync(changeRequest);
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ChangeRequests.Add(created);
                        SelectedChangeRequest = created;
                        TotalRequests++;
                        MyRequests++;
                        StatusMessage = "Change request created successfully";
                    });
                }
                else
                {
                    // Update existing
                    changeRequest.Id = SelectedChangeRequest.Id;
                    var updated = await _changeRequestService.UpdateChangeRequestAsync(changeRequest);
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var index = ChangeRequests.ToList().FindIndex(cr => cr.Id == SelectedChangeRequest.Id);
                        if (index >= 0)
                        {
                            ChangeRequests[index] = updated;
                            SelectedChangeRequest = updated;
                        }
                        StatusMessage = "Change request updated successfully";
                    });
                }

                IsEditing = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving change request");
                StatusMessage = "Error saving change request";
                MessageBox.Show($"Error saving change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                RefreshCommands();
            }
        }

        private async Task DeleteChangeRequestAsync()
        {
            if (SelectedChangeRequest == null) return;

            var changeRequestToDelete = SelectedChangeRequest; // Capture the reference
            var result = MessageBox.Show(
                $"Are you sure you want to delete change request '{changeRequestToDelete.RequestNo}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    var success = await _changeRequestService.DeleteChangeRequestAsync(changeRequestToDelete.Id);
                    
                    if (success)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ChangeRequests.Remove(changeRequestToDelete);
                            TotalRequests--;
                            if (changeRequestToDelete.RequestedById == _authenticationService.CurrentUser?.Id)
                            {
                                MyRequests--;
                            }
                            SelectedChangeRequest = null;
                            ClearForm();
                            StatusMessage = "Change request deleted successfully";
                        });
                    }
                    else
                    {
                        StatusMessage = "Failed to delete change request";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting change request");
                    StatusMessage = "Error deleting change request";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            if (SelectedChangeRequest != null)
            {
                PopulateFormFromSelectedRequest();
            }
            else
            {
                ClearForm();
            }
            StatusMessage = "Edit cancelled";
            RefreshCommands();
        }

        private async Task SubmitForApprovalAsync()
        {
            if (SelectedChangeRequest == null || _authenticationService.CurrentUser == null) return;

            try
            {
                IsLoading = true;
                var success = await _changeRequestService.SubmitForApprovalAsync(
                    SelectedChangeRequest.Id, 
                    _authenticationService.CurrentUser.Id);
                
                if (success)
                {
                    await LoadDataAsync(); // Refresh data
                    StatusMessage = "Change request submitted for approval";
                }
                else
                {
                    StatusMessage = "Failed to submit change request";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting change request for approval");
                StatusMessage = "Error submitting for approval";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ApproveAsync()
        {
            if (SelectedChangeRequest == null || _authenticationService.CurrentUser == null) return;

            var comment = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter approval comment (optional):",
                "Approve Change Request",
                "Approved");

            try
            {
                IsLoading = true;
                var success = await _changeRequestService.ApproveChangeRequestAsync(
                    SelectedChangeRequest.Id,
                    _authenticationService.CurrentUser.Id,
                    comment);
                
                if (success)
                {
                    await LoadDataAsync(); // Refresh data
                    StatusMessage = "Change request approved";
                }
                else
                {
                    StatusMessage = "Failed to approve change request";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving change request");
                StatusMessage = "Error approving change request";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RejectAsync()
        {
            if (SelectedChangeRequest == null || _authenticationService.CurrentUser == null) return;

            var comment = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter rejection reason:",
                "Reject Change Request",
                "");

            if (string.IsNullOrWhiteSpace(comment))
            {
                MessageBox.Show("Rejection reason is required.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                var success = await _changeRequestService.RejectChangeRequestAsync(
                    SelectedChangeRequest.Id,
                    _authenticationService.CurrentUser.Id,
                    comment);
                
                if (success)
                {
                    await LoadDataAsync(); // Refresh data
                    StatusMessage = "Change request rejected";
                }
                else
                {
                    StatusMessage = "Failed to reject change request";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting change request");
                StatusMessage = "Error rejecting change request";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ImplementAsync()
        {
            if (SelectedChangeRequest == null || _authenticationService.CurrentUser == null) return;

            var result = MessageBox.Show(
                "Mark this change request as implemented?",
                "Implement Change Request",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    var success = await _changeRequestService.ImplementChangeRequestAsync(
                        SelectedChangeRequest.Id,
                        _authenticationService.CurrentUser.Id);
                    
                    if (success)
                    {
                        await LoadDataAsync(); // Refresh data
                        StatusMessage = "Change request marked as implemented";
                    }
                    else
                    {
                        StatusMessage = "Failed to implement change request";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error implementing change request");
                    StatusMessage = "Error implementing change request";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ApplyFilters()
        {
            // This is a simple client-side filter
            // In a real application, you might want to filter on the server side
            StatusMessage = "Filters applied";
        }

        private void ClearFilters()
        {
            SearchTerm = string.Empty;
            FilterShip = null;
            FilterStatus = null;
            StatusMessage = "Filters cleared";
        }

        private void ViewDetails()
        {
            if (SelectedChangeRequest != null)
            {
                // TODO: Implement detailed view dialog
                MessageBox.Show(
                    $"Change Request Details:\n\n" +
                    $"Request No: {SelectedChangeRequest.RequestNo}\n" +
                    $"Ship: {SelectedChangeRequest.ShipDisplay}\n" +
                    $"Type: {SelectedChangeRequest.TypeDisplay}\n" +
                    $"Status: {SelectedChangeRequest.StatusDisplay}\n" +
                    $"Purpose: {SelectedChangeRequest.Purpose}\n" +
                    $"Description: {SelectedChangeRequest.Description}",
                    "Change Request Details",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private async void OpenHardwareChangeRequest()
        {
            if (SelectedChangeRequest == null || SelectedChangeRequest.RequestTypeId != 1) return; // Only for Hardware Change

            try
            {
                // Load the detailed hardware change request data
                var hardwareRequests = await _hardwareChangeRequestService.GetAllAsync();
                var hardwareRequest = hardwareRequests.FirstOrDefault(hr => hr.RequestNumber == SelectedChangeRequest.RequestNo);
                
                var viewModel = new HardwareChangeRequestDialogViewModel(_authenticationService, _hardwareChangeRequestService, _changeRequestService, _shipService);
                
                // Always use view-only mode for this command
                viewModel.IsViewMode = true;
                viewModel.IsEditMode = false; // Disable editing completely
                
                // Pre-populate with existing data
                viewModel.RequestNumber = SelectedChangeRequest.RequestNo;
                viewModel.Reason = SelectedChangeRequest.Purpose;
                
                // Set the selected ship from the change request (ships are loaded in constructor)
                if (SelectedChangeRequest.ShipId.HasValue)
                {
                    // Give time for ships to load in constructor
                    await Task.Delay(100);
                    viewModel.SelectedShip = viewModel.Ships.FirstOrDefault(s => s.Id == SelectedChangeRequest.ShipId.Value);
                }
                
                // Load and populate hardware-specific data
                if (hardwareRequest != null)
                {
                    viewModel.SetExistingHardwareChangeRequest(hardwareRequest);
                }
                
                var dialog = new HardwareChangeRequestDialog(viewModel);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading hardware change request details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            RequestNo = string.Empty;
            Purpose = string.Empty;
            Description = string.Empty;
            SelectedShip = null;
            SelectedChangeType = null;
            SelectedStatus = ChangeStatuses.FirstOrDefault(cs => cs.Id == 1); // Draft
            
            // Clear hardware change request properties
            InstalledCbs = string.Empty;
            InstalledComponent = string.Empty;
            BeforeHwModel = string.Empty;
            BeforeHwName = string.Empty;
            BeforeHwOs = string.Empty;
            AfterHwModel = string.Empty;
            AfterHwName = string.Empty;
            AfterHwOs = string.Empty;
            WorkDescription = string.Empty;
            SecurityReviewComment = string.Empty;
        }

        private bool CanSaveChangeRequest()
        {
            return IsEditing &&
                   !string.IsNullOrWhiteSpace(Purpose) &&
                   SelectedChangeType != null &&
                   !IsLoading;
        }

        private bool CanSubmitForApproval()
        {
            return SelectedChangeRequest != null &&
                   SelectedChangeRequest.StatusId == 1 && // Draft
                   SelectedChangeRequest.RequestedById == _authenticationService.CurrentUser?.Id && // Can only submit own requests
                   !IsLoading &&
                   !IsEditing;
        }

        private bool CanApprove()
        {
            return SelectedChangeRequest != null &&
                   (SelectedChangeRequest.StatusId == 2 || SelectedChangeRequest.StatusId == 3) && // Submitted or Under Review
                   _authenticationService.CurrentUser?.Role?.Name == "Administrator" && // Only admins can approve
                   !IsLoading &&
                   !IsEditing;
        }

        private bool CanReject()
        {
            return SelectedChangeRequest != null &&
                   (SelectedChangeRequest.StatusId == 2 || SelectedChangeRequest.StatusId == 3) && // Submitted or Under Review
                   _authenticationService.CurrentUser?.Role?.Name == "Administrator" && // Only admins can reject
                   !IsLoading &&
                   !IsEditing;
        }

        private bool CanImplement()
        {
            return SelectedChangeRequest != null &&
                   SelectedChangeRequest.StatusId == 4 && // Approved
                   !IsLoading &&
                   !IsEditing;
        }

        private void RefreshCommands()
        {
            // Refresh all command states
            ((RelayCommand)AddChangeRequestCommand).RaiseCanExecuteChanged();
            ((RelayCommand)EditChangeRequestCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteChangeRequestCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SaveChangeRequestCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SubmitForApprovalCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ApproveCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RejectCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ImplementCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ViewDetailsCommand).RaiseCanExecuteChanged();
            ((RelayCommand)OpenHardwareChangeRequestCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ViewChangeRequestCommand).RaiseCanExecuteChanged();
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

        private void ViewChangeRequest(ChangeRequest? changeRequest)
        {
            if (changeRequest == null) return;

            // Open the appropriate form based on the change request type in view-only mode
            switch (changeRequest.RequestTypeId)
            {
                case 1: // Hardware Change
                    ViewHardwareChangeRequest(changeRequest);
                    break;
                case 2: // Software Change
                    ViewSoftwareChangeRequest(changeRequest);
                    break;
                case 3: // System Plan
                    ViewSystemPlanChangeRequest(changeRequest);
                    break;
                case 4: // Security Review Statement
                    ViewSecurityReviewStatement(changeRequest);
                    break;
                default:
                    MessageBox.Show("Unknown change request type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private async void ViewHardwareChangeRequest(ChangeRequest changeRequest)
        {
            try
            {
                // Load the detailed hardware change request data
                var hardwareRequests = await _hardwareChangeRequestService.GetAllAsync();
                var hardwareRequest = hardwareRequests.FirstOrDefault(hr => hr.RequestNumber == changeRequest.RequestNo);
                
                var viewModel = new HardwareChangeRequestDialogViewModel(_authenticationService, _hardwareChangeRequestService, _changeRequestService, _shipService);
                
                // Always use view-only mode for this command
                viewModel.IsViewMode = true;
                viewModel.IsEditMode = false; // Disable editing completely
                
                // Pre-populate with existing data
                viewModel.RequestNumber = changeRequest.RequestNo;
                viewModel.Reason = changeRequest.Purpose;
                
                // Set the selected ship from the change request (ships are loaded in constructor)
                if (changeRequest.ShipId.HasValue)
                {
                    // Give time for ships to load in constructor
                    await Task.Delay(100);
                    viewModel.SelectedShip = viewModel.Ships.FirstOrDefault(s => s.Id == changeRequest.ShipId.Value);
                }
                
                // Load and populate hardware-specific data
                if (hardwareRequest != null)
                {
                    viewModel.SetExistingHardwareChangeRequest(hardwareRequest);
                }
                
                var dialog = new HardwareChangeRequestDialog(viewModel);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening hardware change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ViewSoftwareChangeRequest(ChangeRequest changeRequest)
        {
            try
            {
                // Load the detailed software change request data
                var softwareRequests = await _softwareChangeRequestService.GetAllAsync();
                var softwareRequest = softwareRequests.FirstOrDefault(sr => sr.RequestNumber == changeRequest.RequestNo);
                
                var viewModel = new SoftwareChangeRequestDialogViewModel(_softwareChangeRequestService, _authenticationService, _changeRequestService, _shipService);
                
                // Always use view-only mode for this command
                viewModel.IsViewMode = true;
                viewModel.IsEditMode = false; // Disable editing completely
                
                // Pre-populate with existing data
                viewModel.RequestNumber = changeRequest.RequestNo;
                viewModel.Reason = changeRequest.Purpose;
                
                // Set the selected ship from the change request (ships are loaded in constructor)
                if (changeRequest.ShipId.HasValue)
                {
                    // Give time for ships to load in constructor
                    await Task.Delay(100);
                    viewModel.SelectedShip = viewModel.Ships.FirstOrDefault(s => s.Id == changeRequest.ShipId.Value);
                }
                
                // Load and populate software-specific data
                if (softwareRequest != null)
                {
                    viewModel.SetExistingSoftwareChangeRequest(softwareRequest);
                }
                
                var dialog = new SoftwareChangeRequestDialog(viewModel);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening software change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ViewSystemPlanChangeRequest(ChangeRequest changeRequest)
        {
            try
            {
                // Load the detailed system change plan data
                var systemChangePlans = await _systemChangePlanService.GetAllSystemChangePlansAsync();
                var systemChangePlan = systemChangePlans.FirstOrDefault(scp => scp.RequestNumber == changeRequest.RequestNo);
                
                var viewModel = new SystemChangePlanDialogViewModel(_systemChangePlanService, _authenticationService, _shipService, _changeRequestService);
                
                // Always use view-only mode for this command
                viewModel.IsViewMode = true;
                viewModel.IsEditMode = false; // Disable editing completely
                
                // Pre-populate with existing data
                viewModel.RequestNumber = changeRequest.RequestNo;
                viewModel.Reason = changeRequest.Purpose;
                
                // Set the selected ship from the change request (ships are loaded in constructor)
                if (changeRequest.ShipId.HasValue)
                {
                    // Give time for ships to load in constructor
                    await Task.Delay(100);
                    viewModel.SelectedShip = viewModel.Ships.FirstOrDefault(s => s.Id == changeRequest.ShipId.Value);
                }
                
                // Load and populate system plan-specific data
                if (systemChangePlan != null)
                {
                    viewModel.SetExistingSystemChangePlan(systemChangePlan);
                }
                
                var dialog = new SystemChangePlanDialog(viewModel);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening system change plan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ViewSecurityReviewStatement(ChangeRequest changeRequest)
        {
            try
            {
                // Load the detailed security review statement data
                var securityReviewStatements = await _securityReviewStatementService.GetAllSecurityReviewStatementsAsync();
                var securityReviewStatement = securityReviewStatements.FirstOrDefault(srs => srs.RequestNumber == changeRequest.RequestNo);
                
                var viewModel = new SecurityReviewStatementDialogViewModel(_securityReviewStatementService, _authenticationService, _shipService, _changeRequestService);
                
                // Always use view-only mode for this command
                viewModel.IsViewMode = true;
                viewModel.IsEditMode = false; // Disable editing completely
                
                // Pre-populate with existing data
                viewModel.RequestNumber = changeRequest.RequestNo;
                
                // Set the selected ship from the change request (ships are loaded in constructor)
                if (changeRequest.ShipId.HasValue)
                {
                    // Give time for ships to load in constructor
                    await Task.Delay(100);
                    viewModel.SelectedShip = viewModel.Ships.FirstOrDefault(s => s.Id == changeRequest.ShipId.Value);
                }
                
                // Load and populate security review statement-specific data
                if (securityReviewStatement != null)
                {
                    viewModel.SetExistingSecurityReviewStatement(securityReviewStatement);
                }
                
                var dialog = new SecurityReviewStatementDialog(viewModel);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening security review statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditChangeRequest(ChangeRequest? changeRequest)
        {
            if (changeRequest == null) return;

            // Check if user has permission to edit this change request
            if (!CanEditChangeRequest(changeRequest))
            {
                MessageBox.Show("You don't have permission to edit this change request. You can only edit requests that you created.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedChangeRequest = changeRequest;
            
            // Open the appropriate form based on the change request type
            switch (changeRequest.RequestTypeId)
            {
                case 1: // Hardware Change
                    EditHardwareChangeRequest(changeRequest);
                    break;
                case 2: // Software Change
                    EditSoftwareChangeRequest(changeRequest);
                    break;
                case 3: // System Plan
                    EditSystemPlanChangeRequest(changeRequest);
                    break;
                case 4: // Security Review Statement
                    EditSecurityReviewStatement(changeRequest);
                    break;
                default:
                    MessageBox.Show("Unknown change request type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private bool CanEditChangeRequest(ChangeRequest? changeRequest)
        {
            if (changeRequest == null) return false;
            
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return false;

            // Admins can edit any change request
            if (currentUser.Role?.Name == "Administrator") return true;

            // Normal users can only edit their own change requests that are still in Draft status
            return changeRequest.RequestedById == currentUser.Id && changeRequest.StatusId == 1; // Draft
        }

        private bool CanDeleteChangeRequest()
        {
            if (SelectedChangeRequest == null || IsLoading || IsEditing) return false;
            
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser == null) return false;

            // Admins can delete any change request
            if (currentUser.Role?.Name == "Administrator") return true;

            // Normal users can only delete their own change requests that are still in Draft status
            return SelectedChangeRequest.RequestedById == currentUser.Id && SelectedChangeRequest.StatusId == 1; // Draft
        }

        private async void EditHardwareChangeRequest(ChangeRequest changeRequest)
        {
            try
            {
                // Load the detailed hardware change request data
                var hardwareRequests = await _hardwareChangeRequestService.GetAllAsync();
                var hardwareRequest = hardwareRequests.FirstOrDefault(hr => hr.RequestNumber == changeRequest.RequestNo);
                
                var viewModel = new HardwareChangeRequestDialogViewModel(_authenticationService, _hardwareChangeRequestService, _changeRequestService, _shipService);
                
                // Determine if this should be view mode (admin reviewing submitted form) or edit mode
                var currentUser = _authenticationService.CurrentUser;
                var isAdmin = currentUser?.Role?.Name == "Administrator";
                var isSubmitted = changeRequest.StatusId == 2 || changeRequest.StatusId == 3; // Submitted or Under Review
                
                if (isAdmin && isSubmitted)
                {
                    // Admin reviewing submitted form - use view mode with approve/reject buttons
                    viewModel.IsViewMode = true;
                    viewModel.IsEditMode = true; // Set to true to populate data
                }
                else
                {
                    // Normal edit mode for the form creator or draft forms
                    viewModel.IsEditMode = true;
                }
                
                // Pre-populate with existing data
                viewModel.RequestNumber = changeRequest.RequestNo;
                viewModel.Reason = changeRequest.Purpose;
                
                // Set the selected ship from the change request
                if (changeRequest.ShipId.HasValue)
                {
                    var selectedShip = viewModel.Ships.FirstOrDefault(s => s.Id == changeRequest.ShipId.Value);
                    if (selectedShip != null)
                    {
                        viewModel.SelectedShip = selectedShip;
                    }
                }
                
                // Load hardware-specific data if found
                if (hardwareRequest != null)
                {
                    viewModel.Department = hardwareRequest.Department ?? "";
                    viewModel.PositionTitle = hardwareRequest.PositionTitle ?? "";
                    viewModel.RequesterName = hardwareRequest.RequesterName ?? "";
                    viewModel.InstalledCbs = hardwareRequest.InstalledCbs ?? "";
                    viewModel.InstalledComponent = hardwareRequest.InstalledComponent ?? "";
                    viewModel.BeforeHwManufacturerModel = hardwareRequest.BeforeHwManufacturerModel ?? "";
                    viewModel.BeforeHwName = hardwareRequest.BeforeHwName ?? "";
                    viewModel.BeforeHwOs = hardwareRequest.BeforeHwOs ?? "";
                    viewModel.AfterHwManufacturerModel = hardwareRequest.AfterHwManufacturerModel ?? "";
                    viewModel.AfterHwName = hardwareRequest.AfterHwName ?? "";
                    viewModel.AfterHwOs = hardwareRequest.AfterHwOs ?? "";
                    viewModel.WorkDescription = hardwareRequest.WorkDescription ?? "";
                    viewModel.SecurityReviewComment = hardwareRequest.SecurityReviewComment ?? "";
                    viewModel.CreatedDate = hardwareRequest.CreatedDate;
                    
                    // Set the internal hardware change request for editing
                    viewModel.SetExistingHardwareChangeRequest(hardwareRequest);
                }
                
                var dialog = new HardwareChangeRequestDialog(viewModel);
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Refresh the change requests list
                    _ = LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening hardware change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditSoftwareChangeRequest(ChangeRequest changeRequest)
        {
            try
            {
                // Load the detailed software change request data
                var softwareRequests = await _softwareChangeRequestService.GetAllAsync();
                var softwareRequest = softwareRequests.FirstOrDefault(sr => sr.RequestNumber == changeRequest.RequestNo);
                
                var viewModel = new SoftwareChangeRequestDialogViewModel(_softwareChangeRequestService, _authenticationService, _changeRequestService, _shipService);
                
                // Determine if this should be view mode (admin reviewing submitted form) or edit mode
                var currentUser = _authenticationService.CurrentUser;
                var isAdmin = currentUser?.Role?.Name == "Administrator";
                var isSubmitted = changeRequest.StatusId == 2 || changeRequest.StatusId == 3; // Submitted or Under Review
                
                if (isAdmin && isSubmitted)
                {
                    // Admin reviewing submitted form - use view mode with approve/reject buttons
                    viewModel.IsViewMode = true;
                    viewModel.IsEditMode = true; // Set to true to populate data
                }
                else
                {
                    // Normal edit mode for the form creator or draft forms
                    viewModel.IsEditMode = true;
                }
                
                // Pre-populate with existing data
                viewModel.RequestNumber = changeRequest.RequestNo;
                viewModel.Reason = changeRequest.Purpose;
                
                // Set the selected ship from the change request
                if (changeRequest.ShipId.HasValue)
                {
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
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Refresh the change requests list
                    _ = LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening software change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditSystemPlanChangeRequest(ChangeRequest changeRequest)
        {
            try
            {
                // Load the detailed system change plan data
                var systemChangePlans = await _systemChangePlanService.GetAllSystemChangePlansAsync();
                var systemChangePlan = systemChangePlans.FirstOrDefault(scp => scp.RequestNumber == changeRequest.RequestNo);
                
                var viewModel = new SystemChangePlanDialogViewModel(_systemChangePlanService, _authenticationService, _shipService, _changeRequestService);
                
                // Determine if this should be view mode (admin reviewing submitted form) or edit mode
                var currentUser = _authenticationService.CurrentUser;
                var isAdmin = currentUser?.Role?.Name == "Administrator";
                var isSubmitted = changeRequest.StatusId == 2 || changeRequest.StatusId == 3; // Submitted or Under Review
                
                if (isAdmin && isSubmitted)
                {
                    // Admin reviewing submitted form - use view mode with approve/reject buttons
                    viewModel.IsViewMode = true;
                    viewModel.IsEditMode = true; // Set to true to populate data
                }
                else
                {
                    // Normal edit mode for the form creator or draft forms
                    viewModel.IsEditMode = true;
                }
                
                // Pre-populate with existing data
                viewModel.RequestNumber = changeRequest.RequestNo;
                viewModel.Reason = changeRequest.Purpose;
                
                // Set the selected ship from the change request
                if (changeRequest.ShipId.HasValue)
                {
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
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Refresh the change requests list
                    _ = LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening system change plan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditSecurityReviewStatement(ChangeRequest changeRequest)
        {
            try
            {
                var securityReviewStatements = await _securityReviewStatementService.GetAllSecurityReviewStatementsAsync();
                var securityReviewStatement = securityReviewStatements.FirstOrDefault(srs => srs.RequestNumber == changeRequest.RequestNo);
                
                if (securityReviewStatement != null)
                {
                    var viewModel = new SecurityReviewStatementDialogViewModel(_securityReviewStatementService, _authenticationService, _shipService, _changeRequestService);
                    
                    // Determine if this should be view mode (admin reviewing submitted form) or edit mode
                    var currentUser = _authenticationService.CurrentUser;
                    var isAdmin = currentUser?.Role?.Name == "Administrator";
                    var isSubmitted = changeRequest.StatusId == 2 || changeRequest.StatusId == 3; // Submitted or Under Review
                    
                    if (isAdmin && isSubmitted)
                    {
                        // Admin reviewing submitted form - use view mode with approve/reject buttons
                        viewModel.IsViewMode = true;
                        viewModel.IsEditMode = true; // Set to true to populate data
                    }
                    else
                    {
                        // Normal edit mode for the form creator or draft forms
                        viewModel.IsEditMode = true;
                    }
                    
                    // Set the security review statement data
                    viewModel.SecurityReviewStatement = securityReviewStatement;
                    
                    // Set the selected ship from the change request
                    if (changeRequest.ShipId.HasValue)
                    {
                        var selectedShip = viewModel.Ships.FirstOrDefault(s => s.Id == changeRequest.ShipId.Value);
                        if (selectedShip != null)
                        {
                            viewModel.SelectedShip = selectedShip;
                        }
                    }
                    
                    var dialog = new SecurityReviewStatementDialog(viewModel);
                    var result = dialog.ShowDialog();

                    if (result == true)
                    {
                        // Refresh the change requests list
                        _ = LoadDataAsync();
                    }
                }
                else
                {
                    MessageBox.Show("Security Review Statement data not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening security review statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateChangeRequestAsync(ChangeRequest changeRequest)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Updating change request...";

                var updated = await _changeRequestService.UpdateChangeRequestAsync(changeRequest);
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var index = ChangeRequests.ToList().FindIndex(cr => cr.Id == changeRequest.Id);
                    if (index >= 0)
                    {
                        ChangeRequests[index] = updated;
                    }
                    StatusMessage = "Change request updated successfully";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating change request");
                StatusMessage = "Error updating change request";
                MessageBox.Show($"Error updating change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                RefreshCommands();
            }
        }
    }
} 