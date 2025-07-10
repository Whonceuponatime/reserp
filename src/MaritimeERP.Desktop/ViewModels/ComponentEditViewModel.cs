using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;
using ComponentEntity = MaritimeERP.Core.Entities.Component;
using System.Windows;

namespace MaritimeERP.Desktop.ViewModels
{
    public class ComponentEditViewModel : ViewModelBase
    {
        private readonly IComponentService _componentService;
        private readonly ISystemService _systemService;
        private readonly IShipService _shipService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ComponentEditViewModel> _logger;
        private ComponentEntity? _originalComponent;
        private bool _isEditing;
        private string _statusMessage = string.Empty;

        public ComponentEditViewModel(
            IComponentService componentService,
            ISystemService systemService,
            IShipService shipService,
            IServiceProvider serviceProvider,
            ILogger<ComponentEditViewModel> logger,
            ComponentEntity? component = null)
        {
            _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
            _shipService = shipService ?? throw new ArgumentNullException(nameof(shipService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _originalComponent = component;
            _isEditing = component != null;

            SaveComponentCommand = new RelayCommand(async () => await SaveComponentAsync());
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
            
            InitializeDataAsync().ConfigureAwait(false);
            
            if (_isEditing && _originalComponent != null)
            {
                PopulateFromComponent(_originalComponent);
            }
        }

        #region Events

        public event EventHandler<MaritimeERP.Core.Entities.Component>? ComponentSaved;
        public event EventHandler? RequestClose;

        #endregion

        #region Properties

        // Collections
        public ObservableCollection<Ship> Ships { get; } = new();
        public ObservableCollection<ShipSystem> Systems { get; } = new();

        // Core Properties
        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private int _systemId;
        public int SystemId
        {
            get => _systemId;
            set => SetProperty(ref _systemId, value);
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                SetProperty(ref _name, value);
                (SaveComponentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _systemName = string.Empty;
        public string SystemName
        {
            get => _systemName;
            set
            {
                SetProperty(ref _systemName, value);
                (SaveComponentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _componentType = string.Empty;
        public string ComponentType
        {
            get => _componentType;
            set
            {
                SetProperty(ref _componentType, value);
                (SaveComponentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string? _manufacturer;
        public string? Manufacturer
        {
            get => _manufacturer;
            set
            {
                SetProperty(ref _manufacturer, value);
                (SaveComponentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string? _model;
        public string? Model
        {
            get => _model;
            set
            {
                SetProperty(ref _model, value);
                (SaveComponentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string? _osName;
        public string? OsName
        {
            get => _osName;
            set => SetProperty(ref _osName, value);
        }

        private string? _osVersion;
        public string? OsVersion
        {
            get => _osVersion;
            set => SetProperty(ref _osVersion, value);
        }

        private short _usbPorts;
        public short UsbPorts
        {
            get => _usbPorts;
            set => SetProperty(ref _usbPorts, value);
        }

        private short _lanPorts;
        public short LanPorts
        {
            get => _lanPorts;
            set => SetProperty(ref _lanPorts, value);
        }

        private string? _supportedProtocols;
        public string? SupportedProtocols
        {
            get => _supportedProtocols;
            set => SetProperty(ref _supportedProtocols, value);
        }

        private string? _networkSegment;
        public string? NetworkSegment
        {
            get => _networkSegment;
            set => SetProperty(ref _networkSegment, value);
        }

        private string? _connectedCbs;
        public string? ConnectedCbs
        {
            get => _connectedCbs;
            set => SetProperty(ref _connectedCbs, value);
        }

        private string? _connectionPurpose;
        public string? ConnectionPurpose
        {
            get => _connectionPurpose;
            set => SetProperty(ref _connectionPurpose, value);
        }

        private bool _hasRemoteConnection;
        public bool HasRemoteConnection
        {
            get => _hasRemoteConnection;
            set => SetProperty(ref _hasRemoteConnection, value);
        }

        private bool _isTypeApproved;
        public bool IsTypeApproved
        {
            get => _isTypeApproved;
            set => SetProperty(ref _isTypeApproved, value);
        }

        private string _installedLocation = string.Empty;
        public string InstalledLocation
        {
            get => _installedLocation;
            set
            {
                SetProperty(ref _installedLocation, value);
                (SaveComponentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // Selected items
        private Ship? _selectedShip;
        public Ship? SelectedShip
        {
            get => _selectedShip;
            set
            {
                if (SetProperty(ref _selectedShip, value))
                {
                    _ = UpdateSystemsForShipAsync().ConfigureAwait(false);
                }
            }
        }

        private ShipSystem? _selectedSystem;
        public ShipSystem? SelectedSystem
        {
            get => _selectedSystem;
            set
            {
                if (SetProperty(ref _selectedSystem, value))
                {
                    if (value != null)
                    {
                        SystemId = value.Id;
                    }
                }
            }
        }

        // State Properties
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ComponentEntity? Component { get; private set; }

        #endregion

        #region Commands

        public required ICommand SaveComponentCommand { get; init; }
        public required ICommand CancelCommand { get; init; }

        #endregion

        #region Methods

        private async Task InitializeDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading data...";

                // If editing and we have a system ID, load just that system's details
                if (IsEditing && SystemId > 0)
                {
                    var system = await _systemService.GetSystemByIdAsync(SystemId);
                    if (system != null)
                    {
                        Systems.Clear();
                        Systems.Add(system);
                        SelectedSystem = system;

                        // No need to load ships since we're editing a component in an existing system
                        Ships.Clear();
                        if (system.Ship != null)
                        {
                            Ships.Add(system.Ship);
                            SelectedShip = system.Ship;
                        }
                    }
                }
                else
                {
                    // Load ships and systems for new component
                var shipsTask = _shipService.GetAllShipsAsync();
                var systemsTask = _systemService.GetAllSystemsAsync();

                await Task.WhenAll(shipsTask, systemsTask);

                Ships.Clear();
                foreach (var ship in await shipsTask)
                {
                    Ships.Add(ship);
                }

                Systems.Clear();
                foreach (var system in await systemsTask)
                {
                    Systems.Add(system);
                    }
                }

                StatusMessage = "Data loaded successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data in ComponentEditViewModel");
                StatusMessage = $"Error loading data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateSystemsForShipAsync()
        {
            if (SelectedShip == null)
            {
                Systems.Clear();
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "Loading systems...";

                var systems = await _systemService.GetSystemsByShipIdAsync(SelectedShip.Id);

                Systems.Clear();
                foreach (var system in systems)
                {
                    Systems.Add(system);
                }

                // Clear selected system since ship changed
                    SelectedSystem = null;

                StatusMessage = "Systems loaded successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading systems for ship");
                StatusMessage = $"Error loading systems: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PopulateFromComponent(ComponentEntity component)
        {
            Id = component.Id;
            SystemId = component.SystemId;
            SystemName = component.SystemName;
            ComponentType = component.ComponentType;
            Name = component.Name;
            Manufacturer = component.Manufacturer;
            Model = component.Model;
            OsName = component.OsName;
            OsVersion = component.OsVersion;
            UsbPorts = component.UsbPorts;
            LanPorts = component.LanPorts;
            SupportedProtocols = component.SupportedProtocols;
            NetworkSegment = component.NetworkSegment;
            ConnectedCbs = component.ConnectedCbs;
            ConnectionPurpose = component.ConnectionPurpose;
            HasRemoteConnection = component.HasRemoteConnection;
            IsTypeApproved = component.IsTypeApproved;
            InstalledLocation = component.InstalledLocation;

            // If the component has a system, select it
            if (component.System != null)
            {
                SelectedSystem = component.System;
                
                // If the system has a ship, select it
                if (component.System.Ship != null)
                {
                    SelectedShip = component.System.Ship;
                }
            }
        }

        private async Task SaveComponentAsync()
        {
            try
            {
                var component = new MaritimeERP.Core.Entities.Component
                {
                    Id = Id,
                    SystemId = SelectedSystem?.Id ?? 0,
                    SystemName = SystemName,
                    ComponentType = ComponentType,
                    Name = Name,
                    Manufacturer = Manufacturer,
                    Model = Model,
                    InstalledLocation = InstalledLocation,
                    OsName = OsName,
                    OsVersion = OsVersion,
                    UsbPorts = UsbPorts,
                    LanPorts = LanPorts,
                    ConnectedCbs = ConnectedCbs,
                    NetworkSegment = NetworkSegment,
                    SupportedProtocols = SupportedProtocols,
                    ConnectionPurpose = ConnectionPurpose,
                    HasRemoteConnection = HasRemoteConnection,
                    IsTypeApproved = IsTypeApproved
                };

                if (Id == 0)
                {
                    var newComponent = await _componentService.AddComponentAsync(component);
                    ComponentSaved?.Invoke(this, newComponent);
                }
                else
                {
                    var updatedComponent = await _componentService.UpdateComponentAsync(component);
                    ComponentSaved?.Invoke(this, updatedComponent);
                }

                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving component");
                MessageBox.Show("Error saving component", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSave()
        {
            if (IsEditing)
            {
                // When editing, we already have a system
                return !string.IsNullOrWhiteSpace(Name) &&
                       !string.IsNullOrWhiteSpace(SystemName) &&
                       !string.IsNullOrWhiteSpace(ComponentType) &&
                       !string.IsNullOrWhiteSpace(InstalledLocation);
            }
            else
            {
                // For new components, we need a selected system
                return !string.IsNullOrWhiteSpace(Name) &&
                       !string.IsNullOrWhiteSpace(SystemName) &&
                       !string.IsNullOrWhiteSpace(ComponentType) &&
                       !string.IsNullOrWhiteSpace(InstalledLocation) &&
                       SelectedSystem != null;
            }
        }

        public void LoadComponent(ComponentEntity component)
        {
            _originalComponent = component ?? throw new ArgumentNullException(nameof(component));
            _isEditing = true;
            PopulateFromComponent(component);
        }

        #endregion

        protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
        }
    }
} 