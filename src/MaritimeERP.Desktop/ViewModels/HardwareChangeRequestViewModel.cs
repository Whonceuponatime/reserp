using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Desktop.ViewModels
{
    public class HardwareChangeRequestViewModel : ViewModelBase
    {
        private readonly IHardwareChangeRequestService _hardwareChangeRequestService;
        private readonly IAuthenticationService _authenticationService;

        public HardwareChangeRequestViewModel(
            IHardwareChangeRequestService hardwareChangeRequestService,
            IAuthenticationService authenticationService)
        {
            _hardwareChangeRequestService = hardwareChangeRequestService;
            _authenticationService = authenticationService;

            HardwareChangeRequests = new ObservableCollection<HardwareChangeRequest>();
            
            // Initialize commands
            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
            NewRequestCommand = new RelayCommand(NewRequest);
            SaveRequestCommand = new AsyncRelayCommand(SaveRequestAsync, CanSaveRequest);

            // Load data when initialized
            _ = LoadDataAsync();
        }

        #region Properties

        public ObservableCollection<HardwareChangeRequest> HardwareChangeRequests { get; }

        private HardwareChangeRequest? _selectedRequest;
        public HardwareChangeRequest? SelectedRequest
        {
            get => _selectedRequest;
            set
            {
                _selectedRequest = value;
                OnPropertyChanged();
                LoadRequestForEdit();
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFormEnabled));
            }
        }

        public bool IsFormEnabled => IsEditing;

        // Form Properties
        private string _requestNumber = string.Empty;
        public string RequestNumber
        {
            get => _requestNumber;
            set { _requestNumber = value; OnPropertyChanged(); }
        }

        private DateTime _createdDate = DateTime.Now;
        public DateTime CreatedDate
        {
            get => _createdDate;
            set { _createdDate = value; OnPropertyChanged(); }
        }

        private string _requesterName = string.Empty;
        public string RequesterName
        {
            get => _requesterName;
            set { _requesterName = value; OnPropertyChanged(); }
        }

        private string _department = string.Empty;
        public string Department
        {
            get => _department;
            set { _department = value; OnPropertyChanged(); }
        }

        private string _positionTitle = string.Empty;
        public string PositionTitle
        {
            get => _positionTitle;
            set { _positionTitle = value; OnPropertyChanged(); }
        }

        private string _installedCbs = string.Empty;
        public string InstalledCbs
        {
            get => _installedCbs;
            set { _installedCbs = value; OnPropertyChanged(); }
        }

        private string _installedComponent = string.Empty;
        public string InstalledComponent
        {
            get => _installedComponent;
            set { _installedComponent = value; OnPropertyChanged(); }
        }

        private string _reason = string.Empty;
        public string Reason
        {
            get => _reason;
            set { _reason = value; OnPropertyChanged(); }
        }

        private string _beforeHwManufacturerModel = string.Empty;
        public string BeforeHwManufacturerModel
        {
            get => _beforeHwManufacturerModel;
            set { _beforeHwManufacturerModel = value; OnPropertyChanged(); }
        }

        private string _beforeHwName = string.Empty;
        public string BeforeHwName
        {
            get => _beforeHwName;
            set { _beforeHwName = value; OnPropertyChanged(); }
        }

        private string _beforeHwOs = string.Empty;
        public string BeforeHwOs
        {
            get => _beforeHwOs;
            set { _beforeHwOs = value; OnPropertyChanged(); }
        }

        private string _afterHwManufacturerModel = string.Empty;
        public string AfterHwManufacturerModel
        {
            get => _afterHwManufacturerModel;
            set { _afterHwManufacturerModel = value; OnPropertyChanged(); }
        }

        private string _afterHwName = string.Empty;
        public string AfterHwName
        {
            get => _afterHwName;
            set { _afterHwName = value; OnPropertyChanged(); }
        }

        private string _afterHwOs = string.Empty;
        public string AfterHwOs
        {
            get => _afterHwOs;
            set { _afterHwOs = value; OnPropertyChanged(); }
        }

        private string _workDescription = string.Empty;
        public string WorkDescription
        {
            get => _workDescription;
            set { _workDescription = value; OnPropertyChanged(); }
        }

        private string _securityReviewComment = string.Empty;
        public string SecurityReviewComment
        {
            get => _securityReviewComment;
            set { _securityReviewComment = value; OnPropertyChanged(); }
        }

        private string _status = "Draft";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand NewRequestCommand { get; }
        public ICommand SaveRequestCommand { get; }

        #endregion

        #region Methods

        private async Task LoadDataAsync()
        {
            try
            {
                var requests = await _hardwareChangeRequestService.GetAllAsync();
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    HardwareChangeRequests.Clear();
                    foreach (var request in requests)
                    {
                        HardwareChangeRequests.Add(request);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading hardware change requests: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewRequest()
        {
            ClearForm();
            IsEditing = true;
            RequestNumber = ""; // Will be generated on save
            CreatedDate = DateTime.Now;
            
            // Set current user as requester
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                RequesterName = currentUser.FullName;
            }
        }

        private void LoadRequestForEdit()
        {
            if (SelectedRequest == null)
            {
                ClearForm();
                return;
            }

            RequestNumber = SelectedRequest.RequestNumber;
            CreatedDate = SelectedRequest.CreatedDate;
            Department = SelectedRequest.Department ?? "";
            PositionTitle = SelectedRequest.PositionTitle ?? "";
            RequesterName = SelectedRequest.RequesterName ?? "";
            InstalledCbs = SelectedRequest.InstalledCbs ?? "";
            InstalledComponent = SelectedRequest.InstalledComponent ?? "";
            Reason = SelectedRequest.Reason ?? "";
            BeforeHwManufacturerModel = SelectedRequest.BeforeHwManufacturerModel ?? "";
            BeforeHwName = SelectedRequest.BeforeHwName ?? "";
            BeforeHwOs = SelectedRequest.BeforeHwOs ?? "";
            AfterHwManufacturerModel = SelectedRequest.AfterHwManufacturerModel ?? "";
            AfterHwName = SelectedRequest.AfterHwName ?? "";
            AfterHwOs = SelectedRequest.AfterHwOs ?? "";
            WorkDescription = SelectedRequest.WorkDescription ?? "";
            SecurityReviewComment = SelectedRequest.SecurityReviewComment ?? "";
            Status = SelectedRequest.Status;
        }

        private void ClearForm()
        {
            RequestNumber = "";
            CreatedDate = DateTime.Now;
            Department = "";
            PositionTitle = "";
            RequesterName = "";
            InstalledCbs = "";
            InstalledComponent = "";
            Reason = "";
            BeforeHwManufacturerModel = "";
            BeforeHwName = "";
            BeforeHwOs = "";
            AfterHwManufacturerModel = "";
            AfterHwName = "";
            AfterHwOs = "";
            WorkDescription = "";
            SecurityReviewComment = "";
            Status = "Draft";
        }

        private async Task SaveRequestAsync()
        {
            try
            {
                var currentUser = _authenticationService.CurrentUser;
                if (currentUser == null)
                {
                    MessageBox.Show("Please log in to save requests.", "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                HardwareChangeRequest request;
                
                if (SelectedRequest == null)
                {
                    // Create new request
                    request = new HardwareChangeRequest
                    {
                        RequesterUserId = currentUser.Id,
                        CreatedDate = CreatedDate,
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
                        Status = Status
                    };
                    
                    request = await _hardwareChangeRequestService.CreateAsync(request);
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        HardwareChangeRequests.Add(request);
                        SelectedRequest = request;
                    });
                }
                else
                {
                    // Update existing request
                    SelectedRequest.Department = Department;
                    SelectedRequest.PositionTitle = PositionTitle;
                    SelectedRequest.RequesterName = RequesterName;
                    SelectedRequest.InstalledCbs = InstalledCbs;
                    SelectedRequest.InstalledComponent = InstalledComponent;
                    SelectedRequest.Reason = Reason;
                    SelectedRequest.BeforeHwManufacturerModel = BeforeHwManufacturerModel;
                    SelectedRequest.BeforeHwName = BeforeHwName;
                    SelectedRequest.BeforeHwOs = BeforeHwOs;
                    SelectedRequest.AfterHwManufacturerModel = AfterHwManufacturerModel;
                    SelectedRequest.AfterHwName = AfterHwName;
                    SelectedRequest.AfterHwOs = AfterHwOs;
                    SelectedRequest.WorkDescription = WorkDescription;
                    SelectedRequest.SecurityReviewComment = SecurityReviewComment;
                    
                    await _hardwareChangeRequestService.UpdateAsync(SelectedRequest);
                }

                IsEditing = false;
                MessageBox.Show("Hardware change request saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving hardware change request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Command Validation

        private bool CanSaveRequest()
        {
            return IsEditing && !string.IsNullOrWhiteSpace(RequesterName) && !string.IsNullOrWhiteSpace(Reason);
        }

        #endregion
    }
} 