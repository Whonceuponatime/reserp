using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Windows;
using ComponentEntity = MaritimeERP.Core.Entities.Component;
using System.Threading.Tasks;
using System.Linq;

namespace MaritimeERP.Desktop.ViewModels
{
    public class SoftwareViewModel : ViewModelBase
    {
        private readonly ISoftwareService _softwareService;
        private readonly IComponentService _componentService;
        private readonly ISystemService _systemService;
        private readonly IShipService _shipService;
        private readonly INavigationService _navigationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<SoftwareViewModel> _logger;

        // Collections
        public ObservableCollection<Software> SoftwareList { get; } = new();
        public ObservableCollection<ComponentEntity> AvailableComponents { get; } = new();
        public ObservableCollection<ShipSystem> Systems { get; set; } = new();
        public ObservableCollection<Ship> Ships { get; set; } = new();
        public ObservableCollection<string> Manufacturers { get; set; } = new();
        public ObservableCollection<string> SoftwareTypes { get; set; } = new();

        // Selected items
        private Software? _selectedSoftware;
        public Software? SelectedSoftware
        {
            get => _selectedSoftware;
            set
            {
                if (SetProperty(ref _selectedSoftware, value))
                {
                    if (value != null && !IsEditing)
                    {
                        SoftwareName = value.Name;
                        Version = value.Version;
                        Manufacturer = value.Manufacturer;
                        SoftwareType = value.SoftwareType;
                        FunctionPurpose = value.FunctionPurpose;
                        InstalledHardwareComponent = value.InstalledHardwareComponent;
                        
                        // Find and set the selected component
                        if (value.InstalledComponentId.HasValue)
                        {
                            SelectedComponent = AvailableComponents.FirstOrDefault(c => c.Id == value.InstalledComponentId.Value);
                        }
                        else
                        {
                            SelectedComponent = null;
                        }
                    }
                }
            }
        }

        private ComponentEntity? _selectedComponent;
        public ComponentEntity? SelectedComponent
        {
            get => _selectedComponent;
            set => SetProperty(ref _selectedComponent, value);
        }

        private ShipSystem? _selectedSystem;
        public ShipSystem? SelectedSystem
        {
            get => _selectedSystem;
            set
            {
                _selectedSystem = value;
                OnPropertyChanged();
                _ = UpdateComponentsForSystemAsync();
            }
        }

        private Ship? _selectedShip;
        public Ship? SelectedShip
        {
            get => _selectedShip;
            set
            {
                _selectedShip = value;
                OnPropertyChanged();
                _ = UpdateSystemsForShipAsync();
            }
        }

