using Microsoft.EntityFrameworkCore;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Services
{
    public class SoftwareChangeRequestService : ISoftwareChangeRequestService
    {
        private readonly MaritimeERPContext _context;
        private readonly IAuditLogService _auditLogService;

        public SoftwareChangeRequestService(MaritimeERPContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
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
            
            // Log the create operation
            await _auditLogService.LogCreateAsync(request, "Software Change Request created");
            
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<SoftwareChangeRequest> UpdateAsync(SoftwareChangeRequest request)
        {
            // Get the existing entity first for audit logging
            var existingRequest = await _context.SoftwareChangeRequests.FindAsync(request.Id);
            if (existingRequest == null)
            {
                throw new InvalidOperationException($"Software Change Request with ID {request.Id} not found");
            }

            // Store the old values for audit logging
            var oldRequest = new SoftwareChangeRequest
            {
                Id = existingRequest.Id,
                RequestNumber = existingRequest.RequestNumber,
                Department = existingRequest.Department,
                PositionTitle = existingRequest.PositionTitle,
                RequesterName = existingRequest.RequesterName,
                InstalledCbs = existingRequest.InstalledCbs,
                InstalledComponent = existingRequest.InstalledComponent,
                Reason = existingRequest.Reason,
                BeforeSwManufacturer = existingRequest.BeforeSwManufacturer,
                BeforeSwName = existingRequest.BeforeSwName,
                BeforeSwVersion = existingRequest.BeforeSwVersion,
                AfterSwManufacturer = existingRequest.AfterSwManufacturer,
                AfterSwName = existingRequest.AfterSwName,
                AfterSwVersion = existingRequest.AfterSwVersion,
                WorkDescription = existingRequest.WorkDescription,
                SecurityReviewComment = existingRequest.SecurityReviewComment,
                Status = existingRequest.Status,
                CreatedDate = existingRequest.CreatedDate
            };

            _context.Entry(request).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            // Log the update operation
            await _auditLogService.LogUpdateAsync(oldRequest, request, "Software Change Request updated");
            
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var request = await _context.SoftwareChangeRequests.FindAsync(id);
            if (request == null)
                return false;

            _context.SoftwareChangeRequests.Remove(request);
            await _context.SaveChangesAsync();
            
            // Log the delete operation
            await _auditLogService.LogDeleteAsync(request, "Software Change Request deleted");
            
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

            var oldStatus = request.Status;
            request.Status = "Submitted";
            request.PreparedByUserId = reviewerId;
            request.PreparedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            
            // Log the status change
            await _auditLogService.LogActionAsync("SoftwareChangeRequest", "SUBMIT", request.Id.ToString(), 
                request.RequestNumber, $"Status changed from {oldStatus} to Submitted");
            
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<SoftwareChangeRequest> ApproveAsync(int id, int approverId)
        {
            var request = await _context.SoftwareChangeRequests.FindAsync(id);
            if (request == null || (request.Status != "Under Review" && request.Status != "Submitted"))
                throw new InvalidOperationException("Cannot approve request");

            var oldStatus = request.Status;
            request.Status = "Approved";
            request.ApprovedByUserId = approverId;
            request.ApprovedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            
            // Log the approval
            await _auditLogService.LogActionAsync("SoftwareChangeRequest", "APPROVE", request.Id.ToString(), 
                request.RequestNumber, $"Status changed from {oldStatus} to Approved");
            
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<SoftwareChangeRequest> RejectAsync(int id, int rejectorId, string reason)
        {
            var request = await _context.SoftwareChangeRequests.FindAsync(id);
            if (request == null || (request.Status != "Submitted" && request.Status != "Under Review"))
                throw new InvalidOperationException("Cannot reject request");

            var oldStatus = request.Status;
            request.Status = "Rejected";
            request.ReviewedByUserId = rejectorId;
            request.ReviewedAt = DateTime.Now;
            request.SecurityReviewComment = reason;

            await _context.SaveChangesAsync();
            
            // Log the rejection
            await _auditLogService.LogActionAsync("SoftwareChangeRequest", "REJECT", request.Id.ToString(), 
                request.RequestNumber, $"Status changed from {oldStatus} to Rejected. Reason: {reason}");
            
            return await GetByIdAsync(request.Id) ?? request;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.SoftwareChangeRequests.AnyAsync(s => s.Id == id);
        }
    }
} 