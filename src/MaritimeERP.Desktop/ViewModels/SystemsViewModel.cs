using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Core.Interfaces;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace MaritimeERP.Desktop.ViewModels
{
    public class SystemsViewModel : INotifyPropertyChanged
    {
        private readonly ISystemService _systemService;
        private readonly IShipService _shipService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IDataChangeNotificationService _dataChangeNotificationService;
        private readonly ILogger<SystemsViewModel> _logger;
        private readonly INavigationService _navigationService;

        // Collections
        public ObservableCollection<ShipSystem> Systems { get; set; } = new();
        private ObservableCollection<ShipSystem> _filteredSystems = new();
        public ObservableCollection<ShipSystem> FilteredSystems 
        { 
            get => _filteredSystems; 
            set 
            { 
                _filteredSystems = value; 
                OnPropertyChanged(); 
            } 
        }
        public ObservableCollection<Ship> Ships { get; set; } = new();
        public ObservableCollection<SystemCategory> Categories { get; set; } = new();

        public ObservableCollection<string> Manufacturers { get; set; } = new();
        public ObservableCollection<string> SystemTypes { get; set; } = new();

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
                if (_selectedShip != value)
                {
                    _selectedShip = value;
                    OnPropertyChanged();
                    UpdateSystemFromShip();
                    ApplyFilters(); // Apply filtering when ship selection changes
                    _logger.LogDebug("SelectedShip changed to {ShipName}", value?.ShipName ?? "null");
                }
            }
        }

        private SystemCategory? _selectedCategory;
        public SystemCategory? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    UpdateSystemFromCategory();
                    _logger.LogDebug("SelectedCategory changed to {CategoryName}", value?.Name ?? "null");
                }
            }
        }

        private string? _selectedSystemType;
        public string? SelectedSystemType
        {
            get => _selectedSystemType;
            set
            {
                _selectedSystemType = value;
                OnPropertyChanged();
            }
        }

        private string? _securityZoneText;
        public string? SecurityZoneText
        {
            get => _securityZoneText;
            set
            {
                _securityZoneText = value;
                OnPropertyChanged();
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

        private bool _showActiveOnly = false;
        public bool ShowActiveOnly
        {
            get => _showActiveOnly;
            set
            {
                _showActiveOnly = value;
                OnPropertyChanged();
                ApplyFilters();
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
                ApplyFilters();
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
                ApplyFilters();
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
                ApplyFilters();
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
                if (_systemName != value)
                {
                    _systemName = value;
                    OnPropertyChanged();
                    RefreshSaveCommand();
                }
            }
        }

        private string _systemLocation = string.Empty;
        public string SystemLocation
        {
            get => _systemLocation;
            set
            {
                _systemLocation = value;
                OnPropertyChanged();
            }
        }

        private string _systemDescription = string.Empty;
        public string SystemDescription
        {
            get => _systemDescription;
            set
            {
                _systemDescription = value;
                OnPropertyChanged();
            }
        }

        private DateTime? _installationDate;
        public DateTime? InstallationDate
        {
            get => _installationDate;
            set
            {
                _installationDate = value;
                OnPropertyChanged();
            }
        }

        private bool _isSystemActive = true;
        public bool IsSystemActive
        {
            get => _isSystemActive;
            set
            {
                _isSystemActive = value;
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

        private string _manufacturer = string.Empty;
        public string Manufacturer
        {
            get => _manufacturer;
            set
            {
                if (_manufacturer != value)
                {
                    _manufacturer = value;
                    OnPropertyChanged();
                    RefreshSaveCommand();
                }
            }
        }

        private string _model = string.Empty;
        public string Model
        {
            get => _model;
            set
            {
                if (_model != value)
                {
                    _model = value;
                    OnPropertyChanged();
                    RefreshSaveCommand();
                }
            }
        }

        private string _serialNumber = string.Empty;
        public string SerialNumber
        {
            get => _serialNumber;
            set
            {
                if (_serialNumber != value)
                {
                    _serialNumber = value;
                    OnPropertyChanged();
                    RefreshSaveCommand();
                }
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
                RefreshSaveCommand();
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
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged();
                    RefreshSaveCommand();
                }
            }
        }

        // Computed properties
        public int TotalSystems => Systems.Count;
        public int ActiveSystems => Systems.Count(s => s.Name != null); // Placeholder until we have IsActive property

        public bool HasSelectedShip => SelectedShip != null;
        
        public int TotalShips => Ships.Count;
        
        public int ActiveShips => Ships.Count(s => s.IsActive);

        // Permission properties
        public bool CanEditData => _authenticationService.CurrentUser?.Role?.Name == "Administrator";
        public bool IsReadOnlyUser => _authenticationService.CurrentUser?.Role?.Name == "Engineer";

        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand AddSystemCommand { get; }
        public ICommand EditSystemCommand { get; }
        public ICommand DeleteSystemCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand NavigateToComponentsCommand { get; }

        private ICommand? _saveSystemCommand;
        public ICommand SaveSystemCommand 
        { 
            get => _saveSystemCommand ??= new RelayCommand(async () => await SaveSystemAsync(), () => CanSaveSystem());
            private set => _saveSystemCommand = value;
        }

        public SystemsViewModel(
            ISystemService systemService,
            IShipService shipService,
            IAuthenticationService authenticationService,
            IDataChangeNotificationService dataChangeNotificationService,
            ILogger<SystemsViewModel> logger,
            INavigationService navigationService)
        {
            _systemService = systemService;
            _shipService = shipService;
            _authenticationService = authenticationService;
            _dataChangeNotificationService = dataChangeNotificationService;
            _logger = logger;
            _navigationService = navigationService;

            // Initialize commands
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            AddSystemCommand = new RelayCommand(AddSystem, () => !IsLoading && !IsEditing && CanEditData);
            EditSystemCommand = new RelayCommand(EditSystem, () => SelectedSystem != null && !IsLoading && !IsEditing && CanEditData);
            DeleteSystemCommand = new RelayCommand(async () => await DeleteSystemAsync(), () => SelectedSystem != null && !IsLoading && !IsEditing && CanEditData);
            NavigateToComponentsCommand = new RelayCommand(NavigateToComponents, () => SelectedSystem != null);
            CancelEditCommand = new RelayCommand(CancelEdit);
            SaveSystemCommand = new RelayCommand(async () => await SaveSystemAsync(), () => CanSaveSystem());
            ClearFiltersCommand = new RelayCommand(ClearFilters);

            // Subscribe to ship data changes
            SubscribeToShipDataChanges();

            // Load data with proper error handling
            Task.Run(async () => 
            {
                try
                {
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load initial data in SystemsViewModel constructor");
                    StatusMessage = "Failed to load initial data. Please try refreshing.";
                }
            });
        }

        private void SubscribeToShipDataChanges()
                {
            _dataChangeNotificationService.DataChanged += OnDataChanged;
                }

        private async void OnDataChanged(object? sender, MaritimeERP.Core.Interfaces.DataChangeEventArgs e)
            {
                try
                {
                // Refresh ship data when ships are created, updated, or deleted
                if (e.DataType == "Ship" && (e.Operation == "CREATE" || e.Operation == "UPDATE" || e.Operation == "DELETE"))
                {
                    _logger.LogInformation("SystemsViewModel received ship data change notification: {DataType} - {Operation}", e.DataType, e.Operation);
                    await RefreshShipDataAsync();
                }
                }
                catch (Exception ex)
                {
                _logger.LogError(ex, "Error handling ship data change notification in SystemsViewModel");
            }
        }

        private async Task RefreshShipDataAsync()
        {
            try
            {
                var ships = await _shipService.GetAllShipsAsync();
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var selectedShipId = SelectedShip?.Id;
                    
                    Ships.Clear();
                    foreach (var ship in ships)
                    {
                        Ships.Add(ship);
                    }
                    
                    // Restore selected ship if it still exists
                    if (selectedShipId.HasValue)
                    {
                        SelectedShip = Ships.FirstOrDefault(s => s.Id == selectedShipId.Value);
                    }
                    
                    _logger.LogInformation("SystemsViewModel refreshed ship data - {ShipCount} ships loaded", Ships.Count);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing ship data in SystemsViewModel");
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Set loading state on UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "Loading systems data...";
                });
                
                _logger.LogInformation("Starting LoadDataAsync");

                // Load all data in parallel
                var systemsTask = _systemService.GetAllSystemsAsync();
                var shipsTask = _shipService.GetAllShipsAsync();
                var categoriesTask = _systemService.GetSystemCategoriesAsync();
                var manufacturersTask = _systemService.GetManufacturersAsync();

                await Task.WhenAll(systemsTask, shipsTask, categoriesTask, manufacturersTask);

                // Get the results
                var systems = await systemsTask;
                var ships = await shipsTask;
                var categories = await categoriesTask;
                var manufacturers = await manufacturersTask;

                // Update collections on UI thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Systems.Clear();
                    foreach (var system in systems)
                    {
                        Systems.Add(system);
                    }
                    _logger.LogInformation("Loaded {SystemCount} systems", Systems.Count);

                    Ships.Clear();
                    foreach (var ship in ships)
                    {
                        Ships.Add(ship);
                    }
                    _logger.LogInformation("Loaded {ShipCount} ships", Ships.Count);

                    Categories.Clear();
                    foreach (var category in categories)
                    {
                        Categories.Add(category);
                    }
                    _logger.LogInformation("Loaded {CategoryCount} categories", Categories.Count);

                    Manufacturers.Clear();
                    foreach (var manufacturer in manufacturers)
                    {
                        Manufacturers.Add(manufacturer);
                    }
                    _logger.LogInformation("Loaded {ManufacturerCount} manufacturers", Manufacturers.Count);

                    StatusMessage = $"Loaded {Systems.Count} systems successfully";
                    _logger.LogInformation("Systems data loaded successfully");
                    
                    // Apply initial filters
                    ApplyFilters();
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error loading systems data";
                });
                _logger.LogError(ex, "Error loading systems data");
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private async Task SearchSystemsAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                });
                
                var systems = await _systemService.SearchSystemsAsync(SearchText);
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Systems.Clear();
                    foreach (var system in systems)
                    {
                        Systems.Add(system);
                    }
                    StatusMessage = $"Found {Systems.Count} systems";
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error searching systems";
                });
                _logger.LogError(ex, "Error searching systems");
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private async Task FilterSystemsAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                });
                
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

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Systems.Clear();
                    foreach (var system in systems)
                    {
                        Systems.Add(system);
                    }
                    StatusMessage = $"Filtered to {Systems.Count} systems";
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error filtering systems";
                });
                _logger.LogError(ex, "Error filtering systems");
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
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
            SystemName = SelectedSystem.Name;
            Manufacturer = SelectedSystem.Manufacturer;
            Model = SelectedSystem.Model;
            SerialNumber = SelectedSystem.SerialNumber;
            Description = SelectedSystem.Description;
            HasRemoteConnection = SelectedSystem.HasRemoteConnection;
            CategoryId = SelectedSystem.CategoryId;
            SecurityZoneText = SelectedSystem.SecurityZoneText;
            ShipId = SelectedSystem.ShipId;

            // Set selected items
            SelectedShip = Ships.FirstOrDefault(s => s.Id == SelectedSystem.ShipId);
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == SelectedSystem.CategoryId);
        }

        private void UpdateSystemFromShip()
        {
            if (SelectedShip != null)
            {
                ShipId = SelectedShip.Id;
                RefreshSaveCommand();
            }
        }

        private void UpdateSystemFromCategory()
        {
            if (SelectedCategory != null)
            {
                CategoryId = SelectedCategory.Id;
                RefreshSaveCommand();
            }
        }



        private void AddSystem()
        {
            _logger.LogDebug("AddSystem called");
            _logger.LogInformation("AddSystem: Ships count = {ShipCount}, Categories count = {CategoryCount}", 
                Ships.Count, Categories.Count);
            
            // If no data is loaded, try to load it first
            if (Ships.Count == 0 || Categories.Count == 0)
            {
                _logger.LogWarning("No ships or categories loaded. Attempting to reload data...");
                StatusMessage = "Loading required data...";
                
                Task.Run(async () =>
                {
                    try
                    {
                        await LoadDataAsync();
                        // After loading, try to add system again
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (Ships.Count > 0 && Categories.Count > 0)
                            {
                                ClearForm();
                                IsEditing = true;
                                StatusMessage = "Adding new system - fill in all required fields";
                                RefreshSaveCommand();
                            }
                            else
                            {
                                StatusMessage = "Error: Unable to load ships and categories data";
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load data when adding system");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = "Error loading data. Please try again.";
                        });
                    }
                });
                return;
            }
            
            ClearForm();
            IsEditing = true;
            
            StatusMessage = "Adding new system - fill in all required fields";
            
            _logger.LogInformation("AddSystem: After setting IsEditing=true, Ships count = {ShipCount}, Categories count = {CategoryCount}", 
                Ships.Count, Categories.Count);
            
            RefreshSaveCommand();
        }

        private void EditSystem()
        {
            if (SelectedSystem != null)
            {
                IsEditing = true;
                StatusMessage = $"Editing system: {SelectedSystem.Name}";
                RefreshSaveCommand();
            }
        }

        private async Task SaveSystemAsync()
        {
            if (!CanSaveSystem())
            {
                StatusMessage = "Please fill in all required fields";
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "Saving system...";

                var system = new ShipSystem
                {
                    Id = SystemId,
                    ShipId = SelectedShip?.Id ?? throw new InvalidOperationException("Ship must be selected"),
                    CategoryId = SelectedCategory?.Id ?? throw new InvalidOperationException("Category must be selected"),
                    SecurityZoneText = SecurityZoneText?.Trim(),
                    Name = SystemName?.Trim() ?? throw new InvalidOperationException("System name is required"),
                    Manufacturer = Manufacturer?.Trim() ?? throw new InvalidOperationException("Manufacturer is required"),
                    Model = Model?.Trim() ?? throw new InvalidOperationException("Model is required"),
                    SerialNumber = SerialNumber?.Trim() ?? throw new InvalidOperationException("Serial number is required"),
                    Description = Description?.Trim(),
                    HasRemoteConnection = HasRemoteConnection
                };

                if (SystemId == 0)
                {
                    // Create new system
                    system.CreatedAt = DateTime.UtcNow;
                    var createdSystem = await _systemService.CreateSystemAsync(system);
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Systems.Add(createdSystem);
                        ApplyFilters(); // Update filtered collection
                        StatusMessage = "System created successfully";
                    });
                    
                    // Notify dashboard of new system
                    _dataChangeNotificationService.NotifyDataChanged("System", "Created", createdSystem);
                    
                    _logger.LogInformation("System created successfully: {SystemName}", SystemName);
                }
                else
                {
                    // Update existing system
                    system.UpdatedAt = DateTime.UtcNow;
                    var updatedSystem = await _systemService.UpdateSystemAsync(system);
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var index = Systems.ToList().FindIndex(s => s.Id == SystemId);
                        if (index >= 0)
                        {
                            Systems[index] = updatedSystem;
                        }
                        ApplyFilters(); // Update filtered collection
                        StatusMessage = "System updated successfully";
                    });
                    
                    // Notify dashboard of updated system
                    _dataChangeNotificationService.NotifyDataChanged("System", "Updated", updatedSystem);
                    
                    _logger.LogInformation("System updated successfully: {SystemName}", SystemName);
                }

                IsEditing = false;
                ClearForm();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving system: {ex.Message}";
                _logger.LogError(ex, "Error saving system: {Message}", ex.Message);
                MessageBox.Show(
                    $"Error saving system: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                    var deletedSystem = SelectedSystem;
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Systems.Remove(SelectedSystem);
                        ApplyFilters(); // Update filtered collection
                        StatusMessage = "System deleted successfully";
                        ClearForm();
                    });
                    
                    // Notify dashboard of deleted system
                    _dataChangeNotificationService.NotifyDataChanged("System", "Deleted", deletedSystem);
                    
                    _logger.LogInformation("System deleted: {SystemId}", deletedSystem.Id);
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
            _logger.LogDebug("ClearForm called");
            SystemId = 0;
            ShipId = 0;
            SystemName = string.Empty;
            Manufacturer = string.Empty;
            Model = string.Empty;
            SerialNumber = string.Empty;
            Description = null;
            CategoryId = 0;
            SecurityZoneText = null;
            HasRemoteConnection = false;
            SelectedShip = null;
            SelectedCategory = null;
            RefreshSaveCommand();
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedShip = null;
            FilterShipId = null;
            FilterCategoryId = null;
            FilterManufacturer = null;
            ApplyFilters();
        }

        private void NavigateToComponents()
        {
            if (SelectedSystem != null)
            {
                _navigationService.NavigateToPageWithFilter("Components", SelectedSystem);
            }
        }

        private bool CanSaveSystem()
        {
            // Check permissions first
            if (!CanEditData)
                return false;

            // Check if we have required data
            var hasRequiredData = !string.IsNullOrWhiteSpace(SystemName) &&
                                 !string.IsNullOrWhiteSpace(Manufacturer) &&
                                 !string.IsNullOrWhiteSpace(Model) &&
                                 !string.IsNullOrWhiteSpace(SerialNumber) &&
                                 SelectedShip != null &&
                                 SelectedCategory != null;

            return hasRequiredData && IsEditing;
        }

        private void ApplyFilters()
        {
            // Simple filtering implementation
            var filtered = Systems.AsEnumerable();

            // Apply ship filter
            if (SelectedShip != null)
            {
                filtered = filtered.Where(s => s.ShipId == SelectedShip.Id);
            }

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(s => 
                    s.Name.ToLower().Contains(searchLower) ||
                    s.Manufacturer.ToLower().Contains(searchLower) ||
                    s.Model.ToLower().Contains(searchLower) ||
                    (s.Ship?.ShipName?.ToLower().Contains(searchLower) ?? false));
            }

            // Note: Since ShipSystem doesn't have IsActive property, we'll skip the active filter for now
            // if (ShowActiveOnly)
            // {
            //     filtered = filtered.Where(s => s.IsActive);
            // }

            // Update the filtered collection
            FilteredSystems.Clear();
            foreach (var system in filtered.OrderBy(s => s.Ship?.ShipName ?? "").ThenBy(s => s.Name))
            {
                FilteredSystems.Add(system);
            }

            // Update status message
            var resultCount = FilteredSystems.Count;
            StatusMessage = resultCount == Systems.Count 
                ? $"Showing all {resultCount} systems" 
                : $"Showing {resultCount} of {Systems.Count} systems";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RefreshSaveCommand()
        {
            _logger.LogDebug("RefreshSaveCommand called. Current state: " +
                $"IsEditing={IsEditing}, " +
                $"SystemName='{SystemName}', " +
                $"Manufacturer='{Manufacturer}', " +
                $"Model='{Model}', " +
                $"SerialNumber='{SerialNumber}', " +
                $"SelectedShip={SelectedShip?.ShipName ?? "null"}, " +
                $"SelectedCategory={SelectedCategory?.Name ?? "null"}");

            if (SaveSystemCommand is RelayCommand cmd)
            {
                cmd.RaiseCanExecuteChanged();
                _logger.LogDebug("SaveSystemCommand.CanExecute = {CanExecute}", cmd.CanExecute(null));
            }
            else
            {
                _logger.LogWarning("SaveSystemCommand is not a RelayCommand");
            }
        }
    }
} 