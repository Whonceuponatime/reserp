

using System;
using System.ComponentModel;
using System.Windows.Input;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using MaritimeERP.Core.Entities;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Collections.ObjectModel;

namespace MaritimeERP.Desktop.ViewModels
{
    public class SoftwareChangeRequestDialogViewModel : INotifyPropertyChanged
    {
        private readonly ISoftwareChangeRequestService _softwareChangeRequestService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IChangeRequestService _changeRequestService;
        private readonly IShipService _shipService;
        private SoftwareChangeRequest? _softwareChangeRequest;
        private bool _isEditMode;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? RequestClose;
        public event EventHandler? RequestSave;
        
        // Ship selection properties
        public ObservableCollection<Ship> Ships { get; } = new ObservableCollection<Ship>();
        
        private Ship? _selectedShip;
        public Ship? SelectedShip
        {
            get => _selectedShip;
            set
            {
                _selectedShip = value;
                OnPropertyChanged(nameof(SelectedShip));
            }
        }
        
        // Properties matching the Korean form
        private string _requestNumber = string.Empty;
        public string RequestNumber
        {
            get => _requestNumber;
            set
            {
                _requestNumber = value;
                OnPropertyChanged(nameof(RequestNumber));
            }
        }
        
        private DateTime _createdDate = DateTime.Now;
        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged(nameof(CreatedDate));
            }
        }
        
        private string _department = string.Empty;
        public string Department
        {
            get => _department;
            set
            {
                _department = value;
                OnPropertyChanged(nameof(Department));
            }
        }
        
        private string _positionTitle = string.Empty;
        public string PositionTitle
        {
            get => _positionTitle;
            set
            {
                _positionTitle = value;
                OnPropertyChanged(nameof(PositionTitle));
            }
        }
        
        private string _requesterName = string.Empty;
        public string RequesterName
        {
            get => _requesterName;
            set
            {
                _requesterName = value;
                OnPropertyChanged(nameof(RequesterName));
            }
        }
        
        private string _installedCbs = string.Empty;
        public string InstalledCbs
        {
            get => _installedCbs;
            set
            {
                _installedCbs = value;
                OnPropertyChanged(nameof(InstalledCbs));
            }
        }
        
        private string _installedComponent = string.Empty;
        public string InstalledComponent
        {
            get => _installedComponent;
            set
            {
                _installedComponent = value;
                OnPropertyChanged(nameof(InstalledComponent));
            }
        }
        
        private string _reason = string.Empty;
        public string Reason
        {
            get => _reason;
            set
            {
                _reason = value;
                OnPropertyChanged(nameof(Reason));
            }
        }
        
        // Before SW
        private string _beforeSwManufacturer = string.Empty;
        public string BeforeSwManufacturer
        {
            get => _beforeSwManufacturer;
            set
            {
                _beforeSwManufacturer = value;
                OnPropertyChanged(nameof(BeforeSwManufacturer));
            }
        }
        
        private string _beforeSwName = string.Empty;
        public string BeforeSwName
        {
            get => _beforeSwName;
            set
            {
                _beforeSwName = value;
                OnPropertyChanged(nameof(BeforeSwName));
            }
        }
        
        private string _beforeSwVersion = string.Empty;
        public string BeforeSwVersion
        {
            get => _beforeSwVersion;
            set
            {
                _beforeSwVersion = value;
                OnPropertyChanged(nameof(BeforeSwVersion));
            }
        }
        
        // After SW
        private string _afterSwManufacturer = string.Empty;
        public string AfterSwManufacturer
        {
            get => _afterSwManufacturer;
            set
            {
                _afterSwManufacturer = value;
                OnPropertyChanged(nameof(AfterSwManufacturer));
            }
        }
        
        private string _afterSwName = string.Empty;
        public string AfterSwName
        {
            get => _afterSwName;
            set
            {
                _afterSwName = value;
                OnPropertyChanged(nameof(AfterSwName));
            }
        }
        
        private string _afterSwVersion = string.Empty;
        public string AfterSwVersion
        {
            get => _afterSwVersion;
            set
            {
                _afterSwVersion = value;
                OnPropertyChanged(nameof(AfterSwVersion));
            }
        }
        
        private string _workDescription = string.Empty;
        public string WorkDescription
        {
            get => _workDescription;
            set
            {
                _workDescription = value;
                OnPropertyChanged(nameof(WorkDescription));
            }
        }
        
        private string _securityReviewComment = string.Empty;
        public string SecurityReviewComment
        {
            get => _securityReviewComment;
            set
            {
                _securityReviewComment = value;
                OnPropertyChanged(nameof(SecurityReviewComment));
            }
        }
        
        // Status properties
        public bool IsInDraftStatus
        {
            get => !_isUnderReview && !_isApproved;
        }
        
        private bool _isUnderReview;
        public bool IsUnderReview
        {
            get => _isUnderReview;
            set
            {
                _isUnderReview = value;
                OnPropertyChanged(nameof(IsUnderReview));
                OnPropertyChanged(nameof(IsInDraftStatus));
            }
        }
        
        private bool _isApproved;
        public bool IsApproved
        {
            get => _isApproved;
            set
            {
                _isApproved = value;
                OnPropertyChanged(nameof(IsApproved));
                OnPropertyChanged(nameof(IsInDraftStatus));
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged(nameof(IsEditMode));
            }
        }

        private bool _isViewMode;
        public bool IsViewMode
        {
            get => _isViewMode;
            set
            {
                _isViewMode = value;
                OnPropertyChanged(nameof(IsViewMode));
            }
        }
        
        // Admin-specific properties
        public bool IsAdmin => _authenticationService.CurrentUser?.Role?.Name == "Administrator";
        public bool IsAdminViewing => IsAdmin && IsViewMode && IsUnderReview;
        public bool ShowSaveSubmitButtons => !IsAdminViewing;
        public bool ShowApproveRejectButtons => IsAdminViewing;
        
        // Commands
        public ICommand SaveCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }
        
        public SoftwareChangeRequestDialogViewModel(ISoftwareChangeRequestService softwareChangeRequestService, IAuthenticationService authenticationService, IChangeRequestService changeRequestService, IShipService shipService)
        {
            _softwareChangeRequestService = softwareChangeRequestService;
            _authenticationService = authenticationService;
            _changeRequestService = changeRequestService;
            _shipService = shipService;
            
            // Initialize commands
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            SubmitCommand = new RelayCommand(async () => await SubmitAsync());
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
            ApproveCommand = new RelayCommand(async () => await ApproveAsync());
            RejectCommand = new RelayCommand(async () => await RejectAsync());
            
            // Generate request number immediately
            _ = GenerateRequestNumberAsync();
            
            // Load ships
            LoadShips();
            
            // Set current user info
            InitializeUserInfo();
        }
        
        private async Task GenerateRequestNumberAsync()
        {
            try
            {
                RequestNumber = await _softwareChangeRequestService.GenerateRequestNumberAsync();
            }
            catch (Exception ex)
            {
                // Fallback to simple format if service fails
                RequestNumber = $"SW-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";
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

        private void InitializeUserInfo()
        {
            try
            {
                var currentUser = _authenticationService.CurrentUser;
                if (currentUser != null)
                {
                    RequesterName = currentUser.FullName;
                    Department = "IT Department"; // Default value since User entity doesn't have Department
                    PositionTitle = currentUser.Role?.Name ?? "User";
                }
            }
            catch (Exception ex)
            {
                // Handle error silently or log
                System.Diagnostics.Debug.WriteLine($"Error initializing user info: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets an existing software change request for editing
        /// </summary>
        /// <param name="softwareChangeRequest">The existing software change request to edit</param>
        public void SetExistingSoftwareChangeRequest(SoftwareChangeRequest softwareChangeRequest)
        {
            _softwareChangeRequest = softwareChangeRequest;
            _isEditMode = true;
            IsEditMode = true;
            
            // Populate the form fields with existing data
            RequestNumber = softwareChangeRequest.RequestNumber;
            CreatedDate = softwareChangeRequest.CreatedDate;
            Department = softwareChangeRequest.Department ?? "";
            PositionTitle = softwareChangeRequest.PositionTitle ?? "";
            RequesterName = softwareChangeRequest.RequesterName ?? "";
            InstalledCbs = softwareChangeRequest.InstalledCbs ?? "";
            InstalledComponent = softwareChangeRequest.InstalledComponent ?? "";
            Reason = softwareChangeRequest.Reason ?? "";
            BeforeSwManufacturer = softwareChangeRequest.BeforeSwManufacturer ?? "";
            BeforeSwName = softwareChangeRequest.BeforeSwName ?? "";
            BeforeSwVersion = softwareChangeRequest.BeforeSwVersion ?? "";
            AfterSwManufacturer = softwareChangeRequest.AfterSwManufacturer ?? "";
            AfterSwName = softwareChangeRequest.AfterSwName ?? "";
            AfterSwVersion = softwareChangeRequest.AfterSwVersion ?? "";
            WorkDescription = softwareChangeRequest.WorkDescription ?? "";
            SecurityReviewComment = softwareChangeRequest.SecurityReviewComment ?? "";
            
            // Set status indicators
            IsUnderReview = softwareChangeRequest.Status == "Under Review" || softwareChangeRequest.Status == "Submitted";
            IsApproved = softwareChangeRequest.Status == "Approved";
            
            // Refresh UI properties
            OnPropertyChanged(nameof(IsAdminViewing));
            OnPropertyChanged(nameof(ShowSaveSubmitButtons));
            OnPropertyChanged(nameof(ShowApproveRejectButtons));
        }
        
        private async Task SaveAsync()
        {
            try
            {
                if (_softwareChangeRequest == null)
                {
                    _softwareChangeRequest = new SoftwareChangeRequest
                    {
                        RequesterUserId = _authenticationService.CurrentUser?.Id ?? 0,
                        Department = Department,
                        PositionTitle = PositionTitle,
                        RequesterName = RequesterName,
                        InstalledCbs = InstalledCbs,
                        InstalledComponent = InstalledComponent,
                        Reason = Reason,
                        BeforeSwManufacturer = BeforeSwManufacturer,
                        BeforeSwName = BeforeSwName,
                        BeforeSwVersion = BeforeSwVersion,
                        AfterSwManufacturer = AfterSwManufacturer,
                        AfterSwName = AfterSwName,
                        AfterSwVersion = AfterSwVersion,
                        WorkDescription = WorkDescription,
                        SecurityReviewComment = SecurityReviewComment,
                        Status = "Draft",
                        CreatedDate = DateTime.Now
                    };
                    
                    _softwareChangeRequest = await _softwareChangeRequestService.CreateAsync(_softwareChangeRequest);
                    RequestNumber = _softwareChangeRequest.RequestNumber;
                    IsEditMode = true;
                    
                    // Also create a ChangeRequest record for the main UI
                    var changeRequest = new ChangeRequest
                    {
                        RequestNo = _softwareChangeRequest.RequestNumber,
                        RequestTypeId = 2, // Software Change
                        StatusId = 1, // Draft
                        RequestedById = _authenticationService.CurrentUser?.Id ?? 1,
                        ShipId = SelectedShip?.Id, // Include selected ship
                        Purpose = Reason,
                        Description = $"Software Change: {BeforeSwName} → {AfterSwName}",
                        RequestedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _changeRequestService.CreateChangeRequestAsync(changeRequest);
                    
                    MessageBox.Show("Software change request saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _softwareChangeRequest.Department = Department;
                    _softwareChangeRequest.PositionTitle = PositionTitle;
                    _softwareChangeRequest.RequesterName = RequesterName;
                    _softwareChangeRequest.InstalledCbs = InstalledCbs;
                    _softwareChangeRequest.InstalledComponent = InstalledComponent;
                    _softwareChangeRequest.Reason = Reason;
                    _softwareChangeRequest.BeforeSwManufacturer = BeforeSwManufacturer;
                    _softwareChangeRequest.BeforeSwName = BeforeSwName;
                    _softwareChangeRequest.BeforeSwVersion = BeforeSwVersion;
                    _softwareChangeRequest.AfterSwManufacturer = AfterSwManufacturer;
                    _softwareChangeRequest.AfterSwName = AfterSwName;
                    _softwareChangeRequest.AfterSwVersion = AfterSwVersion;
                    _softwareChangeRequest.WorkDescription = WorkDescription;
                    _softwareChangeRequest.SecurityReviewComment = SecurityReviewComment;
                    
                    _softwareChangeRequest = await _softwareChangeRequestService.UpdateAsync(_softwareChangeRequest);
                    
                    // Also update the corresponding ChangeRequest record
                    var changeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                    var correspondingChangeRequest = changeRequests.FirstOrDefault(cr => cr.RequestNo == _softwareChangeRequest.RequestNumber);
                    if (correspondingChangeRequest != null)
                    {
                        correspondingChangeRequest.ShipId = SelectedShip?.Id; // Update selected ship
                        correspondingChangeRequest.Purpose = Reason;
                        correspondingChangeRequest.Description = $"Software Change: {BeforeSwName} → {AfterSwName}";
                        await _changeRequestService.UpdateChangeRequestAsync(correspondingChangeRequest);
                    }
                    
                    MessageBox.Show("Software change request updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                // Trigger both events to indicate success and close the dialog
                RequestSave?.Invoke(this, EventArgs.Empty);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving software change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async Task ApproveAsync()
        {
            try
            {
                if (_softwareChangeRequest != null)
                {
                    await _softwareChangeRequestService.ApproveAsync(_softwareChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0);
                    
                    // Update the corresponding ChangeRequest status
                    var changeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                    var correspondingChangeRequest = changeRequests.FirstOrDefault(cr => cr.RequestNo == _softwareChangeRequest.RequestNumber);
                    if (correspondingChangeRequest != null)
                    {
                        await _changeRequestService.ApproveChangeRequestAsync(correspondingChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0);
                    }
                    
                    IsApproved = true;
                    IsUnderReview = false;
                    
                    MessageBox.Show("Software change request approved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh the UI properties
                    OnPropertyChanged(nameof(IsAdminViewing));
                    OnPropertyChanged(nameof(ShowSaveSubmitButtons));
                    OnPropertyChanged(nameof(ShowApproveRejectButtons));
                    
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error approving software change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RejectAsync()
        {
            try
            {
                if (_softwareChangeRequest != null)
                {
                    await _softwareChangeRequestService.RejectAsync(_softwareChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0, "Rejected by administrator");
                    
                    // Update the corresponding ChangeRequest status
                    var changeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                    var correspondingChangeRequest = changeRequests.FirstOrDefault(cr => cr.RequestNo == _softwareChangeRequest.RequestNumber);
                    if (correspondingChangeRequest != null)
                    {
                        await _changeRequestService.RejectChangeRequestAsync(correspondingChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0, "Rejected by administrator");
                    }
                    
                    IsApproved = false;
                    IsUnderReview = false;
                    
                    MessageBox.Show("Software change request rejected.", "Rejected", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh the UI properties
                    OnPropertyChanged(nameof(IsAdminViewing));
                    OnPropertyChanged(nameof(ShowSaveSubmitButtons));
                    OnPropertyChanged(nameof(ShowApproveRejectButtons));
                    
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error rejecting software change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async Task SubmitAsync()
        {
            try
            {
                await SaveAsync(); // Save first
                
                if (_softwareChangeRequest != null)
                {
                    await _softwareChangeRequestService.SubmitForReviewAsync(_softwareChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0);
                    
                    // Also update the corresponding ChangeRequest status
                    var changeRequests = await _changeRequestService.GetAllChangeRequestsAsync();
                    var correspondingChangeRequest = changeRequests.FirstOrDefault(cr => cr.RequestNo == _softwareChangeRequest.RequestNumber);
                    if (correspondingChangeRequest != null)
                    {
                        await _changeRequestService.SubmitForApprovalAsync(correspondingChangeRequest.Id, _authenticationService.CurrentUser?.Id ?? 0);
                    }
                    
                    IsUnderReview = true;
                    MessageBox.Show("Software change request submitted for review!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    RequestSave?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error submitting software change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        protected virtual void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 