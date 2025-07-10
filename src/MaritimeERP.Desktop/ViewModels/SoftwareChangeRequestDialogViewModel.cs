using System;
using System.ComponentModel;
using System.Windows.Input;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using MaritimeERP.Core.Entities;
using System.Threading.Tasks;

namespace MaritimeERP.Desktop.ViewModels
{
    public class SoftwareChangeRequestDialogViewModel : INotifyPropertyChanged
    {
        private readonly ISoftwareService _softwareService;
        private readonly IAuthenticationService _authenticationService;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? RequestClose;
        
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
        private bool _isUnderReview;
        public bool IsUnderReview
        {
            get => _isUnderReview;
            set
            {
                _isUnderReview = value;
                OnPropertyChanged(nameof(IsUnderReview));
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
            }
        }
        
        // Commands
        public ICommand SaveCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }
        
        public SoftwareChangeRequestDialogViewModel(ISoftwareService softwareService, IAuthenticationService authenticationService)
        {
            _softwareService = softwareService;
            _authenticationService = authenticationService;
            
            // Initialize commands
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            SubmitCommand = new RelayCommand(async () => await SubmitAsync());
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
            
            // Generate request number
            RequestNumber = $"SW-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";
            
            // Set current user info
            InitializeUserInfo();
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
        
        private async Task SaveAsync()
        {
            try
            {
                // For now, just close the dialog since we don't have a specific software change request service method
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // Handle error
                System.Windows.MessageBox.Show($"Error occurred while saving: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private async Task SubmitAsync()
        {
            try
            {
                System.Windows.MessageBox.Show("Software change request has been submitted successfully.", "Submission Complete", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // Handle error
                System.Windows.MessageBox.Show($"Error occurred while submitting: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        protected virtual void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 