using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using ComponentEntity = MaritimeERP.Core.Entities.Component;

namespace MaritimeERP.Desktop.ViewModels
{
    public class ComponentEditViewModel : INotifyPropertyChanged
    {
        private readonly IComponentService _componentService;
        private readonly ISystemService _systemService;
        private readonly IShipService _shipService;
        private readonly IServiceProvider _serviceProvider;
        private ComponentEntity? _originalComponent;
        private bool _isEditing;
        private bool _isLoading;
        private string _statusMessage = string.Empty;

        public ComponentEditViewModel(IComponentService componentService, ISystemService systemService, 
            IShipService shipService, IServiceProvider serviceProvider, ComponentEntity? component = null)
        {
            _componentService = componentService;
            _systemService = systemService;
            _shipService = shipService;
            _serviceProvider = serviceProvider;
            _originalComponent = component;
            _isEditing = component != null;

            InitializeCommands();
            _ = InitializeDataAsync();
            
            if (_isEditing && _originalComponent != null)
            {
                PopulateFromComponent(_originalComponent);
            }
        }

        #region Events

        public event EventHandler? ComponentSaved;
        public event EventHandler? RequestClose;

        #endregion

        #region Properties

        // Collections
        public ObservableCollection<Ship> Ships { get; set; } = new();
        public ObservableCollection<ShipSystem> Systems { get; set; } = new();

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

        private string _componentName = string.Empty;
        public string ComponentName
        {
            get => _componentName;
            set => SetProperty(ref _componentName, value);
        }

        private string _makerModel = string.Empty;
        public string MakerModel
        {
            get => _makerModel;
            set => SetProperty(ref _makerModel, value);
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

        private short _serialPorts;
        public short SerialPorts
        {
            get => _serialPorts;
            set => SetProperty(ref _serialPorts, value);
        }

        private string? _connectedCbs;
        public string? ConnectedCbs
        {
            get => _connectedCbs;
            set => SetProperty(ref _connectedCbs, value);
        }

        private bool _hasRemoteConnection;
        public bool HasRemoteConnection
        {
            get => _hasRemoteConnection;
            set => SetProperty(ref _hasRemoteConnection, value);
        }

        private string _installedLocation = string.Empty;
        public string InstalledLocation
        {
            get => _installedLocation;
            set => SetProperty(ref _installedLocation, value);
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
                    _ = UpdateSystemsForShipAsync();
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

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ComponentEntity? Component { get; private set; }

        #endregion

        #region Commands

        public ICommand SaveComponentCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;

        #endregion

        #region Methods

        private void InitializeCommands()
        {
            SaveComponentCommand = new RelayCommand(async () => await SaveComponentAsync(), CanSaveComponent);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading data...";

                // Load ships and systems
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

                StatusMessage = "Data loaded successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
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

                // Clear selected system if it's no longer valid
                if (SelectedSystem != null && SelectedSystem.ShipId != SelectedShip.Id)
                {
                    SelectedSystem = null;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating systems: {ex.Message}";
            }
        }

        private void PopulateFromComponent(ComponentEntity component)
        {
            Id = component.Id;
            SystemId = component.SystemId;
            ComponentName = component.Name;
            MakerModel = component.MakerModel;
            UsbPorts = component.UsbPorts;
            LanPorts = component.LanPorts;
            SerialPorts = component.SerialPorts;
            ConnectedCbs = component.ConnectedCbs;
            HasRemoteConnection = component.HasRemoteConnection;
            InstalledLocation = component.InstalledLocation;

            // Set selected system and ship
            SelectedSystem = Systems.FirstOrDefault(s => s.Id == SystemId);
            if (SelectedSystem != null)
            {
                SelectedShip = Ships.FirstOrDefault(ship => ship.Id == SelectedSystem.ShipId);
            }
        }

        private async Task SaveComponentAsync()
        {
            try
            {
                if (!ValidateComponent())
                {
                    return;
                }

                IsLoading = true;
                StatusMessage = "Saving component...";

                var component = new ComponentEntity
                {
                    Id = Id,
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

                if (IsEditing)
                {
                    Component = await _componentService.UpdateComponentAsync(component);
                    StatusMessage = "Component updated successfully";
                }
                else
                {
                    Component = await _componentService.CreateComponentAsync(component);
                    StatusMessage = "Component created successfully";
                }

                ComponentSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving component: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool ValidateComponent()
        {
            if (string.IsNullOrWhiteSpace(ComponentName))
            {
                StatusMessage = "Component name is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(MakerModel))
            {
                StatusMessage = "Maker/Model is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(InstalledLocation))
            {
                StatusMessage = "Installed location is required";
                return false;
            }

            if (SystemId <= 0)
            {
                StatusMessage = "Please select a system";
                return false;
            }

            if (UsbPorts < 0 || LanPorts < 0 || SerialPorts < 0)
            {
                StatusMessage = "Port counts cannot be negative";
                return false;
            }

            return true;
        }

        private bool CanSaveComponent()
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(ComponentName) &&
                   !string.IsNullOrWhiteSpace(MakerModel) &&
                   !string.IsNullOrWhiteSpace(InstalledLocation) &&
                   SystemId > 0;
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
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
} 