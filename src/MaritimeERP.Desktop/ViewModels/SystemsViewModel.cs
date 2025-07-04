using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MaritimeERP.Desktop.ViewModels
{
    public class SystemsViewModel : INotifyPropertyChanged
    {
        private readonly ISystemService _systemService;
        private readonly IShipService _shipService;
        private readonly ILogger<SystemsViewModel> _logger;

        // Collections
        public ObservableCollection<ShipSystem> Systems { get; set; } = new();
        public ObservableCollection<Ship> Ships { get; set; } = new();
        public ObservableCollection<SystemCategory> Categories { get; set; } = new();
        public ObservableCollection<SecurityZone> SecurityZones { get; set; } = new();
        public ObservableCollection<string> Manufacturers { get; set; } = new();

        // Selected items
        private ShipSystem? _selectedSystem;
        public ShipSystem? SelectedSystem
        {
            get => _selectedSystem;
            set
            {
                _selectedSystem = value;
                OnPropertyChanged();
                OnSelectedSystemChanged();
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
                UpdateSystemFromShip();
            }
        }

        private SystemCategory? _selectedCategory;
        public SystemCategory? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                UpdateSystemFromCategory();
            }
        }

        private SecurityZone? _selectedSecurityZone;
        public SecurityZone? SelectedSecurityZone
        {
            get => _selectedSecurityZone;
            set
            {
                _selectedSecurityZone = value;
                OnPropertyChanged();
                UpdateSystemFromSecurityZone();
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
                _ = SearchSystemsAsync();
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
                _ = FilterSystemsAsync();
            }
        }

        private int? _filterCategoryId;
        public int? FilterCategoryId
        {
            get => _filterCategoryId;
            set
            {
                _filterCategoryId = value;
                OnPropertyChanged();
                _ = FilterSystemsAsync();
            }
        }

        private string? _filterManufacturer;
        public string? FilterManufacturer
        {
            get => _filterManufacturer;
            set
            {
                _filterManufacturer = value;
                OnPropertyChanged();
                _ = FilterSystemsAsync();
            }
        }

        // Form properties for current system
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

        private int _shipId;
        public int ShipId
        {
            get => _shipId;
            set
            {
                _shipId = value;
                OnPropertyChanged();
            }
        }

        private string _systemName = string.Empty;
        public string SystemName
        {
            get => _systemName;
            set
            {
                _systemName = value;
                OnPropertyChanged();
            }
        }

        private string _manufacturer = string.Empty;
        public string Manufacturer
        {
            get => _manufacturer;
            set
            {
                _manufacturer = value;
                OnPropertyChanged();
            }
        }

        private string _model = string.Empty;
        public string Model
        {
            get => _model;
            set
            {
                _model = value;
                OnPropertyChanged();
            }
        }

        private string _serialNumber = string.Empty;
        public string SerialNumber
        {
            get => _serialNumber;
            set
            {
                _serialNumber = value;
                OnPropertyChanged();
            }
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        private int _categoryId;
        public int CategoryId
        {
            get => _categoryId;
            set
            {
                _categoryId = value;
                OnPropertyChanged();
            }
        }

        private int _securityZoneId;
        public int SecurityZoneId
        {
            get => _securityZoneId;
            set
            {
                _securityZoneId = value;
                OnPropertyChanged();
            }
        }

        // Status properties
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
        public ICommand AddSystemCommand { get; }
        public ICommand EditSystemCommand { get; }
        public ICommand SaveSystemCommand { get; }
        public ICommand DeleteSystemCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public SystemsViewModel(ISystemService systemService, IShipService shipService, ILogger<SystemsViewModel> logger)
        {
            _systemService = systemService;
            _shipService = shipService;
            _logger = logger;

            // Initialize commands
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
            AddSystemCommand = new RelayCommand(AddSystem);
            EditSystemCommand = new RelayCommand(EditSystem, () => SelectedSystem != null);
            SaveSystemCommand = new RelayCommand(async () => await SaveSystemAsync(), CanSaveSystem);
            DeleteSystemCommand = new RelayCommand(async () => await DeleteSystemAsync(), () => SelectedSystem != null);
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
                StatusMessage = "Loading systems data...";

                // Load all data in parallel
                var systemsTask = _systemService.GetAllSystemsAsync();
                var shipsTask = _shipService.GetAllShipsAsync();
                var categoriesTask = _systemService.GetSystemCategoriesAsync();
                var securityZonesTask = _systemService.GetSecurityZonesAsync();
                var manufacturersTask = _systemService.GetManufacturersAsync();

                await Task.WhenAll(systemsTask, shipsTask, categoriesTask, securityZonesTask, manufacturersTask);

                // Update collections
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

                Categories.Clear();
                foreach (var category in await categoriesTask)
                {
                    Categories.Add(category);
                }

                SecurityZones.Clear();
                foreach (var zone in await securityZonesTask)
                {
                    SecurityZones.Add(zone);
                }

                Manufacturers.Clear();
                foreach (var manufacturer in await manufacturersTask)
                {
                    Manufacturers.Add(manufacturer);
                }

                StatusMessage = $"Loaded {Systems.Count} systems successfully";
                _logger.LogInformation("Systems data loaded successfully");
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading systems data";
                _logger.LogError(ex, "Error loading systems data");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchSystemsAsync()
        {
            try
            {
                IsLoading = true;
                var systems = await _systemService.SearchSystemsAsync(SearchText);
                
                Systems.Clear();
                foreach (var system in systems)
                {
                    Systems.Add(system);
                }

                StatusMessage = $"Found {Systems.Count} systems";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error searching systems";
                _logger.LogError(ex, "Error searching systems");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FilterSystemsAsync()
        {
            try
            {
                IsLoading = true;
                IEnumerable<ShipSystem> systems;

                if (FilterShipId.HasValue)
                {
                    systems = await _systemService.GetSystemsByShipIdAsync(FilterShipId.Value);
                }
                else if (FilterCategoryId.HasValue)
                {
                    systems = await _systemService.GetSystemsByCategoryAsync(FilterCategoryId.Value);
                }
                else if (!string.IsNullOrEmpty(FilterManufacturer))
                {
                    systems = await _systemService.GetSystemsByManufacturerAsync(FilterManufacturer);
                }
                else
                {
                    systems = await _systemService.GetAllSystemsAsync();
                }

                Systems.Clear();
                foreach (var system in systems)
                {
                    Systems.Add(system);
                }

                StatusMessage = $"Filtered to {Systems.Count} systems";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error filtering systems";
                _logger.LogError(ex, "Error filtering systems");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnSelectedSystemChanged()
        {
            if (SelectedSystem != null)
            {
                PopulateFormFromSelectedSystem();
            }
        }

        private void PopulateFormFromSelectedSystem()
        {
            if (SelectedSystem == null) return;

            SystemId = SelectedSystem.Id;
            ShipId = SelectedSystem.ShipId;
            SystemName = SelectedSystem.Name;
            Manufacturer = SelectedSystem.Manufacturer;
            Model = SelectedSystem.Model;
            SerialNumber = SelectedSystem.SerialNumber;
            Description = SelectedSystem.Description;
            CategoryId = SelectedSystem.CategoryId;
            SecurityZoneId = SelectedSystem.SecurityZoneId;

            // Update selected items in combos
            SelectedShip = Ships.FirstOrDefault(s => s.Id == ShipId);
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == CategoryId);
            SelectedSecurityZone = SecurityZones.FirstOrDefault(z => z.Id == SecurityZoneId);
        }

        private void UpdateSystemFromShip()
        {
            if (SelectedShip != null)
            {
                ShipId = SelectedShip.Id;
            }
        }

        private void UpdateSystemFromCategory()
        {
            if (SelectedCategory != null)
            {
                CategoryId = SelectedCategory.Id;
            }
        }

        private void UpdateSystemFromSecurityZone()
        {
            if (SelectedSecurityZone != null)
            {
                SecurityZoneId = SelectedSecurityZone.Id;
            }
        }

        private void AddSystem()
        {
            ClearForm();
            IsEditing = true;
            StatusMessage = "Adding new system";
        }

        private void EditSystem()
        {
            if (SelectedSystem != null)
            {
                IsEditing = true;
                StatusMessage = $"Editing system: {SelectedSystem.Name}";
            }
        }

        private async Task SaveSystemAsync()
        {
            try
            {
                IsLoading = true;

                var system = new ShipSystem
                {
                    Id = SystemId,
                    ShipId = ShipId,
                    Name = SystemName,
                    Manufacturer = Manufacturer,
                    Model = Model,
                    SerialNumber = SerialNumber,
                    Description = Description,
                    CategoryId = CategoryId,
                    SecurityZoneId = SecurityZoneId
                };

                if (SystemId == 0)
                {
                    // Create new system
                    var createdSystem = await _systemService.CreateSystemAsync(system);
                    Systems.Add(createdSystem);
                    StatusMessage = "System created successfully";
                }
                else
                {
                    // Update existing system
                    var updatedSystem = await _systemService.UpdateSystemAsync(system);
                    var index = Systems.ToList().FindIndex(s => s.Id == SystemId);
                    if (index >= 0)
                    {
                        Systems[index] = updatedSystem;
                    }
                    StatusMessage = "System updated successfully";
                }

                IsEditing = false;
                ClearForm();
                _logger.LogInformation("System saved successfully: {SystemName}", SystemName);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving system: {ex.Message}";
                _logger.LogError(ex, "Error saving system");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteSystemAsync()
        {
            if (SelectedSystem == null) return;

            try
            {
                IsLoading = true;
                var success = await _systemService.DeleteSystemAsync(SelectedSystem.Id);
                
                if (success)
                {
                    Systems.Remove(SelectedSystem);
                    StatusMessage = "System deleted successfully";
                    ClearForm();
                    _logger.LogInformation("System deleted: {SystemId}", SelectedSystem.Id);
                }
                else
                {
                    StatusMessage = "System not found or could not be deleted";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting system: {ex.Message}";
                _logger.LogError(ex, "Error deleting system");
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
            SystemId = 0;
            ShipId = 0;
            SystemName = string.Empty;
            Manufacturer = string.Empty;
            Model = string.Empty;
            SerialNumber = string.Empty;
            Description = null;
            CategoryId = 0;
            SecurityZoneId = 0;
            SelectedShip = null;
            SelectedCategory = null;
            SelectedSecurityZone = null;
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            FilterShipId = null;
            FilterCategoryId = null;
            FilterManufacturer = null;
            _ = LoadDataAsync();
        }

        private bool CanSaveSystem()
        {
            return IsEditing && 
                   !string.IsNullOrWhiteSpace(SystemName) &&
                   !string.IsNullOrWhiteSpace(Manufacturer) &&
                   !string.IsNullOrWhiteSpace(Model) &&
                   !string.IsNullOrWhiteSpace(SerialNumber) &&
                   ShipId > 0 &&
                   CategoryId > 0 &&
                   SecurityZoneId > 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 