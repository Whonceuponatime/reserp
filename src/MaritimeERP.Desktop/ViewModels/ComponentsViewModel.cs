using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Component = MaritimeERP.Core.Entities.Component;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MaritimeERP.Desktop.ViewModels
{
    public class ComponentsViewModel : INotifyPropertyChanged
    {
        private readonly IComponentService _componentService;
        private readonly ISystemService _systemService;
        private readonly IShipService _shipService;
        private readonly ILogger<ComponentsViewModel> _logger;
        private readonly INavigationService _navigationService;
        private readonly DashboardViewModel _dashboardViewModel;

        // Collections
        public ObservableCollection<Component> Components { get; set; } = new();
        private ObservableCollection<Component> _filteredComponents = new();
        public ObservableCollection<Component> FilteredComponents
        {
            get => _filteredComponents;
            set
            {
                _filteredComponents = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ShipSystem> Systems { get; set; } = new();
        public ObservableCollection<Ship> Ships { get; set; } = new();
        public ObservableCollection<string> MakerModels { get; set; } = new();
        public ObservableCollection<string> Locations { get; set; } = new();

        // Selected items
        private Component? _selectedComponent;
        public Component? SelectedComponent
        {
            get => _selectedComponent;
            set
            {
                _selectedComponent = value;
                OnPropertyChanged();
                OnSelectedComponentChanged();
            }
        }

        private ShipSystem? _selectedSystem;
        public ShipSystem? SelectedSystem
        {
            get => _selectedSystem;
            set
            {
                _selectedSystem = value;
                OnPropertyChanged();
                UpdateComponentFromSystem();
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
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        // Form properties for current component
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
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

        private int _systemId;
        public int SystemId
        {
            get => _systemId;
            set
            {
                _systemId = value;
                OnPropertyChanged();
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private string _makerModel = string.Empty;
        public string MakerModel
        {
            get => _makerModel;
            set
            {
                _makerModel = value;
                OnPropertyChanged();
            }
        }

        private short _usbPorts;
        public short UsbPorts
        {
            get => _usbPorts;
            set
            {
                _usbPorts = value;
                OnPropertyChanged(nameof(UsbPorts));
            }
        }

        private short _lanPorts;
        public short LanPorts
        {
            get => _lanPorts;
            set
            {
                _lanPorts = value;
                OnPropertyChanged();
            }
        }

        private string? _connectedCbs;
        public string? ConnectedCbs
        {
            get => _connectedCbs;
            set
            {
                _connectedCbs = value;
                OnPropertyChanged();
            }
        }

        private string _installedLocation = string.Empty;
        public string InstalledLocation
        {
            get => _installedLocation;
            set
            {
                _installedLocation = value;
                OnPropertyChanged();
            }
        }

        // UI State
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
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

        private int _totalComponents;
        public int TotalComponents
        {
            get => _totalComponents;
            set
            {
                _totalComponents = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand LoadDataCommand { get; private set; }
        public ICommand AddComponentCommand { get; private set; }
        public ICommand EditCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand NavigateToSoftwareCommand { get; private set; }

        private ICommand? _saveCommand;
        public ICommand SaveCommand 
        {
            get
            {
                _saveCommand ??= new RelayCommand(
                    async () => await SaveComponentAsync(),
                    () => !IsLoading && CanSaveComponent()
                );
                return _saveCommand;
            }
        }

        // Events
        public event EventHandler<Component>? ComponentSaved;
        public event EventHandler<Component>? ComponentDeleted;
        public event EventHandler? RequestClose;

        // Properties
        public int Id { get; set; }

        private string _systemName = string.Empty;
        public string SystemName
        {
            get => _systemName;
            set
            {
                _systemName = value;
                OnPropertyChanged(nameof(SystemName));
            }
        }

        private string _componentType = string.Empty;
        public string ComponentType
        {
            get => _componentType;
            set
            {
                _componentType = value;
                OnPropertyChanged(nameof(ComponentType));
            }
        }

        private string _manufacturer = string.Empty;
        public string Manufacturer
        {
            get => _manufacturer;
            set
            {
                _manufacturer = value;
                OnPropertyChanged(nameof(Manufacturer));
            }
        }

        private string _model = string.Empty;
        public string Model
        {
            get => _model;
            set
            {
                _model = value;
                OnPropertyChanged(nameof(Model));
            }
        }

        private string? _osName;
        public string? OsName
        {
            get => _osName;
            set
            {
                _osName = value;
                OnPropertyChanged(nameof(OsName));
            }
        }

        private string? _osVersion;
        public string? OsVersion
        {
            get => _osVersion;
            set
            {
                _osVersion = value;
                OnPropertyChanged(nameof(OsVersion));
            }
        }

        private string? _networkSegment;
        public string? NetworkSegment
        {
            get => _networkSegment;
            set
            {
                _networkSegment = value;
                OnPropertyChanged(nameof(NetworkSegment));
            }
        }

        private string? _supportedProtocols;
        public string? SupportedProtocols
        {
            get => _supportedProtocols;
            set
            {
                _supportedProtocols = value;
                OnPropertyChanged(nameof(SupportedProtocols));
            }
        }

        private string? _connectionPurpose;
        public string? ConnectionPurpose
        {
            get => _connectionPurpose;
            set
            {
                _connectionPurpose = value;
                OnPropertyChanged(nameof(ConnectionPurpose));
            }
        }

        private bool _hasRemoteConnection;
        public bool HasRemoteConnection
        {
            get => _hasRemoteConnection;
            set
            {
                _hasRemoteConnection = value;
                OnPropertyChanged(nameof(HasRemoteConnection));
            }
        }

        private bool _isTypeApproved;
        public bool IsTypeApproved
        {
            get => _isTypeApproved;
            set
            {
                _isTypeApproved = value;
                OnPropertyChanged(nameof(IsTypeApproved));
            }
        }

        public ComponentsViewModel(
            IComponentService componentService,
            ISystemService systemService,
            IShipService shipService,
            ILogger<ComponentsViewModel> logger,
            INavigationService navigationService,
            DashboardViewModel dashboardViewModel)
        {
            _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
            _shipService = shipService ?? throw new ArgumentNullException(nameof(shipService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));

            // Initialize commands with proper CanExecute conditions
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            AddComponentCommand = new RelayCommand(AddComponent, () => !IsLoading && !IsEditing);
            EditCommand = new RelayCommand(StartEditing, () => SelectedComponent != null && !IsEditing);
            DeleteCommand = new RelayCommand(async () => await DeleteComponentAsync(), () => SelectedComponent != null && !IsEditing);
            CancelCommand = new RelayCommand(CancelEdit);
            ClearFiltersCommand = new RelayCommand(ClearFilters, () => !IsLoading);
            NavigateToSoftwareCommand = new RelayCommand(() => NavigateToSoftware(SelectedComponent), () => SelectedComponent != null);

            // Subscribe to property changes to refresh command states
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Name) ||
                    e.PropertyName == nameof(ComponentType) ||
                    e.PropertyName == nameof(InstalledLocation) ||
                    e.PropertyName == nameof(SelectedSystem))
                {
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                }
                else if (e.PropertyName == nameof(SelectedComponent) || e.PropertyName == nameof(IsEditing))
                {
                    ((RelayCommand)EditCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)AddComponentCommand).RaiseCanExecuteChanged();
                }
            };

            _ = LoadDataAsync();
        }

        /// <summary>
        /// Sets a system filter for the components page when navigating from Systems page
        /// </summary>
        /// <param name="systemFilter">The system to filter components by</param>
        public void SetSystemFilter(ShipSystem systemFilter)
        {
            // Set the system filter after data is loaded
            Task.Run(async () =>
            {
                // Wait for data to load first
                while (IsLoading)
                {
                    await Task.Delay(100);
                }

                // Set the filter on the UI thread
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    // Find and select the ship
                    var ship = Ships.FirstOrDefault(s => s.Id == systemFilter.ShipId);
                    if (ship != null)
                    {
                        SelectedShip = ship;
                    }

                    // Find and select the system
                    var system = Systems.FirstOrDefault(s => s.Id == systemFilter.Id);
                    if (system != null)
                    {
                        SelectedSystem = system;
                    }

                    // Update status to show filtering
                    StatusMessage = $"Showing components for system: {systemFilter.Name}";
                });
            });
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading components data...";

                // Load all data in parallel
                var componentsTask = _componentService.GetAllComponentsAsync();
                var systemsTask = _systemService.GetAllSystemsAsync();
                var shipsTask = _shipService.GetAllShipsAsync();

                await Task.WhenAll(componentsTask, systemsTask, shipsTask);

                // Update collections
                Components.Clear();
                foreach (var component in await componentsTask)
                {
                    Components.Add(component);
                }

                Systems.Clear();
                foreach (var system in await systemsTask)
                {
                    Systems.Add(system);
                }

                Ships.Clear();
                foreach (var ship in await shipsTask)
                {
                    Ships.Add(ship);
                }

                TotalComponents = Components.Count;
                ApplyFilters();
                StatusMessage = $"Loaded {Components.Count} components successfully";
                _logger.LogInformation("Components data loaded successfully");
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading components data";
                _logger.LogError(ex, "Error loading components data");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            var filtered = Components.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(c => 
                    c.Name.ToLower().Contains(searchLower) ||
                    c.MakerModel.ToLower().Contains(searchLower) ||
                    c.InstalledLocation.ToLower().Contains(searchLower) ||
                    c.System.Name.ToLower().Contains(searchLower) ||
                    c.System.Ship.ShipName.ToLower().Contains(searchLower));
            }

            // Apply ship filter
            if (SelectedShip != null)
            {
                filtered = filtered.Where(c => c.System.ShipId == SelectedShip.Id);
            }

            // Apply system filter
            if (SelectedSystem != null)
            {
                filtered = filtered.Where(c => c.SystemId == SelectedSystem.Id);
            }

            FilteredComponents.Clear();
            foreach (var component in filtered.OrderBy(c => c.System.Ship.ShipName)
                                             .ThenBy(c => c.System.Name)
                                             .ThenBy(c => c.Name))
            {
                FilteredComponents.Add(component);
            }

            StatusMessage = $"Showing {FilteredComponents.Count} of {TotalComponents} components";
        }

        private async Task UpdateSystemsForShipAsync()
        {
            if (SelectedShip == null)
            {
                // If no ship selected, show all systems
                await LoadDataAsync();
                return;
            }

            try
            {
                var systems = await _systemService.GetSystemsByShipIdAsync(SelectedShip.Id);
                Systems.Clear();
                foreach (var system in systems)
                {
                    Systems.Add(system);
                }
                
                // Reset system selection if current system doesn't belong to selected ship
                if (SelectedSystem != null && SelectedSystem.ShipId != SelectedShip.Id)
                {
                    SelectedSystem = null;
                }
                
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating systems for ship");
            }
        }

        private void OnSelectedComponentChanged()
        {
            if (SelectedComponent != null && !IsEditing)
            {
                PopulateFormFromSelectedComponent();
            }
        }

        private void PopulateFormFromSelectedComponent()
        {
            if (SelectedComponent == null) return;

            Name = SelectedComponent.Name;
            Manufacturer = SelectedComponent.Manufacturer ?? string.Empty;
            Model = SelectedComponent.Model ?? string.Empty;
            ComponentType = SelectedComponent.ComponentType;
            SystemName = SelectedComponent.SystemName;
            InstalledLocation = SelectedComponent.InstalledLocation;
            UsbPorts = SelectedComponent.UsbPorts;
            LanPorts = SelectedComponent.LanPorts;
            ConnectedCbs = SelectedComponent.ConnectedCbs;
            HasRemoteConnection = SelectedComponent.HasRemoteConnection;
            IsTypeApproved = SelectedComponent.IsTypeApproved;
            OsName = SelectedComponent.OsName;
            OsVersion = SelectedComponent.OsVersion;
            NetworkSegment = SelectedComponent.NetworkSegment;
            SupportedProtocols = SelectedComponent.SupportedProtocols;
            ConnectionPurpose = SelectedComponent.ConnectionPurpose;

            // Find and select the system
            var system = Systems.FirstOrDefault(s => s.Id == SelectedComponent.SystemId);
            if (system != null)
            {
                SelectedSystem = system;
            }
        }

        private void UpdateComponentFromSystem()
        {
            if (SelectedSystem != null)
            {
                SystemId = SelectedSystem.Id;
            }
        }

        private void AddComponent()
        {
            SelectedComponent = null;
            ClearForm();
            IsEditing = true;
        }

        private void StartEditing()
        {
            if (SelectedComponent != null)
            {
                IsEditing = true;
            }
        }

        private void CancelEdit()
        {
            if (SelectedComponent != null)
            {
                // Restore original values
                PopulateFormFromSelectedComponent();
            }
            else
            {
                ClearForm();
            }
            IsEditing = false;
        }

        private void ClearForm()
        {
            Name = string.Empty;
            Manufacturer = string.Empty;
            Model = string.Empty;
            ComponentType = string.Empty;
            SystemName = string.Empty;
            InstalledLocation = string.Empty;
            UsbPorts = 0;
            LanPorts = 0;
            ConnectedCbs = string.Empty;
            HasRemoteConnection = false;
            IsTypeApproved = false;
            OsName = string.Empty;
            OsVersion = string.Empty;
            NetworkSegment = string.Empty;
            SupportedProtocols = string.Empty;
            ConnectionPurpose = string.Empty;
            SelectedSystem = null;
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedShip = null;
            SelectedSystem = null;

            ApplyFilters();
            StatusMessage = "Filters cleared";
        }

        private void NavigateToSoftware(Component? component)
        {
            if (component == null) return;

            _navigationService.NavigateToPageWithComponentFilter("Software", component);
        }

        private bool CanSaveComponent()
        {
            return IsEditing && 
                   !string.IsNullOrWhiteSpace(Name) && 
                   !string.IsNullOrWhiteSpace(ComponentType) && 
                   !string.IsNullOrWhiteSpace(InstalledLocation) && 
                   SelectedSystem != null;
        }

        private async Task SaveComponentAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Saving component...";

                var component = new Component
                {
                    Id = SelectedComponent?.Id ?? 0,
                    Name = Name,
                    Manufacturer = Manufacturer,
                    Model = Model,
                    ComponentType = ComponentType,
                    SystemId = SelectedSystem?.Id ?? 0,
                    SystemName = SelectedSystem?.Name ?? string.Empty,
                    InstalledLocation = InstalledLocation,
                    UsbPorts = UsbPorts,
                    LanPorts = LanPorts,
                    ConnectedCbs = ConnectedCbs,
                    HasRemoteConnection = HasRemoteConnection,
                    IsTypeApproved = IsTypeApproved,
                    OsName = OsName,
                    OsVersion = OsVersion,
                    NetworkSegment = NetworkSegment,
                    SupportedProtocols = SupportedProtocols,
                    ConnectionPurpose = ConnectionPurpose
                };

                if (SelectedComponent == null)
                {
                    await _componentService.AddComponentAsync(component);
                    StatusMessage = "Component added successfully.";
                }
                else
                {
                    await _componentService.UpdateComponentAsync(component);
                    StatusMessage = "Component updated successfully.";
                }

                await LoadDataAsync();
                IsEditing = false;
                ClearForm();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving component");
                StatusMessage = "Error saving component. Please try again.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteComponentAsync()
        {
            if (SelectedComponent == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Deleting component...";

                await _componentService.DeleteComponentAsync(SelectedComponent.Id);
                
                    Components.Remove(SelectedComponent);
                FilteredComponents.Remove(SelectedComponent);
                SelectedComponent = null;
                    ClearForm();

                StatusMessage = "Component deleted successfully.";
                ComponentDeleted?.Invoke(this, SelectedComponent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting component");
                StatusMessage = "Error deleting component. Please try again.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Refresh command states when relevant properties change
            if (propertyName == nameof(IsEditing) || 
                propertyName == nameof(IsLoading) || 
                propertyName == nameof(SelectedComponent) ||
                propertyName == nameof(Name) ||
                propertyName == nameof(ComponentType) ||
                propertyName == nameof(Manufacturer) ||
                propertyName == nameof(Model) ||
                propertyName == nameof(InstalledLocation) ||
                propertyName == nameof(SelectedSystem))
            {
                (AddComponentCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (CancelCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
} 