using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Desktop.ViewModels
{
    public class HardwareChangeRequestDialogViewModel : INotifyPropertyChanged
    {
        private readonly IAuthenticationService _authenticationService;

        public HardwareChangeRequestDialogViewModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
            
            // Initialize with current user data
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                RequesterName = currentUser.FullName;
            }
            
            CreatedDate = DateTime.Now;
            RequestNumber = GenerateRequestNumber();
            
            // Initialize commands
            SaveCommand = new RelayCommand(Save, CanSave);
            SubmitCommand = new RelayCommand(Submit, CanSubmit);
            CancelCommand = new RelayCommand(Cancel);
        }

        #region Properties

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

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Events

        public event EventHandler? RequestClose;
        public event EventHandler? RequestSave;

        #endregion

        #region Methods

        private string GenerateRequestNumber()
        {
            var today = DateTime.Now;
            return $"HW-{today:yyyyMM}-{today:ddHHmm}";
        }

        private void Save()
        {
            // TODO: Implement save logic
            // For now, just close the dialog with success
            RequestSave?.Invoke(this, EventArgs.Empty);
        }

        private void Submit()
        {
            // TODO: Implement submit logic
            IsUnderReview = true;
            RequestSave?.Invoke(this, EventArgs.Empty);
        }

        private void Cancel()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(RequesterName) && 
                   !string.IsNullOrWhiteSpace(Reason);
        }

        private bool CanSubmit()
        {
            return CanSave() && 
                   !string.IsNullOrWhiteSpace(WorkDescription);
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