        // Search and filter properties
        private string _searchTerm = string.Empty;
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (SetProperty(ref _searchTerm, value))
                {
                    SearchSoftwareAsync().ConfigureAwait(false);
                }
            }
        }

        // Form properties for current software
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        private int _softwareId;
        public int SoftwareId
        {
            get => _softwareId;
            set
            {
                _softwareId = value;
                OnPropertyChanged();
            }
        }

        private int _componentId;
        public int ComponentId
        {
            get => _componentId;
            set
            {
                _componentId = value;
                OnPropertyChanged();
            }
        }



        private string _softwareName = string.Empty;
        public string SoftwareName
        {
            get => _softwareName;
            set => SetProperty(ref _softwareName, value);
        }

        private string _version = string.Empty;
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        private string _manufacturer = string.Empty;
        public string Manufacturer
        {
            get => _manufacturer;
            set => SetProperty(ref _manufacturer, value);
        }

        private string _softwareType = string.Empty;
        public string SoftwareType
        {
            get => _softwareType;
            set => SetProperty(ref _softwareType, value);
        }

        private string _functionPurpose = string.Empty;
        public string FunctionPurpose
        {
            get => _functionPurpose;
            set => SetProperty(ref _functionPurpose, value);
        }

        private string? _installedHardwareComponent;
        public string? InstalledHardwareComponent
        {
            get => _installedHardwareComponent;
            set
            {
                _installedHardwareComponent = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoading;
        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        private int _totalSoftware;
        public int TotalSoftware
        {
            get => _totalSoftware;
            set
            {
                _totalSoftware = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand NavigateToComponentCommand { get; }
        public ICommand AddSoftwareCommand { get; }
        public ICommand ClearFilterCommand { get; }

        public event EventHandler? RequestClose;

        public SoftwareViewModel(
            ISoftwareService softwareService,
            IComponentService componentService,
            ISystemService systemService,
            IShipService shipService,
            INavigationService navigationService,
            IAuthenticationService authenticationService,
            ILogger<SoftwareViewModel> logger)
        {
            _softwareService = softwareService ?? throw new ArgumentNullException(nameof(softwareService));
            _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
            _shipService = shipService ?? throw new ArgumentNullException(nameof(shipService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SaveCommand = new RelayCommand(async () => await SaveSoftwareAsync(), () => CanSaveSoftware());
            CancelCommand = new RelayCommand(CancelEdit);
            EditCommand = new RelayCommand(StartEditing, () => SelectedSoftware != null && !IsEditing && CanEditData);
            DeleteCommand = new RelayCommand(async () => await DeleteSoftwareAsync(), () => SelectedSoftware != null && !IsEditing && CanEditData);
            NavigateToComponentCommand = new RelayCommand(NavigateToComponent);
            AddSoftwareCommand = new RelayCommand(AddSoftware, () => !IsEditing && CanEditData);
            ClearFilterCommand = new RelayCommand(ClearComponentFilter);

            // Subscribe to property changes to refresh save command
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SoftwareName) ||
                    e.PropertyName == nameof(Version) ||
                    e.PropertyName == nameof(Manufacturer) ||
                    e.PropertyName == nameof(SoftwareType) ||
                    e.PropertyName == nameof(SelectedComponent))
                {
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
                else if (e.PropertyName == nameof(SelectedSoftware) || e.PropertyName == nameof(IsEditing))
                {
                    ((RelayCommand)EditCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)AddSoftwareCommand).RaiseCanExecuteChanged();
                }
            };

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Set loading state on UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "Loading data...";
                });

                // Load all data in parallel
                var softwareTask = _softwareService.GetAllSoftwareAsync();
                var componentsTask = _componentService.GetAllComponentsAsync();
                var shipsTask = _shipService.GetAllShipsAsync();
                var manufacturersTask = _softwareService.GetManufacturersAsync();
                var softwareTypesTask = _softwareService.GetSoftwareTypesAsync();

                await Task.WhenAll(softwareTask, componentsTask, shipsTask, manufacturersTask, softwareTypesTask);

                // Get the results
                var software = await softwareTask;
                var components = await componentsTask;
                var ships = await shipsTask;
                var manufacturers = await manufacturersTask;
                var softwareTypes = await softwareTypesTask;

                // Update collections on UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SoftwareList.Clear();
                    foreach (var item in software)
                    {
                        SoftwareList.Add(item);
                    }

                    AvailableComponents.Clear();
                    foreach (var component in components)
                    {
                        AvailableComponents.Add(component);
                    }

                    Ships.Clear();
                    foreach (var ship in ships)
                    {
                        Ships.Add(ship);
                    }

                    Manufacturers.Clear();
                    foreach (var manufacturer in manufacturers)
                    {
                        Manufacturers.Add(manufacturer);
                    }

                    SoftwareTypes.Clear();
                    foreach (var type in softwareTypes)
                    {
                        SoftwareTypes.Add(type);
                    }

                    ApplyFilters();
                    TotalSoftware = SoftwareList.Count;
                    StatusMessage = "Data loaded successfully";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data");
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

        private void ApplyFilters()
        {
            var filtered = SoftwareList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                filtered = filtered.Where(s => 
                    s.Name.ToLower().Contains(searchLower) ||
                    (s.Manufacturer?.ToLower().Contains(searchLower) ?? false) ||
                    (s.Version?.ToLower().Contains(searchLower) ?? false) ||
                    (s.LicenseType?.ToLower().Contains(searchLower) ?? false) ||
                    (s.Description?.ToLower().Contains(searchLower) ?? false) ||
                    (s.InstalledHardwareComponent?.ToLower().Contains(searchLower) ?? false));
            }

            SoftwareList.Clear();
            foreach (var item in filtered)
            {
                SoftwareList.Add(item);
            }
            TotalSoftware = SoftwareList.Count;
        }

        private async Task SearchSoftwareAsync()
        {
            try
            {
                var results = await _softwareService.SearchSoftwareAsync(SearchTerm);
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SoftwareList.Clear();
                    foreach (var software in results)
                    {
                        SoftwareList.Add(software);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching software");
            }
        }

        private void UpdateFormFields()
        {
            if (SelectedSoftware != null)
            {
                SoftwareName = SelectedSoftware.Name;
                Version = SelectedSoftware.Version ?? string.Empty;
                Manufacturer = SelectedSoftware.Manufacturer ?? string.Empty;
                SoftwareType = SelectedSoftware.SoftwareType ?? string.Empty;
                FunctionPurpose = SelectedSoftware.FunctionPurpose ?? string.Empty;
                InstalledHardwareComponent = SelectedSoftware.InstalledHardwareComponent;
                SelectedComponent = SelectedSoftware.InstalledComponent;
            }
            else
            {
                ClearForm();
            }
        }

        private void ClearForm()
        {
            SoftwareId = 0;
            SoftwareName = string.Empty;
            Manufacturer = string.Empty;
            SoftwareType = string.Empty;
            Version = string.Empty;
            FunctionPurpose = string.Empty;
            InstalledHardwareComponent = null;
            SelectedComponent = null;
            IsEditing = false;
        }

        private bool CanSaveSoftware()
        {
            // Check permissions first
            if (!CanEditData)
                return false;

            // Check if we have required data
            return IsEditing && 
                   !string.IsNullOrWhiteSpace(SoftwareName?.Trim()) &&
                   !string.IsNullOrWhiteSpace(Version?.Trim()) &&
                   SelectedComponent != null;
        }

        private async Task SaveSoftwareAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Saving software...";

                var software = new Software
                {
                    Id = SelectedSoftware?.Id ?? 0,
                    Name = SoftwareName ?? string.Empty,
                    Version = Version ?? string.Empty,
                    Manufacturer = Manufacturer ?? string.Empty,
                    SoftwareType = SoftwareType ?? string.Empty,
                    FunctionPurpose = FunctionPurpose ?? string.Empty,
                    InstalledHardwareComponent = SelectedComponent?.Name ?? string.Empty,
                    InstalledComponentId = SelectedComponent?.Id
                };

                if (SelectedSoftware == null)
                {
                    // Create new software
                    var createdSoftware = await _softwareService.AddSoftwareAsync(software);
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SoftwareList.Add(createdSoftware);
                        TotalSoftware = SoftwareList.Count;
                        StatusMessage = "Software added successfully.";
                    });
                }
                else
                {
                    // Update existing software
                    var updatedSoftware = await _softwareService.UpdateSoftwareAsync(software);
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var index = SoftwareList.ToList().FindIndex(s => s.Id == SelectedSoftware.Id);
                        if (index >= 0)
                        {
                            SoftwareList[index] = updatedSoftware;
                        }
                        StatusMessage = "Software updated successfully.";
                    });
                }

                IsEditing = false;
                ClearForm();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving software");
                StatusMessage = "Error saving software. Please try again.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NavigateToComponent()
        {
            if (SelectedSoftware?.InstalledComponent != null)
            {
                _navigationService.NavigateToPageWithComponentFilter("Components", SelectedSoftware.InstalledComponent);
            }
        }

        public void SetComponentFilter(ComponentEntity component)
        {
            if (component == null) return;
            
            SelectedComponent = component;
            // Don't set SearchTerm to component name - this causes the search to filter incorrectly
            // SearchTerm = component.Name;
            // SearchSoftwareAsync().ConfigureAwait(false);
            
            // Instead, filter the software list to show only software for this component
            FilterSoftwareByComponent(component);
        }

        public void ClearComponentFilter()
        {
            SelectedComponent = null;
            SearchTerm = string.Empty;
            _ = LoadDataAsync(); // Reload all software
        }

        private async void FilterSoftwareByComponent(ComponentEntity component)
        {
            try
            {
                var softwareForComponent = await _softwareService.GetSoftwareByComponentIdAsync(component.Id);
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SoftwareList.Clear();
                    foreach (var software in softwareForComponent)
                    {
                        SoftwareList.Add(software);
                    }
                    TotalSoftware = SoftwareList.Count;
                    StatusMessage = $"Showing {SoftwareList.Count} software items for component: {component.Name}";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering software by component {ComponentId}", component.Id);
                StatusMessage = "Error filtering software by component";
            }
        }

        private async Task UpdateComponentsForSystemAsync()
        {
            if (_selectedSystem == null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableComponents.Clear();
                });
                return;
            }

            try
            {
                var components = await _componentService.GetComponentsBySystemIdAsync(_selectedSystem.Id);
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableComponents.Clear();
                    foreach (var component in components)
                    {
                        AvailableComponents.Add(component);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading components for system {SystemId}", _selectedSystem.Id);
                StatusMessage = "Error loading components";
            }
        }

        private async Task UpdateSystemsForShipAsync()
        {
            if (_selectedShip == null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Systems.Clear();
                });
                return;
            }

            try
            {
                var systems = await _systemService.GetSystemsByShipIdAsync(_selectedShip.Id);
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Systems.Clear();
                    foreach (var system in systems)
                    {
                        Systems.Add(system);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading systems for ship {ShipId}", _selectedShip.Id);
                StatusMessage = "Error loading systems";
            }
        }

        private void StartEditing()
        {
            if (SelectedSoftware != null)
            {
                IsEditing = true;
            }
        }

        private void CancelEdit()
        {
            if (SelectedSoftware != null)
            {
                // Restore original values
                SoftwareName = SelectedSoftware.Name;
                Version = SelectedSoftware.Version;
                Manufacturer = SelectedSoftware.Manufacturer;
                SoftwareType = SelectedSoftware.SoftwareType;
                FunctionPurpose = SelectedSoftware.FunctionPurpose;
                InstalledHardwareComponent = SelectedSoftware.InstalledHardwareComponent;
                SelectedComponent = SelectedSoftware.InstalledComponent;
            }
            else
            {
                ClearForm();
            }
            IsEditing = false;
        }

        private async Task DeleteSoftwareAsync()
        {
            if (SelectedSoftware == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete the software '{SelectedSoftware.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    await _softwareService.DeleteSoftwareAsync(SelectedSoftware.Id);
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SoftwareList.Remove(SelectedSoftware);
                        TotalSoftware = SoftwareList.Count;
                        ClearForm();
                        StatusMessage = "Software deleted successfully";
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting software");
                    MessageBox.Show(
                        "An error occurred while deleting the software. Please try again.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void AddSoftware()
        {
            SelectedSoftware = null;
            ClearForm();
            IsEditing = true;
        }

        public int FilteredSoftwareCount => SoftwareList?.Count ?? 0;
        
        public bool HasSelectedSoftware => SelectedSoftware != null;

        // Permission properties
        public bool CanEditData => _authenticationService.CurrentUser?.Role?.Name == "Administrator";
        public bool IsReadOnlyUser => _authenticationService.CurrentUser?.Role?.Name == "Engineer";
    }
} 