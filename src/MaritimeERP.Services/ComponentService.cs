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
        private readonly IAuditLogService _auditLogService;

        public ComponentService(MaritimeERPContext context, ILogger<ComponentService> logger, IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
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

        public async Task<bool> ValidateComponentAsync(Component component)
        {
            try
            {
                if (component == null)
                {
                    throw new ArgumentNullException(nameof(component));
                }

                if (string.IsNullOrWhiteSpace(component.Name))
                {
                    throw new ArgumentException("Component name is required");
                }

                if (string.IsNullOrWhiteSpace(component.ComponentType))
                {
                    throw new ArgumentException("Component type is required");
                }

                if (string.IsNullOrWhiteSpace(component.SystemName))
                {
                    throw new ArgumentException("System name is required");
                }

                if (string.IsNullOrWhiteSpace(component.InstalledLocation))
                {
                    throw new ArgumentException("Installed location is required");
                }

                // Check if system exists
                if (component.SystemId > 0)
                {
                    var system = await _context.Systems.FindAsync(component.SystemId);
                    if (system == null)
                    {
                        throw new ArgumentException($"System with ID {component.SystemId} not found");
                    }
                }

                // Check for duplicate component names within the same system
                var existingComponent = await _context.Components
                    .FirstOrDefaultAsync(c => 
                        c.Name == component.Name && 
                        c.SystemId == component.SystemId && 
                        c.Id != component.Id);

                if (existingComponent != null)
                {
                    throw new ArgumentException($"A component with name '{component.Name}' already exists in this system");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Component validation failed");
                return false;
            }
        }

        public async Task<Component> AddComponentAsync(Component component)
        {
            if (!await ValidateComponentAsync(component))
            {
                throw new ArgumentException("Component validation failed");
            }

            try
            {
                component.CreatedAt = DateTime.UtcNow;
                component.UpdatedAt = DateTime.UtcNow;
                _context.Components.Add(component);
                await _context.SaveChangesAsync();
                
                // Log the create operation
                await _auditLogService.LogCreateAsync(component, "Component created");
                
                return component;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding component");
                throw;
            }
        }

        public async Task<Component> UpdateComponentAsync(Component component)
        {
            if (!await ValidateComponentAsync(component))
            {
                throw new ArgumentException("Component validation failed");
            }

            try
            {
                var existingComponent = await _context.Components.FindAsync(component.Id);
                if (existingComponent == null)
                {
                    throw new KeyNotFoundException($"Component with ID {component.Id} not found");
                }

                // Store the old values for audit logging
                var oldComponent = new Component
                {
                    Id = existingComponent.Id,
                    SystemId = existingComponent.SystemId,
                    SystemName = existingComponent.SystemName,
                    ComponentType = existingComponent.ComponentType,
                    Name = existingComponent.Name,
                    Manufacturer = existingComponent.Manufacturer,
                    Model = existingComponent.Model,
                    InstalledLocation = existingComponent.InstalledLocation,
                    OsName = existingComponent.OsName,
                    OsVersion = existingComponent.OsVersion,
                    UsbPorts = existingComponent.UsbPorts,
                    LanPorts = existingComponent.LanPorts,
                    ConnectedCbs = existingComponent.ConnectedCbs,
                    NetworkSegment = existingComponent.NetworkSegment,
                    SupportedProtocols = existingComponent.SupportedProtocols,
                    ConnectionPurpose = existingComponent.ConnectionPurpose,
                    HasRemoteConnection = existingComponent.HasRemoteConnection,
                    IsTypeApproved = existingComponent.IsTypeApproved,
                    CreatedAt = existingComponent.CreatedAt,
                    UpdatedAt = existingComponent.UpdatedAt
                };

                component.UpdatedAt = DateTime.UtcNow;
                // Preserve the original CreatedAt value
                component.CreatedAt = existingComponent.CreatedAt;
                
                _context.Entry(existingComponent).CurrentValues.SetValues(component);
                await _context.SaveChangesAsync();
                
                // Log the update operation
                await _auditLogService.LogUpdateAsync(oldComponent, existingComponent, "Component updated");
                
                return existingComponent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating component");
                throw;
            }
        }

        public async Task DeleteComponentAsync(int id)
        {
            try
            {
                var component = await _context.Components.FindAsync(id);
                if (component != null)
                {
                    _context.Components.Remove(component);
                    await _context.SaveChangesAsync();
                    
                    // Log the delete operation
                    await _auditLogService.LogDeleteAsync(component, "Component deleted");
                }
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
                               (c.Manufacturer != null && c.Manufacturer.ToLower().Contains(lowerSearchTerm)) ||
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
                    .Where(c => ((c.Manufacturer ?? "") + " " + (c.Model ?? "")).Contains(makerModel))
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving components by maker/model: {MakerModel}", makerModel);
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
                    .Where(c => c.System.HasRemoteConnection)
                    .OrderBy(c => c.Name)
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
                    .Select(c => ((c.Manufacturer ?? "") + " " + (c.Model ?? "")).Trim())
                    .Distinct()
                    .OrderBy(m => m)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maker/model list");
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
                    .GroupBy(c => ((c.Manufacturer ?? "") + " " + (c.Model ?? "")).Trim())
                    .Select(g => new { MakerModel = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToDictionaryAsync(x => x.MakerModel, x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting components count by maker/model");
                throw;
            }
        }
    }
} 