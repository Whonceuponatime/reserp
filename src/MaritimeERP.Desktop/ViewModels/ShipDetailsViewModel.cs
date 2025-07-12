using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MaritimeERP.Core.Entities;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace MaritimeERP.Desktop.ViewModels
{
    public class ShipDetailsViewModel : INotifyPropertyChanged
    {
        private readonly IShipService _shipService;
        private readonly ISystemService _systemService;
        private readonly IComponentService _componentService;
        private readonly ISoftwareService _softwareService;
        private readonly ILogger<ShipDetailsViewModel> _logger;

        private Ship _ship;
        private bool _isLoading;
        private int _systemsCount;
        private int _componentsCount;
        private int _softwareCount;

        public ShipDetailsViewModel(
            Ship ship,
            IShipService shipService,
            ISystemService systemService,
            IComponentService componentService,
            ISoftwareService softwareService,
            ILogger<ShipDetailsViewModel> logger)
        {
            _ship = ship ?? throw new ArgumentNullException(nameof(ship));
            _shipService = shipService ?? throw new ArgumentNullException(nameof(shipService));
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
            _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
            _softwareService = softwareService ?? throw new ArgumentNullException(nameof(softwareService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Systems = new ObservableCollection<SystemWithComponents>();

            // Load data asynchronously
            _ = LoadShipDetailsAsync();
        }

        public Ship Ship
        {
            get => _ship;
            set => SetProperty(ref _ship, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int SystemsCount
        {
            get => _systemsCount;
            set => SetProperty(ref _systemsCount, value);
        }

        public int ComponentsCount
        {
            get => _componentsCount;
            set => SetProperty(ref _componentsCount, value);
        }

        public int SoftwareCount
        {
            get => _softwareCount;
            set => SetProperty(ref _softwareCount, value);
        }

        public ObservableCollection<SystemWithComponents> Systems { get; }

        private async Task LoadShipDetailsAsync()
        {
            try
            {
                IsLoading = true;

                // Load ship systems
                var systems = await _systemService.GetSystemsByShipIdAsync(Ship.Id);
                var allComponents = await _componentService.GetAllComponentsAsync();
                var allSoftware = await _softwareService.GetAllSoftwareAsync();

                // Group components by system
                var componentsBySystem = allComponents
                    .Where(c => systems.Any(s => s.Id == c.SystemId))
                    .GroupBy(c => c.SystemId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Count software for this ship's components
                var shipComponentIds = allComponents
                    .Where(c => systems.Any(s => s.Id == c.SystemId))
                    .Select(c => c.Id)
                    .ToHashSet();

                var shipSoftwareCount = allSoftware
                    .Count(s => s.InstalledComponentId.HasValue && 
                               shipComponentIds.Contains(s.InstalledComponentId.Value));

                // Update UI on main thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Systems.Clear();

                    foreach (var system in systems.OrderBy(s => s.Name))
                    {
                        var systemWithComponents = new SystemWithComponents
                        {
                            Id = system.Id,
                            Name = system.Name,
                            Manufacturer = system.Manufacturer ?? "Unknown",
                            Model = system.Model ?? "",
                            Description = system.Description ?? "",
                            Components = new ObservableCollection<MaritimeERP.Core.Entities.Component>()
                        };

                        // Add components for this system
                        if (componentsBySystem.TryGetValue(system.Id, out var components))
                        {
                            foreach (var component in components.OrderBy(c => c.Name))
                            {
                                systemWithComponents.Components.Add(component);
                            }
                        }

                        Systems.Add(systemWithComponents);
                    }

                    // Update counts
                    SystemsCount = systems.Count();
                    ComponentsCount = allComponents.Count(c => systems.Any(s => s.Id == c.SystemId));
                    SoftwareCount = shipSoftwareCount;
                });

                _logger.LogInformation("Ship details loaded for {ShipName}: {SystemsCount} systems, {ComponentsCount} components, {SoftwareCount} software",
                    Ship.ShipName, SystemsCount, ComponentsCount, SoftwareCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ship details for {ShipName}", Ship.ShipName);
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error loading ship details: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
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

    /// <summary>
    /// Helper class to represent a system with its components for the visual tree
    /// </summary>
    public class SystemWithComponents
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ObservableCollection<MaritimeERP.Core.Entities.Component> Components { get; set; } = new();
    }
} 