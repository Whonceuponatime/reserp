using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;
using ComponentEntity = MaritimeERP.Core.Entities.Component;

namespace MaritimeERP.Desktop.ViewModels
{
    public class ComponentsViewModel : INotifyPropertyChanged
    {
        private readonly IComponentService _componentService;
        private readonly ISystemService _systemService;
        private readonly IShipService _shipService;
        private readonly ILogger<ComponentsViewModel> _logger;

        // Collections
        public ObservableCollection<ComponentEntity> Components { get; set; } = new();
        public ObservableCollection<ShipSystem> Systems { get; set; } = new();
        public ObservableCollection<Ship> Ships { get; set; } = new();
        public ObservableCollection<string> MakerModels { get; set; } = new();
        public ObservableCollection<string> Locations { get; set; } = new();

        // Selected items
        private ComponentEntity? _selectedComponent;
        public ComponentEntity? SelectedComponent
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
                _ = SearchComponentsAsync();
            }
        }

        private int? _filterSystemId;
        public int? FilterSystemId
        {
            get => _filterSystemId;
            set
            {
                _filterSystemId = value;
                OnPropertyChanged();
                _ = FilterComponentsAsync();
            }
        }

        private int? _filterShipId;
        public int? FilterShipId
        {
            get => _filterShipId;
            set
            {
                _filterShipId = value;
                OnPropertyChanged();
                _ = FilterComponentsAsync();
            }
        }

        private string? _filterMakerModel;
        public string? FilterMakerModel
        {
            get => _filterMakerModel;
            set
            {
                _filterMakerModel = value;
                OnPropertyChanged();
                _ = FilterComponentsAsync();
            }
        }

        private string? _filterLocation;
        public string? FilterLocation
        {
            get => _filterLocation;
            set
            {
                _filterLocation = value;
                OnPropertyChanged();
                _ = FilterComponentsAsync();
            }
        }

        private bool _showOnlyRemoteComponents;
        public bool ShowOnlyRemoteComponents
        {
            get => _showOnlyRemoteComponents;
            set
            {
                _showOnlyRemoteComponents = value;
                OnPropertyChanged();
                _ = FilterComponentsAsync();
            }
        }

        // Form properties for current component
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

        private string _componentName = string.Empty;
        public string ComponentName
        {
            get => _componentName;
            set
            {
                _componentName = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
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

        private short _serialPorts;
        public short SerialPorts
        {
            get => _serialPorts;
            set
            {
                _serialPorts = value;
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

        private bool _hasRemoteConnection;
        public bool HasRemoteConnection
        {
            get => _hasRemoteConnection;
            set
            {
                _hasRemoteConnection = value;
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

        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand AddComponentCommand { get; }
        public ICommand EditComponentCommand { get; }
        public ICommand SaveComponentCommand { get; }
        public ICommand DeleteComponentCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public ComponentsViewModel(IComponentService componentService, ISystemService systemService, 
            IShipService shipService, ILogger<ComponentsViewModel> logger)
        {
            _componentService = componentService;
            _systemService = systemService;
            _shipService = shipService;
            _logger = logger;

            // Initialize commands
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
            AddComponentCommand = new RelayCommand(AddComponent);
            EditComponentCommand = new RelayCommand(EditComponent, () => SelectedComponent != null);
            SaveComponentCommand = new RelayCommand(async () => await SaveComponentAsync(), CanSaveComponent);
            DeleteComponentCommand = new RelayCommand(async () => await DeleteComponentAsync(), () => SelectedComponent != null);
            CancelEditCommand = new RelayCommand(CancelEdit);
            ClearFiltersCommand = new RelayCommand(ClearFilters);

            // Load data on initialization
            _ = LoadDataAsync();
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
                var makerModelsTask = _componentService.GetMakerModelsAsync();
                var locationsTask = _componentService.GetInstalledLocationsAsync();

                await Task.WhenAll(componentsTask, systemsTask, shipsTask, makerModelsTask, locationsTask);

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

                MakerModels.Clear();
                foreach (var makerModel in await makerModelsTask)
                {
                    MakerModels.Add(makerModel);
                }

                Locations.Clear();
                foreach (var location in await locationsTask)
                {
                    Locations.Add(location);
                }

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

        private async Task SearchComponentsAsync()
        {
            try
            {
                IsLoading = true;
                var components = await _componentService.SearchComponentsAsync(SearchText);
                
                Components.Clear();
                foreach (var component in components)
                {
                    Components.Add(component);
                }

                StatusMessage = $"Found {Components.Count} components matching '{SearchText}'";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error searching components";
                _logger.LogError(ex, "Error searching components");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FilterComponentsAsync()
        {
            try
            {
                IsLoading = true;
                IEnumerable<ComponentEntity> components;

                if (ShowOnlyRemoteComponents)
                {
                    components = await _componentService.GetComponentsWithRemoteConnectionAsync();
                }
                else if (FilterSystemId.HasValue)
                {
                    components = await _componentService.GetComponentsBySystemIdAsync(FilterSystemId.Value);
                }
                else if (!string.IsNullOrEmpty(FilterMakerModel))
                {
                    components = await _componentService.GetComponentsByMakerModelAsync(FilterMakerModel);
                }
                else if (!string.IsNullOrEmpty(FilterLocation))
                {
                    components = await _componentService.GetComponentsByLocationAsync(FilterLocation);
                }
                else
                {
                    components = await _componentService.GetAllComponentsAsync();
                }

                // Apply ship filter if specified
                if (FilterShipId.HasValue)
                {
                    components = components.Where(c => c.System.ShipId == FilterShipId.Value);
                }

                Components.Clear();
                foreach (var component in components)
                {
                    Components.Add(component);
                }

                StatusMessage = $"Filtered to {Components.Count} components";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error filtering components";
                _logger.LogError(ex, "Error filtering components");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateSystemsForShipAsync()
        {
            if (SelectedShip == null) return;

            try
            {
                var systems = await _systemService.GetSystemsByShipIdAsync(SelectedShip.Id);
                Systems.Clear();
                foreach (var system in systems)
                {
                    Systems.Add(system);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating systems for ship");
            }
        }

        private void OnSelectedComponentChanged()
        {
            if (SelectedComponent != null)
            {
                PopulateFormFromSelectedComponent();
            }
        }

        private void PopulateFormFromSelectedComponent()
        {
            if (SelectedComponent == null) return;

            ComponentId = SelectedComponent.Id;
            SystemId = SelectedComponent.SystemId;
            ComponentName = SelectedComponent.Name;
            MakerModel = SelectedComponent.MakerModel;
            UsbPorts = SelectedComponent.UsbPorts;
            LanPorts = SelectedComponent.LanPorts;
            SerialPorts = SelectedComponent.SerialPorts;
            ConnectedCbs = SelectedComponent.ConnectedCbs;
            HasRemoteConnection = SelectedComponent.HasRemoteConnection;
            InstalledLocation = SelectedComponent.InstalledLocation;

            // Update selected system
            SelectedSystem = Systems.FirstOrDefault(s => s.Id == SystemId);
            if (SelectedSystem != null)
            {
                SelectedShip = Ships.FirstOrDefault(ship => ship.Id == SelectedSystem.ShipId);
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
            ClearForm();
            IsEditing = true;
            StatusMessage = "Adding new component";
        }

        private void EditComponent()
        {
            if (SelectedComponent != null)
            {
                IsEditing = true;
                StatusMessage = $"Editing component: {SelectedComponent.Name}";
            }
        }

        private async Task SaveComponentAsync()
        {
            try
            {
                IsLoading = true;

                var component = new ComponentEntity
                {
                    Id = ComponentId,
                    SystemId = SystemId,
                    Name = ComponentName,
                    MakerModel = MakerModel,
                    UsbPorts = UsbPorts,
                    LanPorts = LanPorts,
                    SerialPorts = SerialPorts,
                    ConnectedCbs = ConnectedCbs,
                    HasRemoteConnection = HasRemoteConnection,
                    InstalledLocation = InstalledLocation
                };

                if (ComponentId == 0)
                {
                    // Create new component
                    var createdComponent = await _componentService.CreateComponentAsync(component);
                    Components.Add(createdComponent);
                    StatusMessage = "Component created successfully";
                }
                else
                {
                    // Update existing component
                    var updatedComponent = await _componentService.UpdateComponentAsync(component);
                    var index = Components.ToList().FindIndex(c => c.Id == ComponentId);
                    if (index >= 0)
                    {
                        Components[index] = updatedComponent;
                    }
                    StatusMessage = "Component updated successfully";
                }

                IsEditing = false;
                ClearForm();
                _logger.LogInformation("Component saved successfully: {ComponentName}", ComponentName);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving component: {ex.Message}";
                _logger.LogError(ex, "Error saving component");
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
                var success = await _componentService.DeleteComponentAsync(SelectedComponent.Id);
                
                if (success)
                {
                    Components.Remove(SelectedComponent);
                    StatusMessage = "Component deleted successfully";
                    ClearForm();
                    _logger.LogInformation("Component deleted: {ComponentId}", SelectedComponent.Id);
                }
                else
                {
                    StatusMessage = "Component not found or could not be deleted";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting component: {ex.Message}";
                _logger.LogError(ex, "Error deleting component");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            ClearForm();
            StatusMessage = "Edit cancelled";
        }

        private void ClearForm()
        {
            ComponentId = 0;
            SystemId = 0;
            ComponentName = string.Empty;
            MakerModel = string.Empty;
            UsbPorts = 0;
            LanPorts = 0;
            SerialPorts = 0;
            ConnectedCbs = null;
            HasRemoteConnection = false;
            InstalledLocation = string.Empty;
            SelectedSystem = null;
            SelectedShip = null;
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            FilterSystemId = null;
            FilterShipId = null;
            FilterMakerModel = null;
            FilterLocation = null;
            ShowOnlyRemoteComponents = false;
            _ = LoadDataAsync();
        }

        private bool CanSaveComponent()
        {
            return !string.IsNullOrWhiteSpace(ComponentName) &&
                   !string.IsNullOrWhiteSpace(MakerModel) &&
                   !string.IsNullOrWhiteSpace(InstalledLocation) &&
                   SystemId > 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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