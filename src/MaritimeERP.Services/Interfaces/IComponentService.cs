using MaritimeERP.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaritimeERP.Services.Interfaces
{
    public interface IComponentService
    {
        // CRUD Operations
        Task<IEnumerable<Component>> GetAllComponentsAsync();
        Task<Component> GetComponentByIdAsync(int id);
        Task<Component> AddComponentAsync(Component component);
        Task<Component> UpdateComponentAsync(Component component);
        Task DeleteComponentAsync(int id);

        // System-specific operations
        Task<IEnumerable<Component>> GetComponentsBySystemIdAsync(int systemId);
        Task<int> GetComponentCountBySystemIdAsync(int systemId);

        // Search and filtering
        Task<IEnumerable<Component>> SearchComponentsAsync(string searchTerm);
        Task<IEnumerable<Component>> GetComponentsByLocationAsync(string location);
        Task<IEnumerable<Component>> GetComponentsByMakerModelAsync(string makerModel);
        Task<IEnumerable<Component>> GetComponentsWithRemoteConnectionAsync();

        // Lookup data
        Task<IEnumerable<string>> GetMakerModelsAsync();
        Task<IEnumerable<string>> GetInstalledLocationsAsync();

        // Statistics
        Task<int> GetTotalComponentsCountAsync();
        Task<Dictionary<string, int>> GetComponentsCountBySystemAsync();
        Task<Dictionary<string, int>> GetComponentsCountByLocationAsync();
        Task<Dictionary<string, int>> GetComponentsCountByMakerModelAsync();

        // Validation
        Task<bool> ValidateComponentAsync(Component component);
    }
} 