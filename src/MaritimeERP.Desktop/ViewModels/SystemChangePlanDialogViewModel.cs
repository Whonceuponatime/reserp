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
    public class SystemChangePlanDialogViewModel : INotifyPropertyChanged
    {
        private readonly ISystemChangePlanService _systemChangePlanService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IShipService _shipService;
        private readonly IChangeRequestService _changeRequestService;
        private SystemChangePlan _systemChangePlan;
        private bool _isEditMode;

        public SystemChangePlanDialogViewModel(
            ISystemChangePlanService systemChangePlanService,
            IAuthenticationService authenticationService,
            IShipService shipService,
            IChangeRequestService changeRequestService)
        {
            _systemChangePlanService = systemChangePlanService;
            _authenticationService = authenticationService;
            _shipService = shipService;
            _changeRequestService = changeRequestService;
            _systemChangePlan = new SystemChangePlan();
            
            Ships = new ObservableCollection<Ship>();
            
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
            SubmitCommand = new RelayCommand(async () => await SubmitAsync(), () => CanSubmit());
            CancelCommand = new RelayCommand(() => CloseDialog());
            
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
                _systemChangePlan.ShipId = value?.Id;
            }
        }

        public SystemChangePlan SystemChangePlan
        {
            get => _systemChangePlan;
            set
            {
                _systemChangePlan = value;
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

        // Properties bound to UI
        public string RequestNumber
        {
            get => _systemChangePlan.RequestNumber;
            set
            {
                _systemChangePlan.RequestNumber = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreatedDate
        {
            get => _systemChangePlan.CreatedDate;
            set
            {
                _systemChangePlan.CreatedDate = value;
                OnPropertyChanged();
            }
        }

        public bool IsUnderReview
        {
            get => _systemChangePlan.IsUnderReview;
            set
            {
                _systemChangePlan.IsUnderReview = value;
                OnPropertyChanged();
            }
        }

        public bool IsApproved
        {
            get => _systemChangePlan.IsApproved;
            set
            {
                _systemChangePlan.IsApproved = value;
                OnPropertyChanged();
            }
        }

        public string Department
        {
            get => _systemChangePlan.Department;
            set
            {
                _systemChangePlan.Department = value;
                OnPropertyChanged();
            }
        }

        public string PositionTitle
        {
            get => _systemChangePlan.PositionTitle;
            set
            {
                _systemChangePlan.PositionTitle = value;
                OnPropertyChanged();
            }
        }

        public string RequesterName
        {
            get => _systemChangePlan.RequesterName;
            set
            {
                _systemChangePlan.RequesterName = value;
                OnPropertyChanged();
            }
        }

        public string InstalledCbs
        {
            get => _systemChangePlan.InstalledCbs;
            set
            {
                _systemChangePlan.InstalledCbs = value;
                OnPropertyChanged();
            }
        }

        public string InstalledComponent
        {
            get => _systemChangePlan.InstalledComponent;
            set
            {
                _systemChangePlan.InstalledComponent = value;
                OnPropertyChanged();
            }
        }

        public string Reason
        {
            get => _systemChangePlan.Reason;
            set
            {
                _systemChangePlan.Reason = value;
                OnPropertyChanged();
            }
        }

        public string BeforeManufacturerModel
        {
            get => _systemChangePlan.BeforeManufacturerModel;
            set
            {
                _systemChangePlan.BeforeManufacturerModel = value;
                OnPropertyChanged();
            }
        }

        public string BeforeHwSwName
        {
            get => _systemChangePlan.BeforeHwSwName;
            set
            {
                _systemChangePlan.BeforeHwSwName = value;
                OnPropertyChanged();
            }
        }

        public string BeforeVersion
        {
            get => _systemChangePlan.BeforeVersion;
            set
            {
                _systemChangePlan.BeforeVersion = value;
                OnPropertyChanged();
            }
        }

        public string AfterManufacturerModel
        {
            get => _systemChangePlan.AfterManufacturerModel;
            set
            {
                _systemChangePlan.AfterManufacturerModel = value;
                OnPropertyChanged();
            }
        }

        public string AfterHwSwName
        {
            get => _systemChangePlan.AfterHwSwName;
            set
            {
                _systemChangePlan.AfterHwSwName = value;
                OnPropertyChanged();
            }
        }

        public string AfterVersion
        {
            get => _systemChangePlan.AfterVersion;
            set
            {
                _systemChangePlan.AfterVersion = value;
                OnPropertyChanged();
            }
        }

        public string PlanDetails
        {
            get => _systemChangePlan.PlanDetails;
            set
            {
                _systemChangePlan.PlanDetails = value;
                OnPropertyChanged();
            }
        }

        public string SecurityReviewComments
        {
            get => _systemChangePlan.SecurityReviewComments;
            set
            {
                _systemChangePlan.SecurityReviewComments = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action? RequestClose;

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
                MessageBox.Show($"Error initializing system change plan dialog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (string.IsNullOrEmpty(_systemChangePlan.RequestNumber))
                {
                    var requestNumber = await _systemChangePlanService.GenerateRequestNumberAsync();
                    RequestNumber = requestNumber;
                }
            }
            catch (Exception ex)
            {
                // Fallback to timestamp-based number if service fails
                RequestNumber = $"SYS-{DateTime.Now:yyyyMM}-{DateTime.Now:HHmmss}";
                System.Diagnostics.Debug.WriteLine($"Failed to generate request number: {ex.Message}");
            }
        }

        private void InitializeNewRequest()
        {
            var currentUser = _authenticationService.CurrentUser;
            if (currentUser != null)
            {
                // Use available properties from User entity
                RequesterName = currentUser.FullName ?? string.Empty;
                // Set default values for Department and Position since they're not in User entity
                Department = "Engineering"; // Default value
                PositionTitle = "Engineer"; // Default value
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                // Set the user ID
                _systemChangePlan.UserId = _authenticationService.CurrentUser?.Id;
                
                if (IsEditMode)
                {
                    await _systemChangePlanService.UpdateSystemChangePlanAsync(_systemChangePlan);
                    MessageBox.Show("System change plan saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Create the detailed SystemChangePlan record
                    await _systemChangePlanService.CreateSystemChangePlanAsync(_systemChangePlan);
                    
                    // Create the main ChangeRequest record for the table
                    var changeRequest = new ChangeRequest
                    {
                        RequestNo = _systemChangePlan.RequestNumber,
                        ShipId = _systemChangePlan.ShipId,
                        RequestTypeId = 3, // System Plan
                        StatusId = 1, // Draft
                        RequestedById = _authenticationService.CurrentUser?.Id ?? 1,
                        Purpose = _systemChangePlan.Reason,
                        Description = $"System Plan: {_systemChangePlan.BeforeHwSwName} → {_systemChangePlan.AfterHwSwName}",
                        RequestedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _changeRequestService.CreateChangeRequestAsync(changeRequest);
                    
                    MessageBox.Show("System change plan created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    IsEditMode = true;
                }

                // Close dialog after successful save
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving system change plan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SubmitAsync()
        {
            try
            {
                _systemChangePlan.IsUnderReview = true;
                await _systemChangePlanService.UpdateSystemChangePlanAsync(_systemChangePlan);
                MessageBox.Show("System change plan submitted for review!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error submitting system change plan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDialog()
        {
            RequestClose?.Invoke();
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(RequesterName) &&
                   !string.IsNullOrWhiteSpace(Department);
        }

        private bool CanSubmit()
        {
            return CanSave() && !string.IsNullOrWhiteSpace(Reason);
        }

        private void UpdateAllProperties()
        {
            OnPropertyChanged(nameof(RequestNumber));
            OnPropertyChanged(nameof(CreatedDate));
            OnPropertyChanged(nameof(IsUnderReview));
            OnPropertyChanged(nameof(IsApproved));
            OnPropertyChanged(nameof(Department));
            OnPropertyChanged(nameof(PositionTitle));
            OnPropertyChanged(nameof(RequesterName));
            OnPropertyChanged(nameof(InstalledCbs));
            OnPropertyChanged(nameof(InstalledComponent));
            OnPropertyChanged(nameof(Reason));
            OnPropertyChanged(nameof(BeforeManufacturerModel));
            OnPropertyChanged(nameof(BeforeHwSwName));
            OnPropertyChanged(nameof(BeforeVersion));
            OnPropertyChanged(nameof(AfterManufacturerModel));
            OnPropertyChanged(nameof(AfterHwSwName));
            OnPropertyChanged(nameof(AfterVersion));
            OnPropertyChanged(nameof(PlanDetails));
            OnPropertyChanged(nameof(SecurityReviewComments));
            
            // Load selected ship if editing
            if (IsEditMode && _systemChangePlan.ShipId.HasValue)
            {
                SelectedShip = Ships.FirstOrDefault(s => s.Id == _systemChangePlan.ShipId.Value);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 