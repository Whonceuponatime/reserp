using System.Collections.Generic;
using System.Threading.Tasks;
using MaritimeERP.Core.Entities;

namespace MaritimeERP.Services.Interfaces
{
    public interface ISoftwareService
    {
        Task<IEnumerable<Software>> GetAllSoftwareAsync();
        Task<Software?> GetSoftwareByIdAsync(int id);
        Task<Software> AddSoftwareAsync(Software software);
        Task<Software> UpdateSoftwareAsync(Software software);
        Task DeleteSoftwareAsync(int id);
        Task<IEnumerable<Software>> GetSoftwareByComponentIdAsync(int componentId);
        Task<IEnumerable<Software>> SearchSoftwareAsync(string searchTerm);
        Task<IEnumerable<string>> GetManufacturersAsync();
        Task<IEnumerable<string>> GetSoftwareTypesAsync();
    }
} 