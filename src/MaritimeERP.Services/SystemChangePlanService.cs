using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MaritimeERP.Services
{
    public class SystemChangePlanService : ISystemChangePlanService
    {
        private readonly MaritimeERPContext _context;

        public SystemChangePlanService(MaritimeERPContext context)
        {
            _context = context;
        }

        public async Task<List<SystemChangePlan>> GetAllSystemChangePlansAsync()
        {
            return await _context.SystemChangePlans
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public async Task<SystemChangePlan?> GetSystemChangePlanByIdAsync(int id)
        {
            return await _context.SystemChangePlans
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<SystemChangePlan> CreateSystemChangePlanAsync(SystemChangePlan systemChangePlan)
        {
            if (string.IsNullOrEmpty(systemChangePlan.RequestNumber))
            {
                systemChangePlan.RequestNumber = await GenerateRequestNumberAsync();
            }

            systemChangePlan.CreatedDate = DateTime.Now;
            systemChangePlan.UpdatedDate = DateTime.Now;

            _context.SystemChangePlans.Add(systemChangePlan);
            await _context.SaveChangesAsync();
            return systemChangePlan;
        }

        public async Task<SystemChangePlan> UpdateSystemChangePlanAsync(SystemChangePlan systemChangePlan)
        {
            systemChangePlan.UpdatedDate = DateTime.Now;
            _context.SystemChangePlans.Update(systemChangePlan);
            await _context.SaveChangesAsync();
            return systemChangePlan;
        }

        public async Task<bool> DeleteSystemChangePlanAsync(int id)
        {
            var systemChangePlan = await _context.SystemChangePlans.FindAsync(id);
            if (systemChangePlan == null)
                return false;

            _context.SystemChangePlans.Remove(systemChangePlan);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateRequestNumberAsync()
        {
            var today = DateTime.Now;
            var prefix = $"SYS-{today:yyyyMM}";
            
            var lastRequest = await _context.SystemChangePlans
                .Where(s => s.RequestNumber.StartsWith(prefix))
                .OrderByDescending(s => s.RequestNumber)
                .FirstOrDefaultAsync();

            if (lastRequest == null)
            {
                return $"{prefix}-001";
            }

            var lastNumberPart = lastRequest.RequestNumber.Split('-').Last();
            if (int.TryParse(lastNumberPart, out int lastNumber))
            {
                return $"{prefix}-{(lastNumber + 1):D3}";
            }

            return $"{prefix}-001";
        }
    }
} 