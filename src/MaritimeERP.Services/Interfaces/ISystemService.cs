using MaritimeERP.Core.Entities;

namespace MaritimeERP.Services.Interfaces
{
    public interface ISystemService
    {
        // CRUD Operations
        Task<IEnumerable<ShipSystem>> GetAllSystemsAsync();
        Task<ShipSystem?> GetSystemByIdAsync(int id);
        Task<ShipSystem> CreateSystemAsync(ShipSystem system);
        Task<ShipSystem> UpdateSystemAsync(ShipSystem system);
        Task<bool> DeleteSystemAsync(int id);

        // Ship-specific operations
        Task<IEnumerable<ShipSystem>> GetSystemsByShipIdAsync(int shipId);
        Task<int> GetSystemCountByShipIdAsync(int shipId);

        // Search and filtering
        Task<IEnumerable<ShipSystem>> SearchSystemsAsync(string searchTerm);
        Task<IEnumerable<ShipSystem>> GetSystemsByCategoryAsync(int categoryId);
        Task<IEnumerable<ShipSystem>> GetSystemsByManufacturerAsync(string manufacturer);

        // Lookup data
        Task<IEnumerable<SystemCategory>> GetSystemCategoriesAsync();
        Task<IEnumerable<string>> GetManufacturersAsync();

        // Validation
        Task<bool> IsSerialNumberUniqueAsync(string serialNumber, int? excludeSystemId = null);

        // Statistics
        Task<int> GetTotalSystemsCountAsync();
        Task<Dictionary<string, int>> GetSystemsCountByCategoryAsync();
        Task<Dictionary<string, int>> GetSystemsCountByManufacturerAsync();
    }
} 