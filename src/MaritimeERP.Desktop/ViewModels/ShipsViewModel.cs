using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Desktop.Views;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Desktop.ViewModels
{
    public class ShipsViewModel : INotifyPropertyChanged
    {
        private readonly IShipService _shipService;
        private readonly IServiceProvider _serviceProvider;
        private ObservableCollection<Ship> _ships = new();
        private ObservableCollection<Ship> _filteredShips = new();
        private ObservableCollection<ShipType> _shipTypes = new();
        private Ship? _selectedShip;
        private ShipType? _selectedShipType;
        private string _searchText = string.Empty;
        private string _selectedStatus = "All";
        private bool _isLoading = false;

        public ShipsViewModel(IShipService shipService, IServiceProvider serviceProvider)
        {
            _shipService = shipService;
            _serviceProvider = serviceProvider;
            
            InitializeCommands();
            _ = LoadDataAsync();
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

        public Ship? SelectedShip
        {
            get => _selectedShip;
            set
            {
                SetProperty(ref _selectedShip, value);
                OnPropertyChanged(nameof(HasSelectedShip));
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

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool HasSelectedShip => SelectedShip != null;

        // Commands
        public ICommand AddShipCommand { get; private set; } = null!;
        public ICommand EditShipCommand { get; private set; } = null!;
        public ICommand DeleteShipCommand { get; private set; } = null!;
        public ICommand ViewDetailsCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;

        private void InitializeCommands()
        {
            AddShipCommand = new AsyncRelayCommand(AddShipAsync);
            EditShipCommand = new AsyncRelayCommand(EditShipAsync, () => HasSelectedShip);
            DeleteShipCommand = new AsyncRelayCommand(DeleteShipAsync, () => HasSelectedShip);
            ViewDetailsCommand = new RelayCommand(ViewDetails, () => HasSelectedShip);
            RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                
                // Load ships and ship types
                var ships = await _shipService.GetAllShipsAsync();
                var shipTypes = await _shipService.GetShipTypeEntitiesAsync();

                Ships.Clear();
                foreach (var ship in ships)
                {
                    Ships.Add(ship);
                }

                ShipTypes.Clear();
                ShipTypes.Add(new ShipType { Id = 0, Name = "All Types" }); // Add "All" option
                foreach (var shipType in shipTypes)
                {
                    ShipTypes.Add(shipType);
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading ships: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
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
                    (s.ShipType?.ToLower().Contains(searchLower) ?? false));
            }

            // Apply ship type filter
            if (SelectedShipType != null && SelectedShipType.Id != 0)
            {
                filtered = filtered.Where(s => s.ShipType == SelectedShipType.Name);
            }

            // Apply status filter
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
                // Open ship details dialog (read-only)
                var dialog = new ShipDetailsDialog();
                var detailsViewModel = new ShipDetailsViewModel(SelectedShip);
                dialog.DataContext = detailsViewModel;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error viewing ship details: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

    public class ShipDetailsViewModel
    {
        public Ship Ship { get; }

        public ShipDetailsViewModel(Ship ship)
        {
            Ship = ship;
        }
    }
} 