using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MaritimeERP.Services
{
    public class ComponentService : IComponentService
    {
        private readonly MaritimeERPContext _context;
        private readonly ILogger<ComponentService> _logger;

        public ComponentService(MaritimeERPContext context, ILogger<ComponentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Component>> GetAllComponentsAsync()
        {
            try
            {
                return await _context.Components
                    .Include(c => c.System)
                        .ThenInclude(s => s.Ship)
                    .Include(c => c.System)
                        .ThenInclude(s => s.Category)
                    .OrderBy(c => c.System.Ship.ShipName)
                    .ThenBy(c => c.System.Name)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all components");
                throw;
            }
        }

        public async Task<Component?> GetComponentByIdAsync(int id)
        {
            try
            {
                return await _context.Components
                    .Include(c => c.System)
                        .ThenInclude(s => s.Ship)
                    .Include(c => c.System)
                        .ThenInclude(s => s.Category)
                    .Include(c => c.Software)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving component with ID {ComponentId}", id);
                throw;
            }
        }

        public async Task<Component> CreateComponentAsync(Component component)
        {
            try
            {
                // Validate the component
                if (!await ValidateComponentAsync(component))
                {
                    throw new InvalidOperationException("Component validation failed");
                }

                // Ensure the system exists
                var system = await _context.Systems.FindAsync(component.SystemId);
                if (system == null)
                {
                    throw new InvalidOperationException($"System with ID {component.SystemId} not found");
                }

                component.CreatedAt = DateTime.UtcNow;
                _context.Components.Add(component);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Component created: {ComponentName} for system {SystemId}", 
                    component.Name, component.SystemId);
                return component;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating component: {ComponentName}", component.Name);
                throw;
            }
        }

        public async Task<Component> UpdateComponentAsync(Component component)
        {
            try
            {
                // Validate the component
                if (!await ValidateComponentAsync(component))
                {
                    throw new InvalidOperationException("Component validation failed");
                }

                var existingComponent = await _context.Components.FindAsync(component.Id);
                if (existingComponent == null)
                {
                    throw new InvalidOperationException($"Component with ID {component.Id} not found");
                }

                // Update properties
                existingComponent.Name = component.Name;
                existingComponent.MakerModel = component.MakerModel;
                existingComponent.UsbPorts = component.UsbPorts;
                existingComponent.LanPorts = component.LanPorts;
                existingComponent.SerialPorts = component.SerialPorts;
                existingComponent.ConnectedCbs = component.ConnectedCbs;
                existingComponent.HasRemoteConnection = component.HasRemoteConnection;
                existingComponent.InstalledLocation = component.InstalledLocation;
                existingComponent.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Component updated: {ComponentId} - {ComponentName}", 
                    component.Id, component.Name);
                return existingComponent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating component: {ComponentId}", component.Id);
                throw;
            }
        }

        public async Task<bool> DeleteComponentAsync(int id)
        {
            try
            {
                var component = await _context.Components
                    .Include(c => c.Software)
                    .FirstOrDefaultAsync(c => c.Id == id);
                
                if (component == null)
                {
                    return false;
                }

                // Check if component has software
                if (component.Software.Any())
                {
                    throw new InvalidOperationException("Cannot delete component that has software installed. Please remove all software first.");
                }

                _context.Components.Remove(component);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Component deleted: {ComponentId} - {ComponentName}", 
                    id, component.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting component: {ComponentId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Component>> GetComponentsBySystemIdAsync(int systemId)
        {
            try
            {
                return await _context.Components
                    .Include(c => c.System)
                        .ThenInclude(s => s.Ship)
                    .Where(c => c.SystemId == systemId)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving components for system {SystemId}", systemId);
                throw;
            }
        }

        public async Task<int> GetComponentCountBySystemIdAsync(int systemId)
        {
            try
            {
                return await _context.Components.CountAsync(c => c.SystemId == systemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting components for system {SystemId}", systemId);
                throw;
            }
        }

        public async Task<IEnumerable<Component>> SearchComponentsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllComponentsAsync();
                }

                var lowerSearchTerm = searchTerm.ToLower();
                return await _context.Components
                    .Include(c => c.System)
                        .ThenInclude(s => s.Ship)
                    .Include(c => c.System)
                        .ThenInclude(s => s.Category)
                    .Where(c => c.Name.ToLower().Contains(lowerSearchTerm) ||
                               c.MakerModel.ToLower().Contains(lowerSearchTerm) ||
                               c.InstalledLocation.ToLower().Contains(lowerSearchTerm) ||
                               (c.ConnectedCbs != null && c.ConnectedCbs.ToLower().Contains(lowerSearchTerm)) ||
                               c.System.Name.ToLower().Contains(lowerSearchTerm) ||
                               c.System.Ship.ShipName.ToLower().Contains(lowerSearchTerm))
                    .OrderBy(c => c.System.Ship.ShipName)
                    .ThenBy(c => c.System.Name)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching components with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<Component>> GetComponentsByLocationAsync(string location)
        {
            try
            {
                return await _context.Components
                    .Include(c => c.System)
                        .ThenInclude(s => s.Ship)
                    .Include(c => c.System)
                        .ThenInclude(s => s.Category)
                    .Where(c => c.InstalledLocation.ToLower() == location.ToLower())
                    .OrderBy(c => c.System.Ship.ShipName)
                    .ThenBy(c => c.System.Name)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving components for location: {Location}", location);
                throw;
            }
        }

        public async Task<IEnumerable<Component>> GetComponentsByMakerModelAsync(string makerModel)
        {
            try
            {
                return await _context.Components
                    .Include(c => c.System)
                        .ThenInclude(s => s.Ship)
                    .Include(c => c.System)
                        .ThenInclude(s => s.Category)
                    .Where(c => c.MakerModel.ToLower() == makerModel.ToLower())
                    .OrderBy(c => c.System.Ship.ShipName)
                    .ThenBy(c => c.System.Name)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving components for maker/model: {MakerModel}", makerModel);
                throw;
            }
        }

        public async Task<IEnumerable<Component>> GetComponentsWithRemoteConnectionAsync()
        {
            try
            {
                return await _context.Components
                    .Include(c => c.System)
                        .ThenInclude(s => s.Ship)
                    .Include(c => c.System)
                        .ThenInclude(s => s.Category)
                    .Where(c => c.HasRemoteConnection)
                    .OrderBy(c => c.System.Ship.ShipName)
                    .ThenBy(c => c.System.Name)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving components with remote connection");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetMakerModelsAsync()
        {
            try
            {
                return await _context.Components
                    .Select(c => c.MakerModel)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maker models");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetInstalledLocationsAsync()
        {
            try
            {
                return await _context.Components
                    .Select(c => c.InstalledLocation)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving installed locations");
                throw;
            }
        }

        public async Task<int> GetTotalComponentsCountAsync()
        {
            try
            {
                return await _context.Components.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total components count");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetComponentsCountBySystemAsync()
        {
            try
            {
                return await _context.Components
                    .Include(c => c.System)
                        .ThenInclude(s => s.Ship)
                    .GroupBy(c => $"{c.System.Ship.ShipName} - {c.System.Name}")
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting components count by system");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetComponentsCountByLocationAsync()
        {
            try
            {
                return await _context.Components
                    .GroupBy(c => c.InstalledLocation)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting components count by location");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetComponentsCountByMakerModelAsync()
        {
            try
            {
                return await _context.Components
                    .GroupBy(c => c.MakerModel)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting components count by maker/model");
                throw;
            }
        }

        public async Task<bool> ValidateComponentAsync(Component component)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(component.Name))
                {
                    _logger.LogWarning("Component validation failed: Name is required");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(component.MakerModel))
                {
                    _logger.LogWarning("Component validation failed: MakerModel is required");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(component.InstalledLocation))
                {
                    _logger.LogWarning("Component validation failed: InstalledLocation is required");
                    return false;
                }

                if (component.SystemId <= 0)
                {
                    _logger.LogWarning("Component validation failed: Valid SystemId is required");
                    return false;
                }

                // Port validation
                if (component.UsbPorts < 0 || component.LanPorts < 0 || component.SerialPorts < 0)
                {
                    _logger.LogWarning("Component validation failed: Port counts cannot be negative");
                    return false;
                }

                // Check if system exists
                var systemExists = await _context.Systems.AnyAsync(s => s.Id == component.SystemId);
                if (!systemExists)
                {
                    _logger.LogWarning("Component validation failed: System with ID {SystemId} does not exist", component.SystemId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating component");
                return false;
            }
        }
    }
} 