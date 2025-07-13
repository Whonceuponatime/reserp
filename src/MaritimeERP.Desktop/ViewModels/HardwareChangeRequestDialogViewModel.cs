using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using MaritimeERP.Core.Entities;
using System.Windows;
using System.Collections.ObjectModel;

namespace MaritimeERP.Desktop.ViewModels
{
    public class HardwareChangeRequestDialogViewModel : INotifyPropertyChanged
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IHardwareChangeRequestService _hardwareChangeRequestService;
        private readonly IChangeRequestService _changeRequestService;
        private readonly IShipService _shipService;
        private HardwareChangeRequest? _hardwareChangeRequest;
        private bool _isEditMode;

        public HardwareChangeRequestDialogViewModel(
            IAuthenticationService authenticationService, 
            IHardwareChangeRequestService hardwareChangeRequestService,
            IChangeRequestService changeRequestService,
            IShipService shipService)
        {
            _authenticationService = authenticationService;
            _hardwareChangeRequestService = hardwareChangeRequestService;
            _changeRequestService = changeRequestService;
            _shipService = shipService;
            
            // Initialize with current user data
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                RequesterName = currentUser.FullName;
                Department = "Engineering"; // Default since User doesn't have Department
                PositionTitle = "Engineer"; // Default since User doesn't have Position
            }
            
            CreatedDate = DateTime.Now;
            
            // Generate request number immediately
            _ = GenerateRequestNumberAsync();
            
            // Load ships
            LoadShips();
            
