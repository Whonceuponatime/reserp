using MaritimeERP.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaritimeERP.Services.Interfaces
{
    public interface ISystemChangePlanService
    {
        Task<List<SystemChangePlan>> GetAllSystemChangePlansAsync();
        Task<SystemChangePlan?> GetSystemChangePlanByIdAsync(int id);
        Task<SystemChangePlan> CreateSystemChangePlanAsync(SystemChangePlan systemChangePlan);
        Task<SystemChangePlan> UpdateSystemChangePlanAsync(SystemChangePlan systemChangePlan);
        Task<bool> DeleteSystemChangePlanAsync(int id);
        Task<string> GenerateRequestNumberAsync();
    }
} 