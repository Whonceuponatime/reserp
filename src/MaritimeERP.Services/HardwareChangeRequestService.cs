using Microsoft.EntityFrameworkCore;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Services
{
    public class HardwareChangeRequestService : IHardwareChangeRequestService
    {
        private readonly MaritimeERPContext _context;

        public HardwareChangeRequestService(MaritimeERPContext context)
        {
            _context = context;
        }

        public async Task<List<HardwareChangeRequest>> GetAllAsync()
        {
            return await _context.HardwareChangeRequests
                .Include(h => h.RequesterUser)
                .Include(h => h.PreparedByUser)
                .Include(h => h.ReviewedByUser)
                .Include(h => h.ApprovedByUser)
                .OrderByDescending(h => h.CreatedDate)
                .ToListAsync();
        }

        public async Task<HardwareChangeRequest?> GetByIdAsync(int id)
        {
            return await _context.HardwareChangeRequests
                .Include(h => h.RequesterUser)
                .Include(h => h.PreparedByUser)
                .Include(h => h.ReviewedByUser)
                .Include(h => h.ApprovedByUser)
                .FirstOrDefaultAsync(h => h.Id == id);
        }

        public async Task<HardwareChangeRequest> CreateAsync(HardwareChangeRequest request)
        {
            request.CreatedDate = DateTime.Now;
            request.Status = "Draft";
            
            if (string.IsNullOrEmpty(request.RequestNumber))
            {
                request.RequestNumber = await GenerateRequestNumberAsync();
            }

            _context.HardwareChangeRequests.Add(request);
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<HardwareChangeRequest> UpdateAsync(HardwareChangeRequest request)
        {
            _context.Entry(request).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var request = await _context.HardwareChangeRequests.FindAsync(id);
            if (request == null)
                return false;

            _context.HardwareChangeRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateRequestNumberAsync()
        {
            var today = DateTime.Now;
            var prefix = $"HW-{today:yyyyMM}";
            
            var lastRequest = await _context.HardwareChangeRequests
                .Where(r => r.RequestNumber.StartsWith(prefix))
                .OrderByDescending(r => r.RequestNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastRequest != null)
            {
                var lastNumberPart = lastRequest.RequestNumber.Split('-').Last();
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}-{nextNumber:D3}";
        }

        public async Task<bool> SubmitForReviewAsync(int id, int userId)
        {
            var request = await _context.HardwareChangeRequests.FindAsync(id);
            if (request == null || request.Status != "Draft")
                return false;

            request.Status = "Submitted";
            request.PreparedByUserId = userId;
            request.PreparedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReviewAsync(int id, int reviewerId, string comment)
        {
            var request = await _context.HardwareChangeRequests.FindAsync(id);
            if (request == null || request.Status != "Submitted")
                return false;

            request.Status = "Under Review";
            request.ReviewedByUserId = reviewerId;
            request.ReviewedAt = DateTime.Now;
            request.SecurityReviewComment = comment;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveAsync(int id, int approverId)
        {
            var request = await _context.HardwareChangeRequests.FindAsync(id);
            if (request == null || request.Status != "Under Review")
                return false;

            request.Status = "Approved";
            request.ApprovedByUserId = approverId;
            request.ApprovedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectAsync(int id, int reviewerId, string reason)
        {
            var request = await _context.HardwareChangeRequests.FindAsync(id);
            if (request == null || (request.Status != "Submitted" && request.Status != "Under Review"))
                return false;

            request.Status = "Rejected";
            request.ReviewedByUserId = reviewerId;
            request.ReviewedAt = DateTime.Now;
            request.SecurityReviewComment = reason;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<HardwareChangeRequest>> GetByRequesterAsync(int userId)
        {
            return await _context.HardwareChangeRequests
                .Include(h => h.RequesterUser)
                .Include(h => h.PreparedByUser)
                .Include(h => h.ReviewedByUser)
                .Include(h => h.ApprovedByUser)
                .Where(h => h.RequesterUserId == userId)
                .OrderByDescending(h => h.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<HardwareChangeRequest>> GetPendingReviewAsync()
        {
            return await _context.HardwareChangeRequests
                .Include(h => h.RequesterUser)
                .Include(h => h.PreparedByUser)
                .Include(h => h.ReviewedByUser)
                .Include(h => h.ApprovedByUser)
                .Where(h => h.Status == "Submitted" || h.Status == "Under Review")
                .OrderBy(h => h.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<HardwareChangeRequest>> GetByStatusAsync(string status)
        {
            return await _context.HardwareChangeRequests
                .Include(h => h.RequesterUser)
                .Include(h => h.PreparedByUser)
                .Include(h => h.ReviewedByUser)
                .Include(h => h.ApprovedByUser)
                .Where(h => h.Status == status)
                .OrderByDescending(h => h.CreatedDate)
                .ToListAsync();
        }
    }
} 