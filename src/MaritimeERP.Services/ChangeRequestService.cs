using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Services
{
    public class ChangeRequestService : IChangeRequestService
    {
        private readonly MaritimeERPContext _context;
        private readonly ILogger<ChangeRequestService> _logger;

        public ChangeRequestService(MaritimeERPContext context, ILogger<ChangeRequestService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ChangeRequest>> GetAllChangeRequestsAsync()
        {
            try
            {
                return await _context.ChangeRequests
                    .Include(cr => cr.Ship)
                    .Include(cr => cr.RequestType)
                    .Include(cr => cr.Status)
                    .Include(cr => cr.RequestedBy)
                    .Include(cr => cr.Approvals)
                        .ThenInclude(a => a.ActionBy)
                    .Include(cr => cr.HardwareChangeDetail)
                    .Include(cr => cr.SoftwareChangeDetail)
                    .Include(cr => cr.SystemPlanDetail)
                    .Include(cr => cr.SecurityReviewItems)
                    .OrderByDescending(cr => cr.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all change requests");
                throw;
            }
        }

        public async Task<IEnumerable<ChangeRequest>> GetChangeRequestsForUserAsync(int userId, string userRole)
        {
            try
            {
                var query = _context.ChangeRequests
                    .Include(cr => cr.Ship)
                    .Include(cr => cr.RequestType)
                    .Include(cr => cr.Status)
                    .Include(cr => cr.RequestedBy)
                    .Include(cr => cr.Approvals)
                        .ThenInclude(a => a.ActionBy)
                    .Include(cr => cr.HardwareChangeDetail)
                    .Include(cr => cr.SoftwareChangeDetail)
                    .Include(cr => cr.SystemPlanDetail)
                    .Include(cr => cr.SecurityReviewItems);

                // Role-based filtering
                if (userRole == "Administrator")
                {
                    // Admins can see all change requests
                    return await query
                        .OrderByDescending(cr => cr.CreatedAt)
                        .ToListAsync();
                }
                else
                {
                    // Normal users (Engineers) can only see their own change requests
                    return await query
                        .Where(cr => cr.RequestedById == userId)
                        .OrderByDescending(cr => cr.CreatedAt)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving change requests for user {UserId} with role {UserRole}", userId, userRole);
                throw;
            }
        }

        public async Task<ChangeRequest?> GetChangeRequestByIdAsync(int id)
        {
            try
            {
                return await _context.ChangeRequests
                    .Include(cr => cr.Ship)
                    .Include(cr => cr.RequestType)
                    .Include(cr => cr.Status)
                    .Include(cr => cr.RequestedBy)
                    .Include(cr => cr.Approvals)
                        .ThenInclude(a => a.ActionBy)
                    .Include(cr => cr.HardwareChangeDetail)
                    .Include(cr => cr.SoftwareChangeDetail)
                    .Include(cr => cr.SystemPlanDetail)
                    .Include(cr => cr.SecurityReviewItems)
                    .FirstOrDefaultAsync(cr => cr.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving change request by ID {Id}", id);
                throw;
            }
        }

        public async Task<ChangeRequest?> GetChangeRequestByRequestNoAsync(string requestNo)
        {
            try
            {
                return await _context.ChangeRequests
                    .Include(cr => cr.Ship)
                    .Include(cr => cr.RequestType)
                    .Include(cr => cr.Status)
                    .Include(cr => cr.RequestedBy)
                    .Include(cr => cr.Approvals)
                        .ThenInclude(a => a.ActionBy)
                    .Include(cr => cr.HardwareChangeDetail)
                    .Include(cr => cr.SoftwareChangeDetail)
                    .Include(cr => cr.SystemPlanDetail)
                    .Include(cr => cr.SecurityReviewItems)
                    .FirstOrDefaultAsync(cr => cr.RequestNo == requestNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving change request by request number {RequestNo}", requestNo);
                throw;
            }
        }

        public async Task<ChangeRequest> CreateChangeRequestAsync(ChangeRequest changeRequest)
        {
            try
            {
                // Generate request number if not provided
                if (string.IsNullOrEmpty(changeRequest.RequestNo))
                {
                    changeRequest.RequestNo = await GenerateRequestNumberAsync(changeRequest.RequestTypeId);
                }

                changeRequest.CreatedAt = DateTime.UtcNow;
                changeRequest.RequestedAt = DateTime.UtcNow;

                // Set initial status to Draft
                if (changeRequest.StatusId == 0)
                {
                    changeRequest.StatusId = 1; // Draft
                }

                _context.ChangeRequests.Add(changeRequest);
                await _context.SaveChangesAsync();

                // Reload with includes
                return await GetChangeRequestByIdAsync(changeRequest.Id) ?? changeRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating change request");
                throw;
            }
        }

        public async Task<ChangeRequest> UpdateChangeRequestAsync(ChangeRequest changeRequest)
        {
            try
            {
                // Get the existing entity from the database to avoid tracking conflicts
                var existingRequest = await _context.ChangeRequests.FindAsync(changeRequest.Id);
                if (existingRequest == null)
                {
                    throw new InvalidOperationException($"ChangeRequest with ID {changeRequest.Id} not found");
                }

                // Update the properties of the existing tracked entity
                existingRequest.RequestNo = changeRequest.RequestNo;
                existingRequest.ShipId = changeRequest.ShipId;
                existingRequest.RequestTypeId = changeRequest.RequestTypeId;
                existingRequest.StatusId = changeRequest.StatusId;
                existingRequest.RequestedById = changeRequest.RequestedById;
                existingRequest.RequestedAt = changeRequest.RequestedAt;
                existingRequest.Purpose = changeRequest.Purpose;
                existingRequest.Description = changeRequest.Description;
                existingRequest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Reload with includes
                return await GetChangeRequestByIdAsync(changeRequest.Id) ?? existingRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating change request");
                throw;
            }
        }

        public async Task<bool> DeleteChangeRequestAsync(int id)
        {
            try
            {
                var changeRequest = await _context.ChangeRequests.FindAsync(id);
                if (changeRequest != null)
                {
                    _context.ChangeRequests.Remove(changeRequest);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting change request {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ChangeRequest>> GetChangeRequestsByShipAsync(int shipId)
        {
            try
            {
                return await _context.ChangeRequests
                    .Include(cr => cr.Ship)
                    .Include(cr => cr.RequestType)
                    .Include(cr => cr.Status)
                    .Include(cr => cr.RequestedBy)
                    .Where(cr => cr.ShipId == shipId)
                    .OrderByDescending(cr => cr.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving change requests for ship {ShipId}", shipId);
                throw;
            }
        }

        public async Task<IEnumerable<ChangeRequest>> GetChangeRequestsByStatusAsync(int statusId)
        {
            try
            {
                return await _context.ChangeRequests
                    .Include(cr => cr.Ship)
                    .Include(cr => cr.RequestType)
                    .Include(cr => cr.Status)
                    .Include(cr => cr.RequestedBy)
                    .Where(cr => cr.StatusId == statusId)
                    .OrderByDescending(cr => cr.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving change requests by status {StatusId}", statusId);
                throw;
            }
        }

        public async Task<IEnumerable<ChangeRequest>> GetChangeRequestsByUserAsync(int userId)
        {
            try
            {
                return await _context.ChangeRequests
                    .Include(cr => cr.Ship)
                    .Include(cr => cr.RequestType)
                    .Include(cr => cr.Status)
                    .Include(cr => cr.RequestedBy)
                    .Where(cr => cr.RequestedById == userId)
                    .OrderByDescending(cr => cr.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving change requests for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ChangeRequest>> GetPendingApprovalsAsync(int userId)
        {
            try
            {
                // Get change requests that are submitted for review but not yet approved by this user
                return await _context.ChangeRequests
                    .Include(cr => cr.Ship)
                    .Include(cr => cr.RequestType)
                    .Include(cr => cr.Status)
                    .Include(cr => cr.RequestedBy)
                    .Include(cr => cr.Approvals)
                    .Where(cr => cr.StatusId == 2 || cr.StatusId == 3) // Submitted or Under Review
                    .Where(cr => !cr.Approvals.Any(a => a.ActionById == userId))
                    .OrderByDescending(cr => cr.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending approvals for user {UserId}", userId);
                throw;
            }
        }

        public async Task<string> GenerateRequestNumberAsync(int changeTypeId)
        {
            try
            {
                var changeType = await _context.ChangeTypes.FindAsync(changeTypeId);
                var prefix = changeType?.Name switch
                {
                    "Hardware Change" => "HW",
                    "Software Change" => "SW",
                    "System Plan" => "SP",
                    _ => "CR"
                };

                var year = DateTime.Now.Year;
                var month = DateTime.Now.Month;

                // Find the next sequence number for this type and month
                var lastRequest = await _context.ChangeRequests
                    .Where(cr => cr.RequestTypeId == changeTypeId)
                    .Where(cr => cr.CreatedAt.Year == year && cr.CreatedAt.Month == month)
                    .OrderByDescending(cr => cr.RequestNo)
                    .FirstOrDefaultAsync();

                int sequence = 1;
                if (lastRequest != null && !string.IsNullOrEmpty(lastRequest.RequestNo))
                {
                    // Extract sequence number from last request
                    var parts = lastRequest.RequestNo.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int lastSequence))
                    {
                        sequence = lastSequence + 1;
                    }
                }

                return $"{prefix}-{year:0000}{month:00}-{sequence:000}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating request number for change type {ChangeTypeId}", changeTypeId);
                throw;
            }
        }

        public async Task<bool> SubmitForApprovalAsync(int changeRequestId, int userId)
        {
            try
            {
                var changeRequest = await _context.ChangeRequests.FindAsync(changeRequestId);
                if (changeRequest == null) return false;

                // Update status to Submitted
                changeRequest.StatusId = 2; // Submitted
                changeRequest.UpdatedAt = DateTime.UtcNow;

                // Add approval record
                var approval = new Approval
                {
                    ChangeRequestId = changeRequestId,
                    Stage = 1,
                    Action = "Submitted",
                    ActionById = userId,
                    ActionAt = DateTime.UtcNow,
                    Comment = "Change request submitted for approval"
                };

                _context.Approvals.Add(approval);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Change request {ChangeRequestId} submitted for approval by user {UserId}", changeRequestId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting change request {ChangeRequestId} for approval", changeRequestId);
                throw;
            }
        }

        public async Task<bool> ApproveChangeRequestAsync(int changeRequestId, int userId, string? comment = null)
        {
            try
            {
                var changeRequest = await _context.ChangeRequests
                    .Include(cr => cr.Approvals)
                    .FirstOrDefaultAsync(cr => cr.Id == changeRequestId);
                
                if (changeRequest == null) return false;

                // Update status to Approved
                changeRequest.StatusId = 4; // Approved
                changeRequest.UpdatedAt = DateTime.UtcNow;

                // Add approval record
                var approval = new Approval
                {
                    ChangeRequestId = changeRequestId,
                    Stage = (short)(changeRequest.Approvals.Count + 1),
                    Action = "Approved",
                    ActionById = userId,
                    ActionAt = DateTime.UtcNow,
                    Comment = comment ?? "Change request approved"
                };

                _context.Approvals.Add(approval);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Change request {ChangeRequestId} approved by user {UserId}", changeRequestId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving change request {ChangeRequestId}", changeRequestId);
                throw;
            }
        }

        public async Task<bool> RejectChangeRequestAsync(int changeRequestId, int userId, string comment)
        {
            try
            {
                var changeRequest = await _context.ChangeRequests
                    .Include(cr => cr.Approvals)
                    .FirstOrDefaultAsync(cr => cr.Id == changeRequestId);
                
                if (changeRequest == null) return false;

                // Update status to Rejected
                changeRequest.StatusId = 5; // Rejected
                changeRequest.UpdatedAt = DateTime.UtcNow;

                // Add approval record
                var approval = new Approval
                {
                    ChangeRequestId = changeRequestId,
                    Stage = (short)(changeRequest.Approvals.Count + 1),
                    Action = "Rejected",
                    ActionById = userId,
                    ActionAt = DateTime.UtcNow,
                    Comment = comment
                };

                _context.Approvals.Add(approval);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Change request {ChangeRequestId} rejected by user {UserId}", changeRequestId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting change request {ChangeRequestId}", changeRequestId);
                throw;
            }
        }

        public async Task<bool> ImplementChangeRequestAsync(int changeRequestId, int userId)
        {
            try
            {
                var changeRequest = await _context.ChangeRequests
                    .Include(cr => cr.Approvals)
                    .FirstOrDefaultAsync(cr => cr.Id == changeRequestId);
                
                if (changeRequest == null) return false;

                // Update status to Completed
                changeRequest.StatusId = 6; // Completed
                changeRequest.UpdatedAt = DateTime.UtcNow;

                // Add approval record
                var approval = new Approval
                {
                    ChangeRequestId = changeRequestId,
                    Stage = (short)(changeRequest.Approvals.Count + 1),
                    Action = "Implemented",
                    ActionById = userId,
                    ActionAt = DateTime.UtcNow,
                    Comment = "Change request implementation completed"
                };

                _context.Approvals.Add(approval);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Change request {ChangeRequestId} marked as implemented by user {UserId}", changeRequestId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error implementing change request {ChangeRequestId}", changeRequestId);
                throw;
            }
        }

        public async Task<ChangeRequestStatistics> GetChangeRequestStatisticsAsync()
        {
            try
            {
                var stats = new ChangeRequestStatistics();

                // Total requests
                stats.TotalRequests = await _context.ChangeRequests.CountAsync();

                // By status
                stats.PendingApproval = await _context.ChangeRequests.CountAsync(cr => cr.StatusId == 2 || cr.StatusId == 3);
                stats.Approved = await _context.ChangeRequests.CountAsync(cr => cr.StatusId == 4);
                stats.Implemented = await _context.ChangeRequests.CountAsync(cr => cr.StatusId == 6);
                stats.Rejected = await _context.ChangeRequests.CountAsync(cr => cr.StatusId == 5);

                // By type
                var typeStats = await _context.ChangeRequests
                    .Include(cr => cr.RequestType)
                    .GroupBy(cr => cr.RequestType.Name)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var typeStat in typeStats)
                {
                    stats.RequestsByType[typeStat.Type] = typeStat.Count;
                }

                // By month (last 12 months)
                var monthStats = await _context.ChangeRequests
                    .Where(cr => cr.CreatedAt >= DateTime.Now.AddMonths(-12))
                    .GroupBy(cr => new { cr.CreatedAt.Year, cr.CreatedAt.Month })
                    .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() })
                    .ToListAsync();

                foreach (var monthStat in monthStats)
                {
                    var monthKey = $"{monthStat.Year}-{monthStat.Month:00}";
                    stats.RequestsByMonth[monthKey] = monthStat.Count;
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving change request statistics");
                throw;
            }
        }
    }
} 