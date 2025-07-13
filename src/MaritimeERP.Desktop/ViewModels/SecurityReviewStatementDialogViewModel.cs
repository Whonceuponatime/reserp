using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MaritimeERP.Desktop.ViewModels
{
    public class SecurityReviewStatementDialogViewModel : INotifyPropertyChanged
    {
        private readonly ISecurityReviewStatementService _securityReviewStatementService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IShipService _shipService;
        private readonly IChangeRequestService _changeRequestService;
        private SecurityReviewStatement _securityReviewStatement;
        private bool _isEditMode;

        public SecurityReviewStatementDialogViewModel(
            ISecurityReviewStatementService securityReviewStatementService,
            IAuthenticationService authenticationService,
            IShipService shipService,
            IChangeRequestService changeRequestService)
        {
            _securityReviewStatementService = securityReviewStatementService;
            _authenticationService = authenticationService;
            _shipService = shipService;
            _changeRequestService = changeRequestService;
            _securityReviewStatement = new SecurityReviewStatement();
            
            Ships = new ObservableCollection<Ship>();
            
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
            SubmitCommand = new RelayCommand(async () => await SubmitAsync(), () => CanSubmit());
            CancelCommand = new RelayCommand(() => CloseDialog());
            ApproveCommand = new RelayCommand(async () => await ApproveAsync(), () => CanApprove());
            RejectCommand = new RelayCommand(async () => await RejectAsync(), () => CanReject());
            
            // Initialize data asynchronously
            _ = InitializeAsync();
        }

        // Collections
        public ObservableCollection<Ship> Ships { get; }

        // Selected Ship
        private Ship? _selectedShip;
        public Ship? SelectedShip
        {
            get => _selectedShip;
            set
            {
                _selectedShip = value;
                OnPropertyChanged();
                _securityReviewStatement.ShipId = value?.Id;
            }
        }

        public SecurityReviewStatement SecurityReviewStatement
        {
            get => _securityReviewStatement;
            set
            {
                _securityReviewStatement = value;
                OnPropertyChanged();
                UpdateAllProperties();
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged();
            }
        }

        private bool _isViewMode;
        public bool IsViewMode
        {
            get => _isViewMode;
            set
            {
                _isViewMode = value;
                OnPropertyChanged();
            }
        }

        // Admin-specific properties
        public bool IsAdmin => _authenticationService.CurrentUser?.Role?.Name == "Administrator";
        public bool IsAdminViewing => IsAdmin && IsViewMode && IsUnderReview;
        public bool ShowSaveSubmitButtons => !IsAdminViewing;
        public bool ShowApproveRejectButtons => IsAdminViewing;

        // Properties bound to UI - Header Information
        public string RequestNumber
        {
            get => _securityReviewStatement.RequestNumber;
            set
            {
                _securityReviewStatement.RequestNumber = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreatedDate
        {
            get => _securityReviewStatement.CreatedDate;
            set
            {
                _securityReviewStatement.CreatedDate = value;
                OnPropertyChanged();
            }
        }

        public bool IsInDraftStatus
        {
            get => !_securityReviewStatement.IsUnderReview && !_securityReviewStatement.IsApproved;
        }

        public bool IsUnderReview
        {
            get => _securityReviewStatement.IsUnderReview;
            set
            {
                _securityReviewStatement.IsUnderReview = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsInDraftStatus));
            }
        }

        public bool IsApproved
        {
            get => _securityReviewStatement.IsApproved;
            set
            {
                _securityReviewStatement.IsApproved = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsInDraftStatus));
            }
        }

        // Reviewer Information (검토자)
        public string ReviewerDepartment
        {
            get => _securityReviewStatement.ReviewerDepartment;
            set
            {
                _securityReviewStatement.ReviewerDepartment = value;
                OnPropertyChanged();
            }
        }

        public string ReviewerPosition
        {
            get => _securityReviewStatement.ReviewerPosition;
            set
            {
                _securityReviewStatement.ReviewerPosition = value;
                OnPropertyChanged();
            }
        }

        public string ReviewerName
        {
            get => _securityReviewStatement.ReviewerName;
            set
            {
                _securityReviewStatement.ReviewerName = value;
                OnPropertyChanged();
            }
        }

        public DateTime? ReviewDate
        {
            get => _securityReviewStatement.ReviewDate;
            set
            {
                _securityReviewStatement.ReviewDate = value;
                OnPropertyChanged();
            }
        }

        // Review Items (검토 항목)
        public string ReviewItem1
        {
            get => _securityReviewStatement.ReviewItem1;
            set
            {
                _securityReviewStatement.ReviewItem1 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewResult1
        {
            get => _securityReviewStatement.ReviewResult1;
            set
            {
                _securityReviewStatement.ReviewResult1 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewRemarks1
        {
            get => _securityReviewStatement.ReviewRemarks1;
            set
            {
                _securityReviewStatement.ReviewRemarks1 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewItem2
        {
            get => _securityReviewStatement.ReviewItem2;
            set
            {
                _securityReviewStatement.ReviewItem2 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewResult2
        {
            get => _securityReviewStatement.ReviewResult2;
            set
            {
                _securityReviewStatement.ReviewResult2 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewRemarks2
        {
            get => _securityReviewStatement.ReviewRemarks2;
            set
            {
                _securityReviewStatement.ReviewRemarks2 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewItem3
        {
            get => _securityReviewStatement.ReviewItem3;
            set
            {
                _securityReviewStatement.ReviewItem3 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewResult3
        {
            get => _securityReviewStatement.ReviewResult3;
            set
            {
                _securityReviewStatement.ReviewResult3 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewRemarks3
        {
            get => _securityReviewStatement.ReviewRemarks3;
            set
            {
                _securityReviewStatement.ReviewRemarks3 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewItem4
        {
            get => _securityReviewStatement.ReviewItem4;
            set
            {
                _securityReviewStatement.ReviewItem4 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewResult4
        {
            get => _securityReviewStatement.ReviewResult4;
            set
            {
                _securityReviewStatement.ReviewResult4 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewRemarks4
        {
            get => _securityReviewStatement.ReviewRemarks4;
            set
            {
                _securityReviewStatement.ReviewRemarks4 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewItem5
        {
            get => _securityReviewStatement.ReviewItem5;
            set
            {
                _securityReviewStatement.ReviewItem5 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewResult5
        {
            get => _securityReviewStatement.ReviewResult5;
            set
            {
                _securityReviewStatement.ReviewResult5 = value;
                OnPropertyChanged();
            }
        }

        public string ReviewRemarks5
        {
            get => _securityReviewStatement.ReviewRemarks5;
            set
            {
                _securityReviewStatement.ReviewRemarks5 = value;
                OnPropertyChanged();
            }
        }

        // Overall Review Results (검토 결과)
        public string OverallReviewResult
        {
            get => _securityReviewStatement.OverallReviewResult;
            set
            {
                _securityReviewStatement.OverallReviewResult = value;
                OnPropertyChanged();
            }
        }

        // Review Opinion (검토 의견)
        public string ReviewOpinion
        {
            get => _securityReviewStatement.ReviewOpinion;
            set
            {
                _securityReviewStatement.ReviewOpinion = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }

        public event Action? RequestClose;

        public void SetExistingSecurityReviewStatement(SecurityReviewStatement securityReviewStatement)
        {
            _securityReviewStatement = securityReviewStatement;
            _isEditMode = true;
            IsEditMode = true;
            
            // Set status flags based on the security review statement status
            IsUnderReview = securityReviewStatement.IsUnderReview;
            IsApproved = securityReviewStatement.IsApproved;
            
            UpdateAllProperties();
            
            // Refresh UI properties
            OnPropertyChanged(nameof(IsAdminViewing));
            OnPropertyChanged(nameof(ShowSaveSubmitButtons));
            OnPropertyChanged(nameof(ShowApproveRejectButtons));
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadShips();
                await GenerateRequestNumberAsync();
                InitializeNewRequest();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing security review statement dialog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadShips()
        {
            try
            {
                var ships = await _shipService.GetAllShipsAsync();
                Ships.Clear();
                foreach (var ship in ships)
                {
                    Ships.Add(ship);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading ships: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task GenerateRequestNumberAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_securityReviewStatement.RequestNumber))
                {
                    var requestNumber = await _securityReviewStatementService.GenerateRequestNumberAsync();
                    RequestNumber = requestNumber;
                }
            }
            catch (Exception ex)
            {
                // Fallback to timestamp-based number if service fails
                RequestNumber = $"SER-{DateTime.Now:yyyyMM}-{DateTime.Now:HHmmss}";
                System.Diagnostics.Debug.WriteLine($"Failed to generate request number: {ex.Message}");
            }
        }

        private void InitializeNewRequest()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                // Initialize reviewer information with current user
                ReviewerName = currentUser.FullName ?? string.Empty;
                ReviewerDepartment = "Security Department"; // Default value
                ReviewerPosition = "Security Officer"; // Default value
            }
            
            // Set default review date to today
            ReviewDate = DateTime.Today;
        }

        private async Task SaveAsync()
        {
            try
            {
                // Set the user ID
                _securityReviewStatement.UserId = _authenticationService.CurrentUser?.Id;
                
                if (IsEditMode)
                {
                    await _securityReviewStatementService.UpdateSecurityReviewStatementAsync(_securityReviewStatement);
                    MessageBox.Show("Security review statement saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _securityReviewStatementService.CreateSecurityReviewStatementAsync(_securityReviewStatement);
                    
                    // Also create a ChangeRequest record for the main UI
                    var changeRequest = new ChangeRequest
                    {
                        RequestNo = _securityReviewStatement.RequestNumber,
                        RequestTypeId = 4, // Security Review Statement
                        StatusId = 1, // Draft
                        RequestedById = _authenticationService.CurrentUser?.Id ?? 1,
                        ShipId = SelectedShip?.Id,
                        Purpose = "Security Review Statement",
                        Description = $"Security Review Statement for {SelectedShip?.ShipName ?? "Unknown Ship"}",
                        RequestedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _changeRequestService.CreateChangeRequestAsync(changeRequest);
                    
                    MessageBox.Show("Security review statement created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    IsEditMode = true;
                }

                // Close dialog after successful save
                CloseDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving security review statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ApproveAsync()
        {
            try
            {
                if (_securityReviewStatement?.Id > 0)
                {
                    await _securityReviewStatementService.ApproveAsync(_securityReviewStatement.Id, _authenticationService.CurrentUser?.Id ?? 0);
                    
                    // Update the corresponding ChangeRequest status
                    var changeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                    var correspondingChangeRequest = changeRequests.FirstOrDefault(cr => cr.RequestNo == _securityReviewStatement.RequestNumber);
                    if (correspondingChangeRequest != null)
                    {
                        await _changeRequestService.ApproveChangeRequestAsync(correspondingChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0);
                    }
                    
                    IsApproved = true;
                    IsUnderReview = false;
                    
                    MessageBox.Show("Security review statement approved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh the UI properties
                    OnPropertyChanged(nameof(IsAdminViewing));
                    OnPropertyChanged(nameof(ShowSaveSubmitButtons));
                    OnPropertyChanged(nameof(ShowApproveRejectButtons));
                    
                    CloseDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error approving security review statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RejectAsync()
        {
            try
            {
                if (_securityReviewStatement?.Id > 0)
                {
                    await _securityReviewStatementService.RejectAsync(_securityReviewStatement.Id, _authenticationService.CurrentUser?.Id ?? 0, "Rejected by administrator");
                    
                    // Update the corresponding ChangeRequest status
                    var changeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                    var correspondingChangeRequest = changeRequests.FirstOrDefault(cr => cr.RequestNo == _securityReviewStatement.RequestNumber);
                    if (correspondingChangeRequest != null)
                    {
                        await _changeRequestService.RejectChangeRequestAsync(correspondingChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0, "Rejected by administrator");
                    }
                    
                    IsApproved = false;
                    IsUnderReview = false;
                    
                    MessageBox.Show("Security review statement rejected.", "Rejected", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh the UI properties
                    OnPropertyChanged(nameof(IsAdminViewing));
                    OnPropertyChanged(nameof(ShowSaveSubmitButtons));
                    OnPropertyChanged(nameof(ShowApproveRejectButtons));
                    
                    CloseDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error rejecting security review statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SubmitAsync()
        {
            try
            {
                await SaveAsync(); // Save first
                
                if (_authenticationService.CurrentUser?.Id != null)
                {
                    await _securityReviewStatementService.SubmitForReviewAsync(_securityReviewStatement.Id, _authenticationService.CurrentUser.Id);
                    
                    // Also update the corresponding ChangeRequest status
                    var changeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                    var correspondingChangeRequest = changeRequests.FirstOrDefault(cr => cr.RequestNo == _securityReviewStatement.RequestNumber);
                    if (correspondingChangeRequest != null)
                    {
                        await _changeRequestService.SubmitForApprovalAsync(correspondingChangeRequest.Id, _authenticationService.CurrentUser.Id);
                    }
                    
                    IsUnderReview = true;
                    MessageBox.Show("Security review statement submitted for review!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    RequestClose?.Invoke();
                }
                else
                {
                    MessageBox.Show("Unable to submit: User not authenticated.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error submitting security review statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDialog()
        {
            RequestClose?.Invoke();
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(ReviewerName) &&
                   !string.IsNullOrWhiteSpace(ReviewerDepartment) &&
                   SelectedShip != null;
        }

        private bool CanSubmit()
        {
            return CanSave() && 
                   !string.IsNullOrWhiteSpace(OverallReviewResult) &&
                   ReviewDate.HasValue;
        }

        private bool CanApprove()
        {
            return IsAdminViewing && _securityReviewStatement?.Id > 0;
        }

        private bool CanReject()
        {
            return IsAdminViewing && _securityReviewStatement?.Id > 0;
        }

        private void UpdateAllProperties()
        {
            OnPropertyChanged(nameof(RequestNumber));
            OnPropertyChanged(nameof(CreatedDate));
            OnPropertyChanged(nameof(IsInDraftStatus));
            OnPropertyChanged(nameof(IsUnderReview));
            OnPropertyChanged(nameof(IsApproved));
            OnPropertyChanged(nameof(ReviewerDepartment));
            OnPropertyChanged(nameof(ReviewerPosition));
            OnPropertyChanged(nameof(ReviewerName));
            OnPropertyChanged(nameof(ReviewDate));
            OnPropertyChanged(nameof(ReviewItem1));
            OnPropertyChanged(nameof(ReviewResult1));
            OnPropertyChanged(nameof(ReviewRemarks1));
            OnPropertyChanged(nameof(ReviewItem2));
            OnPropertyChanged(nameof(ReviewResult2));
            OnPropertyChanged(nameof(ReviewRemarks2));
            OnPropertyChanged(nameof(ReviewItem3));
            OnPropertyChanged(nameof(ReviewResult3));
            OnPropertyChanged(nameof(ReviewRemarks3));
            OnPropertyChanged(nameof(ReviewItem4));
            OnPropertyChanged(nameof(ReviewResult4));
            OnPropertyChanged(nameof(ReviewRemarks4));
            OnPropertyChanged(nameof(ReviewItem5));
            OnPropertyChanged(nameof(ReviewResult5));
            OnPropertyChanged(nameof(ReviewRemarks5));
            OnPropertyChanged(nameof(OverallReviewResult));
            OnPropertyChanged(nameof(ReviewOpinion));
            
            // Load selected ship if editing - do this asynchronously to ensure ships are loaded
            if (IsEditMode && _securityReviewStatement.ShipId.HasValue)
            {
                _ = SetSelectedShipAsync(_securityReviewStatement.ShipId.Value);
            }
        }

        private async Task SetSelectedShipAsync(int shipId)
        {
            // Wait for ships to be loaded if they haven't been loaded yet
            var maxWaitTime = TimeSpan.FromSeconds(5);
            var startTime = DateTime.Now;
            
            while (Ships.Count == 0 && DateTime.Now - startTime < maxWaitTime)
            {
                await Task.Delay(100);
            }
            
            // Set the selected ship
            var ship = Ships.FirstOrDefault(s => s.Id == shipId);
            if (ship != null)
            {
                SelectedShip = ship;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 