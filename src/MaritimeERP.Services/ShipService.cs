using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Services
{
    public class ShipService : IShipService
    {
        private readonly MaritimeERPContext _context;
        private readonly ILogger<ShipService> _logger;
        private readonly IAuditLogService _auditLogService;

        public ShipService(MaritimeERPContext context, ILogger<ShipService> logger, IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        public async Task<IEnumerable<Ship>> GetAllShipsAsync()
        {
            try
            {
                return await _context.Ships
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.ShipName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all ships");
                throw;
            }
        }

        public async Task<Ship?> GetShipByIdAsync(int id)
        {
            try
            {
                return await _context.Ships
                    .Include(s => s.Systems)
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ship with ID {ShipId}", id);
                throw;
            }
        }

        public async Task<Ship?> GetShipByImoAsync(string imoNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imoNumber))
                    return null;

                return await _context.Ships
                    .FirstOrDefaultAsync(s => s.ImoNumber == imoNumber && !s.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ship with IMO {ImoNumber}", imoNumber);
                throw;
            }
        }

        public async Task<IEnumerable<Ship>> SearchShipsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllShipsAsync();

                var term = searchTerm.ToLower();
                return await _context.Ships
                    .Where(s => !s.IsDeleted && 
                               (s.ShipName.ToLower().Contains(term) ||
                                s.ImoNumber.Contains(term) ||
                                s.Flag.ToLower().Contains(term) ||
                                s.PortOfRegistry.ToLower().Contains(term) ||
                                s.Class.ToLower().Contains(term) ||
                                s.ClassNotation.ToLower().Contains(term) ||
                                s.Owner.ToLower().Contains(term)))
                    .OrderBy(s => s.ShipName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching ships with term '{SearchTerm}'", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<Ship>> GetShipsByTypeAsync(string shipType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(shipType))
                    return await GetAllShipsAsync();

                return await _context.Ships
                    .Where(s => !s.IsDeleted && s.ShipType.ToLower() == shipType.ToLower())
                    .OrderBy(s => s.ShipName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ships by type '{ShipType}'", shipType);
                throw;
            }
        }

        public async Task<IEnumerable<Ship>> GetShipsByFlagAsync(string flag)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(flag))
                    return await GetAllShipsAsync();

                return await _context.Ships
                    .Where(s => !s.IsDeleted && s.Flag.ToLower() == flag.ToLower())
                    .OrderBy(s => s.ShipName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ships by flag '{Flag}'", flag);
                throw;
            }
        }

        public async Task<Ship> CreateShipAsync(Ship ship)
        {
            try
            {
                if (ship == null)
                    throw new ArgumentNullException(nameof(ship));

                // Validate IMO number uniqueness
                if (!string.IsNullOrWhiteSpace(ship.ImoNumber))
                {
                    var existingShip = await _context.Ships
                        .FirstOrDefaultAsync(s => s.ImoNumber == ship.ImoNumber && !s.IsDeleted);
                    
                    if (existingShip != null)
                        throw new InvalidOperationException($"A ship with IMO number {ship.ImoNumber} already exists.");
                }

                ship.CreatedAt = DateTime.UtcNow;
                ship.UpdatedAt = DateTime.UtcNow;
                ship.IsDeleted = false;

                _context.Ships.Add(ship);
                await _context.SaveChangesAsync();

                // Log the create operation
                await _auditLogService.LogCreateAsync(ship, "Ship created");

                _logger.LogInformation("Ship '{ShipName}' with IMO {ImoNumber} created successfully", 
                    ship.ShipName, ship.ImoNumber);

                return ship;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ship '{ShipName}'", ship?.ShipName);
                throw;
            }
        }

        public async Task<Ship> UpdateShipAsync(Ship ship)
        {
            try
            {
                if (ship == null)
                    throw new ArgumentNullException(nameof(ship));

                var existingShip = await _context.Ships
                    .FirstOrDefaultAsync(s => s.Id == ship.Id && !s.IsDeleted);

                if (existingShip == null)
                    throw new InvalidOperationException($"Ship with ID {ship.Id} not found.");

                // Validate IMO number uniqueness (excluding current ship)
                if (!string.IsNullOrWhiteSpace(ship.ImoNumber) && ship.ImoNumber != existingShip.ImoNumber)
                {
                    var duplicateShip = await _context.Ships
                        .FirstOrDefaultAsync(s => s.ImoNumber == ship.ImoNumber && s.Id != ship.Id && !s.IsDeleted);
                    
                    if (duplicateShip != null)
                        throw new InvalidOperationException($"A ship with IMO number {ship.ImoNumber} already exists.");
                }

                // Store the old values for audit logging
                var oldShip = new Ship
                {
                    Id = existingShip.Id,
                    ShipName = existingShip.ShipName,
                    ImoNumber = existingShip.ImoNumber,
                    ShipType = existingShip.ShipType,
                    Flag = existingShip.Flag,
                    PortOfRegistry = existingShip.PortOfRegistry,
                    Class = existingShip.Class,
                    ClassNotation = existingShip.ClassNotation,
                    BuildYear = existingShip.BuildYear,
                    GrossTonnage = existingShip.GrossTonnage,
                    NetTonnage = existingShip.NetTonnage,
                    DeadweightTonnage = existingShip.DeadweightTonnage,
                    Owner = existingShip.Owner,
                    IsActive = existingShip.IsActive,
                    CreatedAt = existingShip.CreatedAt,
                    UpdatedAt = existingShip.UpdatedAt,
                    IsDeleted = existingShip.IsDeleted
                };

                // Update properties - only include fields that exist in the simplified Ship entity
                existingShip.ShipName = ship.ShipName;
                existingShip.ImoNumber = ship.ImoNumber;
                existingShip.ShipType = ship.ShipType;
                existingShip.Flag = ship.Flag;
                existingShip.PortOfRegistry = ship.PortOfRegistry;
                existingShip.Class = ship.Class;
                existingShip.ClassNotation = ship.ClassNotation;
                existingShip.BuildYear = ship.BuildYear;
                existingShip.GrossTonnage = ship.GrossTonnage;
                existingShip.NetTonnage = ship.NetTonnage;
                existingShip.DeadweightTonnage = ship.DeadweightTonnage;
                existingShip.Owner = ship.Owner;
                existingShip.IsActive = ship.IsActive;
                existingShip.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log the update operation
                await _auditLogService.LogUpdateAsync(oldShip, existingShip, "Ship updated");

                _logger.LogInformation("Ship '{ShipName}' with ID {ShipId} updated successfully", 
                    existingShip.ShipName, existingShip.Id);

                return existingShip;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ship with ID {ShipId}", ship?.Id);
                throw;
            }
        }

        public async Task<bool> DeleteShipAsync(int id)
        {
            try
            {
                var ship = await _context.Ships
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (ship == null)
                    return false;

                // Soft delete
                ship.IsDeleted = true;
                ship.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log the delete operation
                await _auditLogService.LogDeleteAsync(ship, "Ship deleted");

                _logger.LogInformation("Ship '{ShipName}' with ID {ShipId} deleted successfully", 
                    ship.ShipName, ship.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ship with ID {ShipId}", id);
                throw;
            }
        }

        public async Task<bool> ActivateShipAsync(int id)
        {
            try
            {
                var ship = await _context.Ships
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (ship == null)
                    return false;

                ship.IsActive = true;
                ship.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Ship '{ShipName}' with ID {ShipId} activated successfully", 
                    ship.ShipName, ship.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating ship with ID {ShipId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivateShipAsync(int id)
        {
            try
            {
                var ship = await _context.Ships
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (ship == null)
                    return false;

                ship.IsActive = false;
                ship.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Ship '{ShipName}' with ID {ShipId} deactivated successfully", 
                    ship.ShipName, ship.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating ship with ID {ShipId}", id);
                throw;
            }
        }

        public async Task<int> GetShipCountAsync()
        {
            try
            {
                return await _context.Ships.CountAsync(s => !s.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ship count");
                throw;
            }
        }

        public async Task<int> GetActiveShipCountAsync()
        {
            try
            {
                return await _context.Ships.CountAsync(s => !s.IsDeleted && s.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active ship count");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetShipTypesAsync()
        {
            try
            {
                return await _context.Ships
                    .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.ShipType))
                    .Select(s => s.ShipType)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ship types");
                throw;
            }
        }

        public async Task<IEnumerable<ShipType>> GetShipTypeEntitiesAsync()
        {
            try
            {
                var shipTypes = await _context.Ships
                    .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.ShipType))
                    .Select(s => s.ShipType)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();

                return shipTypes.Select((type, index) => new ShipType
                {
                    Id = index + 1,
                    Name = type
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ship type entities");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetShipFlagsAsync()
        {
            try
            {
                return await _context.Ships
                    .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.Flag))
                    .Select(s => s.Flag)
                    .Distinct()
                    .OrderBy(f => f)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ship flags");
                throw;
            }
        }

        public async Task<bool> ImoExistsAsync(string imoNumber, int? excludeShipId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imoNumber))
                    return false;

                var query = _context.Ships
                    .Where(s => !s.IsDeleted && s.ImoNumber == imoNumber);

                if (excludeShipId.HasValue)
                {
                    query = query.Where(s => s.Id != excludeShipId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if IMO {ImoNumber} exists", imoNumber);
                throw;
            }
        }

        public async Task<ShipStatistics> GetShipStatisticsAsync()
        {
            try
            {
                var ships = await _context.Ships
                    .Where(s => !s.IsDeleted)
                    .ToListAsync();

                var currentYear = DateTime.UtcNow.Year;

                var statistics = new ShipStatistics
                {
                    TotalShips = ships.Count,
                    TotalGrossTonnage = ships.Sum(s => s.GrossTonnage ?? 0),
                    ShipsThisYear = ships.Count(s => s.CreatedAt.Year == currentYear),
                    ShipsByFlag = ships
                        .Where(s => !string.IsNullOrEmpty(s.Flag))
                        .GroupBy(s => s.Flag)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    ShipsByYear = ships
                        .Where(s => s.BuildYear.HasValue)
                        .GroupBy(s => s.BuildYear!.Value)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ship statistics");
                throw;
            }
        }
    }
} 