            // Initialize commands
            SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
            SubmitCommand = new RelayCommand(async () => await SubmitAsync(), CanSubmit);
            CancelCommand = new RelayCommand(Cancel);
            ApproveCommand = new RelayCommand(async () => await ApproveAsync(), CanApprove);
            RejectCommand = new RelayCommand(async () => await RejectAsync(), CanReject);
        }

        #region Properties

        // Ship selection properties
        public ObservableCollection<Ship> Ships { get; } = new ObservableCollection<Ship>();
        
        private Ship? _selectedShip;
        public Ship? SelectedShip
        {
            get => _selectedShip;
            set => SetProperty(ref _selectedShip, value);
        }

        private string _requestNumber = string.Empty;
        public string RequestNumber
        {
            get => _requestNumber;
            set => SetProperty(ref _requestNumber, value);
        }

        private DateTime _createdDate = DateTime.Now;
        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        private string _department = string.Empty;
        public string Department
        {
            get => _department;
            set => SetProperty(ref _department, value);
        }

        private string _positionTitle = string.Empty;
        public string PositionTitle
        {
            get => _positionTitle;
            set => SetProperty(ref _positionTitle, value);
        }

        private string _requesterName = string.Empty;
        public string RequesterName
        {
            get => _requesterName;
            set => SetProperty(ref _requesterName, value);
        }

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

        private string _reason = string.Empty;
        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        private string _beforeHwManufacturerModel = string.Empty;
        public string BeforeHwManufacturerModel
        {
            get => _beforeHwManufacturerModel;
            set => SetProperty(ref _beforeHwManufacturerModel, value);
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

        private string _afterHwManufacturerModel = string.Empty;
        public string AfterHwManufacturerModel
        {
            get => _afterHwManufacturerModel;
            set => SetProperty(ref _afterHwManufacturerModel, value);
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

        private bool _isUnderReview;
        public bool IsUnderReview
        {
            get => _isUnderReview;
            set => SetProperty(ref _isUnderReview, value);
        }

        private bool _isApproved;
        public bool IsApproved
        {
            get => _isApproved;
            set => SetProperty(ref _isApproved, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private bool _isViewMode;
        public bool IsViewMode
        {
            get => _isViewMode;
            set => SetProperty(ref _isViewMode, value);
        }

        // Admin-specific properties
        public bool IsAdmin => _authenticationService.CurrentUser?.Role?.Name == "Administrator";
        public bool IsAdminViewing => IsAdmin && IsViewMode && IsUnderReview;
        public bool ShowSaveSubmitButtons => !IsAdminViewing;
        public bool ShowApproveRejectButtons => IsAdminViewing;

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }

        #endregion

        #region Events

        public event EventHandler? RequestClose;
        public event EventHandler? RequestSave;

        #endregion

        #region Methods

        /// <summary>
        /// Sets an existing hardware change request for editing
        /// </summary>
        /// <param name="hardwareChangeRequest">The existing hardware change request to edit</param>
        public void SetExistingHardwareChangeRequest(HardwareChangeRequest hardwareChangeRequest)
        {
            _hardwareChangeRequest = hardwareChangeRequest;
            _isEditMode = true;
            IsEditMode = true;
            
            // Populate form fields with existing data
            RequestNumber = hardwareChangeRequest.RequestNumber;
            RequesterName = hardwareChangeRequest.RequesterName;
            Department = hardwareChangeRequest.Department ?? "";
            PositionTitle = hardwareChangeRequest.PositionTitle ?? "";
            InstalledCbs = hardwareChangeRequest.InstalledCbs ?? "";
            InstalledComponent = hardwareChangeRequest.InstalledComponent ?? "";
            Reason = hardwareChangeRequest.Reason ?? "";
            BeforeHwManufacturerModel = hardwareChangeRequest.BeforeHwManufacturerModel ?? "";
            BeforeHwName = hardwareChangeRequest.BeforeHwName ?? "";
            BeforeHwOs = hardwareChangeRequest.BeforeHwOs ?? "";
            AfterHwManufacturerModel = hardwareChangeRequest.AfterHwManufacturerModel ?? "";
            AfterHwName = hardwareChangeRequest.AfterHwName ?? "";
            AfterHwOs = hardwareChangeRequest.AfterHwOs ?? "";
            WorkDescription = hardwareChangeRequest.WorkDescription ?? "";
            SecurityReviewComment = hardwareChangeRequest.SecurityReviewComment ?? "";
            CreatedDate = hardwareChangeRequest.CreatedDate == default ? DateTime.Now : hardwareChangeRequest.CreatedDate;
            
            // Set status flags based on the request status
            IsUnderReview = hardwareChangeRequest.Status == "Under Review" || hardwareChangeRequest.Status == "Submitted";
            IsApproved = hardwareChangeRequest.Status == "Approved";
            
            // Refresh UI properties
            OnPropertyChanged(nameof(IsAdminViewing));
            OnPropertyChanged(nameof(ShowSaveSubmitButtons));
            OnPropertyChanged(nameof(ShowApproveRejectButtons));
        }

        private async Task GenerateRequestNumberAsync()
        {
            try
            {
                RequestNumber = await _hardwareChangeRequestService.GenerateRequestNumberAsync();
            }
            catch (Exception ex)
            {
                // Fallback to simple format if service fails
                RequestNumber = GenerateRequestNumber();
                System.Diagnostics.Debug.WriteLine($"Error generating request number: {ex.Message}");
            }
        }

        private async void LoadShips()
        {
            try
            {
                Ships.Clear();
                var ships = await _shipService.GetAllShipsAsync();
                foreach (var ship in ships)
                {
                    Ships.Add(ship);
                }
                if (Ships.Count > 0)
                {
                    SelectedShip = Ships.First();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading ships: {ex.Message}");
            }
        }

        private string GenerateRequestNumber()
        {
            var today = DateTime.Now;
            return $"HW-{today:yyyyMM}-{today:ddHHmm}";
        }

        private async Task SaveAsync()
        {
            try
            {
                if (_hardwareChangeRequest == null)
                {
                    _hardwareChangeRequest = new HardwareChangeRequest
                    {
                        RequesterUserId = _authenticationService.CurrentUser?.Id ?? 0,
                        Department = Department,
                        PositionTitle = PositionTitle,
                        RequesterName = RequesterName,
                        InstalledCbs = InstalledCbs,
                        InstalledComponent = InstalledComponent,
                        Reason = Reason,
                        BeforeHwManufacturerModel = BeforeHwManufacturerModel,
                        BeforeHwName = BeforeHwName,
                        BeforeHwOs = BeforeHwOs,
                        AfterHwManufacturerModel = AfterHwManufacturerModel,
                        AfterHwName = AfterHwName,
                        AfterHwOs = AfterHwOs,
                        WorkDescription = WorkDescription,
                        SecurityReviewComment = SecurityReviewComment,
                        Status = "Draft",
                        CreatedDate = DateTime.Now
                    };
                    
                    _hardwareChangeRequest = await _hardwareChangeRequestService.CreateAsync(_hardwareChangeRequest);
                    RequestNumber = _hardwareChangeRequest.RequestNumber;
                    IsEditMode = true;
                    
                    // Also create a ChangeRequest record for the main UI
                    var changeRequest = new ChangeRequest
                    {
                        RequestNo = _hardwareChangeRequest.RequestNumber,
                        RequestTypeId = 1, // Hardware Change
                        StatusId = 1, // Draft
                        RequestedById = _authenticationService.CurrentUser?.Id ?? 1,
                        ShipId = SelectedShip?.Id, // Include selected ship
                        Purpose = Reason,
                        Description = $"Hardware Change: {BeforeHwName} → {AfterHwName}",
                        RequestedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _changeRequestService.CreateChangeRequestAsync(changeRequest);
                    
                    MessageBox.Show("Hardware change request saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _hardwareChangeRequest.Department = Department;
                    _hardwareChangeRequest.PositionTitle = PositionTitle;
                    _hardwareChangeRequest.RequesterName = RequesterName;
                    _hardwareChangeRequest.InstalledCbs = InstalledCbs;
                    _hardwareChangeRequest.InstalledComponent = InstalledComponent;
                    _hardwareChangeRequest.Reason = Reason;
                    _hardwareChangeRequest.BeforeHwManufacturerModel = BeforeHwManufacturerModel;
                    _hardwareChangeRequest.BeforeHwName = BeforeHwName;
                    _hardwareChangeRequest.BeforeHwOs = BeforeHwOs;
                    _hardwareChangeRequest.AfterHwManufacturerModel = AfterHwManufacturerModel;
                    _hardwareChangeRequest.AfterHwName = AfterHwName;
                    _hardwareChangeRequest.AfterHwOs = AfterHwOs;
                    _hardwareChangeRequest.WorkDescription = WorkDescription;
                    _hardwareChangeRequest.SecurityReviewComment = SecurityReviewComment;
                    
                    _hardwareChangeRequest = await _hardwareChangeRequestService.UpdateAsync(_hardwareChangeRequest);
                    
                    // Also update the corresponding ChangeRequest record
                    var changeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                    var correspondingChangeRequest = changeRequests.FirstOrDefault(cr => cr.RequestNo == _hardwareChangeRequest.RequestNumber);
                    if (correspondingChangeRequest != null)
                    {
                        correspondingChangeRequest.ShipId = SelectedShip?.Id; // Update selected ship
                        correspondingChangeRequest.Purpose = Reason;
                        correspondingChangeRequest.Description = $"Hardware Change: {BeforeHwName} → {AfterHwName}";
                        await _changeRequestService.UpdateChangeRequestAsync(correspondingChangeRequest);
                    }
                    
                    MessageBox.Show("Hardware change request updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                RequestSave?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving hardware change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SubmitAsync()
        {
            try
            {
                await SaveAsync(); // Save first
                
                if (_hardwareChangeRequest != null)
                {
                    await _hardwareChangeRequestService.SubmitForReviewAsync(_hardwareChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0);
                    
                    // Also update the corresponding ChangeRequest status
                    var changeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                    var correspondingChangeRequest = changeRequests.FirstOrDefault(cr => cr.RequestNo == _hardwareChangeRequest.RequestNumber);
                    if (correspondingChangeRequest != null)
                    {
                        await _changeRequestService.SubmitForApprovalAsync(correspondingChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0);
                    }
                    
                    IsUnderReview = true;
                    MessageBox.Show("Hardware change request submitted for review!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    RequestSave?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error submitting hardware change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ApproveAsync()
        {
            try
            {
                if (_hardwareChangeRequest != null)
                {
                    await _hardwareChangeRequestService.ApproveAsync(_hardwareChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0);
                    
                    // Update the corresponding ChangeRequest status
                    var changeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                    var correspondingChangeRequest = changeRequests.FirstOrDefault(cr => cr.RequestNo == _hardwareChangeRequest.RequestNumber);
                    if (correspondingChangeRequest != null)
                    {
                        await _changeRequestService.ApproveChangeRequestAsync(correspondingChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0);
                    }
                    
                    IsApproved = true;
                    IsUnderReview = false;
                    
                    MessageBox.Show("Hardware change request approved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh the UI properties
                    OnPropertyChanged(nameof(IsAdminViewing));
                    OnPropertyChanged(nameof(ShowSaveSubmitButtons));
                    OnPropertyChanged(nameof(ShowApproveRejectButtons));
                    
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error approving hardware change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RejectAsync()
        {
            try
            {
                if (_hardwareChangeRequest != null)
                {
                    await _hardwareChangeRequestService.RejectAsync(_hardwareChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0, "Rejected by administrator");
                    
                    // Update the corresponding ChangeRequest status
                    var changeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                    var correspondingChangeRequest = changeRequests.FirstOrDefault(cr => cr.RequestNo == _hardwareChangeRequest.RequestNumber);
                    if (correspondingChangeRequest != null)
                    {
                        await _changeRequestService.RejectChangeRequestAsync(correspondingChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0, "Rejected by administrator");
                    }
                    
                    IsApproved = false;
                    IsUnderReview = false;
                    
                    MessageBox.Show("Hardware change request rejected.", "Rejected", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh the UI properties
                    OnPropertyChanged(nameof(IsAdminViewing));
                    OnPropertyChanged(nameof(ShowSaveSubmitButtons));
                    OnPropertyChanged(nameof(ShowApproveRejectButtons));
                    
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error rejecting hardware change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private bool CanSave()
        {
            return !IsViewMode && !string.IsNullOrWhiteSpace(RequesterName) && 
                   !string.IsNullOrWhiteSpace(Reason);
        }

        private bool CanSubmit()
        {
            return !IsViewMode && CanSave() && 
                   !string.IsNullOrWhiteSpace(WorkDescription);
        }

        private bool CanApprove()
        {
            return IsAdminViewing && _hardwareChangeRequest != null;
        }

        private bool CanReject()
        {
            return IsAdminViewing && _hardwareChangeRequest != null;
        }

        #endregion

        #region INotifyPropertyChanged

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

        #endregion
    }
} 