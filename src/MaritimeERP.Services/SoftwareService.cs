using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace MaritimeERP.Services
{
    public class SoftwareService : ISoftwareService
    {
        private readonly MaritimeERPContext _context;
        private readonly ILogger<SoftwareService> _logger;

        public SoftwareService(MaritimeERPContext context, ILogger<SoftwareService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Software>> GetAllSoftwareAsync()
        {
            try
            {
                return await _context.Software
                    .Include(s => s.InstalledComponent)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading software data");
                throw;
            }
        }

        public async Task<Software?> GetSoftwareByIdAsync(int id)
        {
            try
            {
                return await _context.Software
                    .Include(s => s.InstalledComponent)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting software by id {Id}", id);
                throw;
            }
        }

        public async Task<Software> AddSoftwareAsync(Software software)
        {
            try
            {
                software.CreatedAt = DateTime.UtcNow;
                _context.Software.Add(software);
                await _context.SaveChangesAsync();
                
                // Reload the software with related data
                return await _context.Software
                    .Include(s => s.InstalledComponent)
                    .FirstAsync(s => s.Id == software.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding software");
                throw;
            }
        }

        public async Task<Software> UpdateSoftwareAsync(Software software)
        {
            try
            {
                // Get the existing entity from the database to avoid tracking conflicts
                var existingSoftware = await _context.Software.FindAsync(software.Id);
                if (existingSoftware == null)
                {
                    throw new InvalidOperationException($"Software with ID {software.Id} not found");
                }

                // Update the properties of the existing tracked entity
                existingSoftware.Name = software.Name;
                existingSoftware.Version = software.Version;
                existingSoftware.Manufacturer = software.Manufacturer;
                existingSoftware.SoftwareType = software.SoftwareType;
                existingSoftware.InstalledComponentId = software.InstalledComponentId;
                existingSoftware.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                // Reload the software with related data
                return await _context.Software
                    .Include(s => s.InstalledComponent)
                    .FirstAsync(s => s.Id == software.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating software");
                throw;
            }
        }

        public async Task DeleteSoftwareAsync(int id)
        {
            try
            {
                var software = await _context.Software.FindAsync(id);
                if (software != null)
                {
                    _context.Software.Remove(software);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting software");
                throw;
            }
        }

        public async Task<IEnumerable<Software>> GetSoftwareByComponentIdAsync(int componentId)
        {
            try
            {
                return await _context.Software
                    .Include(s => s.InstalledComponent)
                    .Where(s => s.InstalledComponentId == componentId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading software by component ID {ComponentId}", componentId);
                throw;
            }
        }

        public async Task<IEnumerable<Software>> SearchSoftwareAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllSoftwareAsync();
            }

            var searchLower = searchTerm.ToLower();
            try
            {
                return await _context.Software
                    .Include(s => s.InstalledComponent)
                    .Where(s => s.Name.ToLower().Contains(searchLower) ||
                               (s.Manufacturer != null && s.Manufacturer.ToLower().Contains(searchLower)) ||
                               (s.Version != null && s.Version.ToLower().Contains(searchLower)) ||
                               (s.InstalledComponent != null && s.InstalledComponent.Name.ToLower().Contains(searchLower)))
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching software with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetManufacturersAsync()
        {
            try
            {
                var result = await _context.Software
                    .Where(s => !string.IsNullOrEmpty(s.Manufacturer))
                    .Select(s => s.Manufacturer!)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manufacturers list");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetSoftwareTypesAsync()
        {
            try
            {
                var result = await _context.Software
                    .Where(s => !string.IsNullOrEmpty(s.SoftwareType))
                    .Select(s => s.SoftwareType!)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving software types list");
                throw;
            }
        }

        private async Task ValidateSoftwareAsync(Software software)
        {
            if (string.IsNullOrWhiteSpace(software.Name))
            {
                throw new ArgumentException("Software name is required");
            }

            if (string.IsNullOrWhiteSpace(software.Version))
            {
                throw new ArgumentException("Software version is required");
            }

            if (software.InstalledComponentId.HasValue)
            {
                var componentExists = await _context.Components.AnyAsync(c => c.Id == software.InstalledComponentId);
                if (!componentExists)
                {
                    throw new ArgumentException($"Component with ID {software.InstalledComponentId} not found");
                }
            }
        }
    }
} 