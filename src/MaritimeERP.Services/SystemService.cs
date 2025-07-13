using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MaritimeERP.Services
{
    public class SystemService : ISystemService
    {
        private readonly MaritimeERPContext _context;
        private readonly ILogger<SystemService> _logger;
        private readonly IAuditLogService _auditLogService;

        public SystemService(MaritimeERPContext context, ILogger<SystemService> logger, IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        public async Task<IEnumerable<ShipSystem>> GetAllSystemsAsync()
        {
            try
            {
                return await _context.Systems
                    .Include(s => s.Ship)
                    .Include(s => s.Category)
                    .OrderBy(s => s.Ship.ShipName)
                    .ThenBy(s => s.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all systems");
                throw;
            }
        }

        public async Task<ShipSystem?> GetSystemByIdAsync(int id)
        {
            try
            {
                return await _context.Systems
                    .Include(s => s.Ship)
                    .Include(s => s.Category)
                    .Include(s => s.Components)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system with ID {SystemId}", id);
                throw;
            }
        }

        public async Task<ShipSystem> CreateSystemAsync(ShipSystem system)
        {
            try
            {
                // Validate serial number uniqueness
                if (!await IsSerialNumberUniqueAsync(system.SerialNumber))
                {
                    throw new InvalidOperationException($"Serial number '{system.SerialNumber}' already exists");
                }

                system.CreatedAt = DateTime.UtcNow;
                _context.Systems.Add(system);
                await _context.SaveChangesAsync();

                // Log the create operation
                await _auditLogService.LogCreateAsync(system, "System created");

                _logger.LogInformation("System created: {SystemName} for ship {ShipId}", system.Name, system.ShipId);
                return system;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system: {SystemName}", system.Name);
                throw;
            }
        }

        public async Task<ShipSystem> UpdateSystemAsync(ShipSystem system)
        {
            try
            {
                // Validate serial number uniqueness (excluding current system)
                if (!await IsSerialNumberUniqueAsync(system.SerialNumber, system.Id))
                {
                    throw new InvalidOperationException($"Serial number '{system.SerialNumber}' already exists");
                }

                // Get the existing entity from the database to avoid tracking conflicts
                var existingSystem = await _context.Systems.FindAsync(system.Id);
                if (existingSystem == null)
                {
                    throw new InvalidOperationException($"System with ID {system.Id} not found");
                }

                // Store the old values for audit logging
                var oldSystem = new ShipSystem
                {
                    Id = existingSystem.Id,
                    Name = existingSystem.Name,
                    ShipId = existingSystem.ShipId,
                    CategoryId = existingSystem.CategoryId,
                    SecurityZone = existingSystem.SecurityZone,
                    Manufacturer = existingSystem.Manufacturer,
                    Model = existingSystem.Model,
                    SerialNumber = existingSystem.SerialNumber,
                    Description = existingSystem.Description,
                    HasRemoteConnection = existingSystem.HasRemoteConnection,
                    CreatedAt = existingSystem.CreatedAt,
                    UpdatedAt = existingSystem.UpdatedAt
                };

                // Update the properties of the existing tracked entity
                existingSystem.Name = system.Name;
                existingSystem.ShipId = system.ShipId;
                existingSystem.CategoryId = system.CategoryId;
                existingSystem.SecurityZone = system.SecurityZone;
                existingSystem.Manufacturer = system.Manufacturer;
                existingSystem.Model = system.Model;
                existingSystem.SerialNumber = system.SerialNumber;
                existingSystem.Description = system.Description;
                existingSystem.HasRemoteConnection = system.HasRemoteConnection;
                existingSystem.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log the update operation
                await _auditLogService.LogUpdateAsync(oldSystem, existingSystem, "System updated");

                _logger.LogInformation("System updated: {SystemId} - {SystemName}", system.Id, system.Name);
                
                // Return the updated entity with all related data
                return await GetSystemByIdAsync(system.Id) ?? existingSystem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system: {SystemId}", system.Id);
                throw;
            }
        }

        public async Task<bool> DeleteSystemAsync(int id)
        {
            try
            {
                var system = await _context.Systems.FindAsync(id);
                if (system == null)
                {
                    return false;
                }

                // Check if system has components
                var hasComponents = await _context.Components.AnyAsync(c => c.SystemId == id);
                if (hasComponents)
                {
                    throw new InvalidOperationException("Cannot delete system that has components. Please remove all components first.");
                }

                _context.Systems.Remove(system);
                await _context.SaveChangesAsync();

                // Log the delete operation
                await _auditLogService.LogDeleteAsync(system, "System deleted");

                _logger.LogInformation("System deleted: {SystemId} - {SystemName}", id, system.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting system: {SystemId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ShipSystem>> GetSystemsByShipIdAsync(int shipId)
        {
            try
            {
                return await _context.Systems
                    .Include(s => s.Ship)
                    .Include(s => s.Category)
                    .Where(s => s.ShipId == shipId)
                    .OrderBy(s => s.Category.Name)
                    .ThenBy(s => s.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving systems for ship {ShipId}", shipId);
                throw;
            }
        }

        public async Task<int> GetSystemCountByShipIdAsync(int shipId)
        {
            try
            {
                return await _context.Systems.CountAsync(s => s.ShipId == shipId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting systems for ship {ShipId}", shipId);
                throw;
            }
        }

        public async Task<IEnumerable<ShipSystem>> SearchSystemsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllSystemsAsync();
                }

                var lowerSearchTerm = searchTerm.ToLower();
                return await _context.Systems
                    .Include(s => s.Ship)
                    .Include(s => s.Category)
                    .Where(s => s.Name.ToLower().Contains(lowerSearchTerm) ||
                               s.Manufacturer.ToLower().Contains(lowerSearchTerm) ||
                               s.Model.ToLower().Contains(lowerSearchTerm) ||
                               s.SerialNumber.ToLower().Contains(lowerSearchTerm) ||
                               (s.Description != null && s.Description.ToLower().Contains(lowerSearchTerm)) ||
                               s.Ship.ShipName.ToLower().Contains(lowerSearchTerm))
                    .OrderBy(s => s.Ship.ShipName)
                    .ThenBy(s => s.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching systems with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<ShipSystem>> GetSystemsByCategoryAsync(int categoryId)
        {
            try
            {
                return await _context.Systems
                    .Include(s => s.Ship)
                    .Include(s => s.Category)
                    .Where(s => s.CategoryId == categoryId)
                    .OrderBy(s => s.Ship.ShipName)
                    .ThenBy(s => s.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving systems for category {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<IEnumerable<ShipSystem>> GetSystemsByManufacturerAsync(string manufacturer)
        {
            try
            {
                return await _context.Systems
                    .Include(s => s.Ship)
                    .Include(s => s.Category)
                    .Where(s => s.Manufacturer.ToLower() == manufacturer.ToLower())
                    .OrderBy(s => s.Ship.ShipName)
                    .ThenBy(s => s.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving systems for manufacturer: {Manufacturer}", manufacturer);
                throw;
            }
        }

        public async Task<IEnumerable<SystemCategory>> GetSystemCategoriesAsync()
        {
            try
            {
                return await _context.SystemCategories
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system categories");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetManufacturersAsync()
        {
            try
            {
                return await _context.Systems
                    .Select(s => s.Manufacturer)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manufacturers");
                throw;
            }
        }



        public async Task<bool> IsSerialNumberUniqueAsync(string serialNumber, int? excludeSystemId = null)
        {
            try
            {
                var query = _context.Systems.Where(s => s.SerialNumber.ToLower() == serialNumber.ToLower());
                
                if (excludeSystemId.HasValue)
                {
                    query = query.Where(s => s.Id != excludeSystemId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking serial number uniqueness: {SerialNumber}", serialNumber);
                throw;
            }
        }

        public async Task<int> GetTotalSystemsCountAsync()
        {
            try
            {
                return await _context.Systems.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total systems count");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetSystemsCountByCategoryAsync()
        {
            try
            {
                return await _context.Systems
                    .Include(s => s.Category)
                    .GroupBy(s => s.Category.Name)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting systems count by category");
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetSystemsCountByManufacturerAsync()
        {
            try
            {
                return await _context.Systems
                    .GroupBy(s => s.Manufacturer)
                    .Select(g => new { Manufacturer = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Manufacturer, x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting systems count by manufacturer");
                throw;
            }
        }
    }
} 