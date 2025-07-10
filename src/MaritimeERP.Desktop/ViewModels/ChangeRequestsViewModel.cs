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
    public class ChangeRequestsViewModel : INotifyPropertyChanged
    {
        private readonly IChangeRequestService _changeRequestService;
        private readonly IShipService _shipService;
        private readonly IAuthenticationService _authenticationService;
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

        public ChangeRequestsViewModel(
            IChangeRequestService changeRequestService,
            IShipService shipService,
            IAuthenticationService authenticationService,
            ILogger<ChangeRequestsViewModel> logger)
        {
            _changeRequestService = changeRequestService ?? throw new ArgumentNullException(nameof(changeRequestService));
            _shipService = shipService ?? throw new ArgumentNullException(nameof(shipService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize commands
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            AddChangeRequestCommand = new RelayCommand(AddChangeRequest, () => !IsLoading && !IsEditing);
            EditChangeRequestCommand = new RelayCommand(EditChangeRequest, () => SelectedChangeRequest != null && !IsLoading && !IsEditing);
            DeleteChangeRequestCommand = new RelayCommand(async () => await DeleteChangeRequestAsync(), () => SelectedChangeRequest != null && !IsLoading && !IsEditing);
            SaveChangeRequestCommand = new RelayCommand(async () => await SaveChangeRequestAsync(), () => CanSaveChangeRequest());
            CancelEditCommand = new RelayCommand(CancelEdit);
            SubmitForApprovalCommand = new RelayCommand(async () => await SubmitForApprovalAsync(), () => CanSubmitForApproval());
            ApproveCommand = new RelayCommand(async () => await ApproveAsync(), () => CanApprove());
            RejectCommand = new RelayCommand(async () => await RejectAsync(), () => CanReject());
            ImplementCommand = new RelayCommand(async () => await ImplementAsync(), () => CanImplement());
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ViewDetailsCommand = new RelayCommand(ViewDetails, () => SelectedChangeRequest != null);

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
                    StatusMessage = "Loading change requests...";
                });

                // Load all data in parallel
                var changeRequestsTask = _changeRequestService.GetAllChangeRequestsAsync();
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

                    // Update statistics
                    TotalRequests = statistics.TotalRequests;
                    PendingApprovals = statistics.PendingApproval;

                    // Calculate my requests
                    var currentUserId = _authenticationService.CurrentUser?.Id ?? 0;
                    MyRequests = changeRequests.Count(cr => cr.RequestedById == currentUserId);

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
            ClearForm();
            IsEditing = true;
            StatusMessage = "Creating new change request";
            RefreshCommands();
        }

        private void EditChangeRequest()
        {
            if (SelectedChangeRequest != null)
            {
                IsEditing = true;
                StatusMessage = $"Editing change request: {SelectedChangeRequest.RequestNo}";
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

            var result = MessageBox.Show(
                $"Are you sure you want to delete change request '{SelectedChangeRequest.RequestNo}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    var success = await _changeRequestService.DeleteChangeRequestAsync(SelectedChangeRequest.Id);
                    
                    if (success)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ChangeRequests.Remove(SelectedChangeRequest);
                            TotalRequests--;
                            if (SelectedChangeRequest.RequestedById == _authenticationService.CurrentUser?.Id)
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

        private void ClearForm()
        {
            RequestNo = string.Empty;
            Purpose = string.Empty;
            Description = string.Empty;
            SelectedShip = null;
            SelectedChangeType = null;
            SelectedStatus = ChangeStatuses.FirstOrDefault(cs => cs.Id == 1); // Draft
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
                   SelectedChangeRequest.RequestedById == _authenticationService.CurrentUser?.Id &&
                   !IsLoading &&
                   !IsEditing;
        }

        private bool CanApprove()
        {
            return SelectedChangeRequest != null &&
                   (SelectedChangeRequest.StatusId == 2 || SelectedChangeRequest.StatusId == 3) && // Submitted or Under Review
                   SelectedChangeRequest.RequestedById != _authenticationService.CurrentUser?.Id &&
                   !IsLoading &&
                   !IsEditing;
        }

        private bool CanReject()
        {
            return CanApprove(); // Same conditions as approve
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
} 