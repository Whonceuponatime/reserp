using Microsoft.EntityFrameworkCore;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Services
{
    public class SoftwareChangeRequestService : ISoftwareChangeRequestService
    {
        private readonly MaritimeERPContext _context;

        public SoftwareChangeRequestService(MaritimeERPContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SoftwareChangeRequest>> GetAllAsync()
        {
            return await _context.SoftwareChangeRequests
                .Include(s => s.RequesterUser)
                .Include(s => s.PreparedByUser)
                .Include(s => s.ReviewedByUser)
                .Include(s => s.ApprovedByUser)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public async Task<SoftwareChangeRequest?> GetByIdAsync(int id)
        {
            return await _context.SoftwareChangeRequests
                .Include(s => s.RequesterUser)
                .Include(s => s.PreparedByUser)
                .Include(s => s.ReviewedByUser)
                .Include(s => s.ApprovedByUser)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<SoftwareChangeRequest?> GetByRequestNumberAsync(string requestNumber)
        {
            return await _context.SoftwareChangeRequests
                .Include(s => s.RequesterUser)
                .Include(s => s.PreparedByUser)
                .Include(s => s.ReviewedByUser)
                .Include(s => s.ApprovedByUser)
                .FirstOrDefaultAsync(s => s.RequestNumber == requestNumber);
        }

        public async Task<SoftwareChangeRequest> CreateAsync(SoftwareChangeRequest request)
        {
            request.CreatedDate = DateTime.Now;
            request.Status = "Draft";
            
            if (string.IsNullOrEmpty(request.RequestNumber))
            {
                request.RequestNumber = await GenerateRequestNumberAsync();
            }

            _context.SoftwareChangeRequests.Add(request);
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<SoftwareChangeRequest> UpdateAsync(SoftwareChangeRequest request)
        {
            _context.Entry(request).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var request = await _context.SoftwareChangeRequests.FindAsync(id);
            if (request == null)
                return false;

            _context.SoftwareChangeRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateRequestNumberAsync()
        {
            var today = DateTime.Now;
            var prefix = $"SW-{today:yyyyMM}";
            
            var lastRequest = await _context.SoftwareChangeRequests
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

        public async Task<SoftwareChangeRequest> SubmitForReviewAsync(int id, int reviewerId)
        {
            var request = await _context.SoftwareChangeRequests.FindAsync(id);
            if (request == null || request.Status != "Draft")
                throw new InvalidOperationException("Cannot submit request for review");

            request.Status = "Submitted";
            request.PreparedByUserId = reviewerId;
            request.PreparedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<SoftwareChangeRequest> ApproveAsync(int id, int approverId)
        {
            var request = await _context.SoftwareChangeRequests.FindAsync(id);
            if (request == null || request.Status != "Under Review")
                throw new InvalidOperationException("Cannot approve request");

            request.Status = "Approved";
            request.ApprovedByUserId = approverId;
            request.ApprovedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<SoftwareChangeRequest> RejectAsync(int id, int rejectorId, string reason)
        {
            var request = await _context.SoftwareChangeRequests.FindAsync(id);
            if (request == null || (request.Status != "Submitted" && request.Status != "Under Review"))
                throw new InvalidOperationException("Cannot reject request");

            request.Status = "Rejected";
            request.ReviewedByUserId = rejectorId;
            request.ReviewedAt = DateTime.Now;
            request.SecurityReviewComment = reason;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.SoftwareChangeRequests.AnyAsync(s => s.Id == id);
        }
    }
} 