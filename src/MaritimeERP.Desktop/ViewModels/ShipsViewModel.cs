using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Desktop.Views;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Desktop.ViewModels
{
    public class ShipsViewModel : ViewModelBase
    {
        private readonly IShipService _shipService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthenticationService _authenticationService;
        private ObservableCollection<Ship> _ships = new();
        private ObservableCollection<Ship> _filteredShips = new();
        private ObservableCollection<ShipType> _shipTypes = new();
        private ObservableCollection<string> _flags = new();
        private Ship? _selectedShip;
        private ShipType? _selectedShipType;
        private string? _selectedFlag;
        private string _searchText = string.Empty;
        private string _selectedStatus = "All";
        private bool _showActiveOnly = false;

        public ShipsViewModel(IShipService shipService, IServiceProvider serviceProvider, IAuthenticationService authenticationService)
        {
            _shipService = shipService;
            _serviceProvider = serviceProvider;
            _authenticationService = authenticationService;
            
            InitializeCommands();
            
            // Load data in background without blocking UI
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    StatusMessage = "Error loading ships data";
                }
            });
        }

        // Properties
        public ObservableCollection<Ship> Ships
        {
            get => _ships;
            set => SetProperty(ref _ships, value);
        }

        public ObservableCollection<Ship> FilteredShips
        {
            get => _filteredShips;
            set => SetProperty(ref _filteredShips, value);
        }

        public ObservableCollection<ShipType> ShipTypes
        {
            get => _shipTypes;
            set => SetProperty(ref _shipTypes, value);
        }

        public ObservableCollection<string> Flags
        {
            get => _flags;
            set => SetProperty(ref _flags, value);
        }

        public Ship? SelectedShip
        {
            get => _selectedShip;
            set
            {
                SetProperty(ref _selectedShip, value);
                OnPropertyChanged(nameof(HasSelectedShip));
                RefreshCommandStates();
            }
        }

        public ShipType? SelectedShipType
        {
            get => _selectedShipType;
            set
            {
                SetProperty(ref _selectedShipType, value);
                ApplyFilters();
            }
        }

        public string? SelectedFlag
        {
            get => _selectedFlag;
            set
            {
                SetProperty(ref _selectedFlag, value);
                ApplyFilters();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                ApplyFilters();
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                SetProperty(ref _selectedStatus, value);
                ApplyFilters();
            }
        }

        public bool ShowActiveOnly
        {
            get => _showActiveOnly;
            set
            {
                SetProperty(ref _showActiveOnly, value);
                ApplyFilters();
            }
        }



        public bool HasSelectedShip => SelectedShip != null;
        
        public int TotalShips => Ships.Count;
        
        public int ActiveShips => Ships.Count(s => s.IsActive);

        // Permission properties
        public bool CanEditData => _authenticationService.CurrentUser?.Role?.Name == "Administrator";
        public bool IsReadOnlyUser => _authenticationService.CurrentUser?.Role?.Name == "Engineer";

        // Commands
        public ICommand AddShipCommand { get; private set; } = null!;
        public ICommand EditShipCommand { get; private set; } = null!;
        public ICommand DeleteShipCommand { get; private set; } = null!;
        public ICommand ViewDetailsCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddShipCommand = new AsyncRelayCommand(AddShipAsync, () => CanEditData && !IsLoading);
            EditShipCommand = new AsyncRelayCommand(EditShipAsync, () => HasSelectedShip && CanEditData && !IsLoading);
            DeleteShipCommand = new AsyncRelayCommand(DeleteShipAsync, () => HasSelectedShip && CanEditData && !IsLoading);
            ViewDetailsCommand = new RelayCommand(ViewDetails, () => HasSelectedShip);
            RefreshCommand = new AsyncRelayCommand(RefreshDataAsync, () => !IsLoading);
        }

        protected override async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading ships...";
                
                // Load ships and ship types in parallel
                var shipsTask = _shipService.GetAllShipsAsync();
                var shipTypesTask = _shipService.GetShipTypeEntitiesAsync();

                await Task.WhenAll(shipsTask, shipTypesTask);

                var ships = await shipsTask;
                var shipTypes = await shipTypesTask;

                // Update UI on main thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                Ships.Clear();
                foreach (var ship in ships)
                {
                    Ships.Add(ship);
                }

                // Populate ShipTypes dropdown
                ShipTypes.Clear();
                ShipTypes.Add(new ShipType { Id = 0, Name = "All Types" }); // Add "All" option
                foreach (var shipType in shipTypes)
                {
                    ShipTypes.Add(shipType);
                }

                // Populate Flags dropdown with unique flags from ships
                Flags.Clear();
                Flags.Add("All Flags"); // Add "All" option
                var uniqueFlags = ships.Where(s => !string.IsNullOrEmpty(s.Flag))
                                      .Select(s => s.Flag)
                                      .Distinct()
                                      .OrderBy(f => f);
                foreach (var flag in uniqueFlags)
                {
                    Flags.Add(flag);
                }

                ApplyFilters();
                
                // Notify property changes for computed properties
                OnPropertyChanged(nameof(TotalShips));
                OnPropertyChanged(nameof(ActiveShips));
                
                StatusMessage = $"Loaded {TotalShips} ships successfully";
                    IsDataLoaded = true;
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusMessage = $"Error loading ships: {ex.Message}";
                });
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsLoading = false;
                    RefreshCommandStates();
                });
            }
        }

        private void ApplyFilters()
        {
            var filtered = Ships.AsEnumerable();

            // Apply text search
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(s => 
                    s.ShipName.ToLower().Contains(searchLower) ||
                    s.ImoNumber.ToLower().Contains(searchLower) ||
                    s.Flag.ToLower().Contains(searchLower) ||
                    (s.ShipType?.ToLower().Contains(searchLower) ?? false) ||
                    (s.Owner?.ToLower().Contains(searchLower) ?? false));
            }

            // Apply ship type filter
            if (SelectedShipType != null && SelectedShipType.Id != 0)
            {
                filtered = filtered.Where(s => s.ShipType == SelectedShipType.Name);
            }

            // Apply flag filter
            if (!string.IsNullOrEmpty(SelectedFlag) && SelectedFlag != "All Flags")
            {
                filtered = filtered.Where(s => s.Flag == SelectedFlag);
            }

            // Apply active only filter
            if (ShowActiveOnly)
            {
                filtered = filtered.Where(s => s.IsActive);
            }

            // Apply status filter (legacy support)
            if (SelectedStatus != "All")
            {
                filtered = filtered.Where(s => 
                    (SelectedStatus == "Active" && s.IsActive) ||
                    (SelectedStatus == "Inactive" && !s.IsActive));
            }

            FilteredShips.Clear();
            foreach (var ship in filtered.OrderBy(s => s.ShipName))
            {
                FilteredShips.Add(ship);
            }
            
            // Update status message
            var totalFiltered = FilteredShips.Count;
            StatusMessage = totalFiltered == Ships.Count 
                ? $"Showing all {totalFiltered} ships" 
                : $"Showing {totalFiltered} of {Ships.Count} ships";
        }

        private async Task AddShipAsync()
        {
            try
            {
                // Create a new ship dialog
                var dialog = new ShipEditDialog();
                var editViewModel = new ShipEditViewModel(_shipService, _serviceProvider);
                dialog.DataContext = editViewModel;

                if (dialog.ShowDialog() == true)
                {
                    await LoadDataAsync(); // Refresh the list
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding ship: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task EditShipAsync()
        {
            if (SelectedShip == null) return;

            try
            {
                // Open ship edit dialog
                var dialog = new ShipEditDialog();
                var editViewModel = new ShipEditViewModel(_shipService, _serviceProvider, SelectedShip);
                dialog.DataContext = editViewModel;

                if (dialog.ShowDialog() == true)
                {
                    await LoadDataAsync(); // Refresh the list
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing ship: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteShipAsync()
        {
            if (SelectedShip == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete the ship '{SelectedShip.ShipName}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _shipService.DeleteShipAsync(SelectedShip.Id);
                    await LoadDataAsync(); // Refresh the list
                    SelectedShip = null;

                    MessageBox.Show("Ship deleted successfully.", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting ship: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewDetails()
        {
            if (SelectedShip == null) return;

            try
            {
                // Create ship details view model with all required services
                var shipService = _serviceProvider.GetRequiredService<IShipService>();
                var systemService = _serviceProvider.GetRequiredService<ISystemService>();
                var componentService = _serviceProvider.GetRequiredService<IComponentService>();
                var softwareService = _serviceProvider.GetRequiredService<ISoftwareService>();
                var logger = _serviceProvider.GetRequiredService<ILogger<ShipDetailsViewModel>>();

                var detailsViewModel = new ShipDetailsViewModel(
                    SelectedShip,
                    shipService,
                    systemService,
                    componentService,
                    softwareService,
                    logger);

                // Open beautiful ship details dialog
                var dialog = new ShipDetailsDialog(detailsViewModel);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error viewing ship details: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshDataAsync()
        {
            await base.RefreshDataAsync();
        }

        private void RefreshCommandStates()
        {
            ((AsyncRelayCommand)AddShipCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)EditShipCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)DeleteShipCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ViewDetailsCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)RefreshCommand).RaiseCanExecuteChanged();
        }

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
    }

    // Ship Edit ViewModel - moved to separate file
    // See ShipEditViewModel.cs for implementation
} 