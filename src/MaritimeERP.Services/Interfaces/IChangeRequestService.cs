using MaritimeERP.Core.Entities;

namespace MaritimeERP.Services.Interfaces
{
    public interface IChangeRequestService
    {
        Task<IEnumerable<ChangeRequest>> GetAllChangeRequestsAsync();
        Task<IEnumerable<ChangeRequest>> GetChangeRequestsForUserAsync(int userId, string userRole);
        Task<ChangeRequest?> GetChangeRequestByIdAsync(int id);
        Task<ChangeRequest?> GetChangeRequestByRequestNoAsync(string requestNo);
        Task<ChangeRequest> CreateChangeRequestAsync(ChangeRequest changeRequest);
        Task<ChangeRequest> UpdateChangeRequestAsync(ChangeRequest changeRequest);
        Task<bool> DeleteChangeRequestAsync(int id);
        
        Task<IEnumerable<ChangeRequest>> GetChangeRequestsByShipAsync(int shipId);
        Task<IEnumerable<ChangeRequest>> GetChangeRequestsByStatusAsync(int statusId);
        Task<IEnumerable<ChangeRequest>> GetChangeRequestsByUserAsync(int userId);
        Task<IEnumerable<ChangeRequest>> GetPendingApprovalsAsync(int userId);
        
        Task<string> GenerateRequestNumberAsync(int changeTypeId);
        Task<bool> SubmitForApprovalAsync(int changeRequestId, int userId);
        Task<bool> ApproveChangeRequestAsync(int changeRequestId, int userId, string? comment = null);
        Task<bool> RejectChangeRequestAsync(int changeRequestId, int userId, string comment);
        Task<bool> ImplementChangeRequestAsync(int changeRequestId, int userId);
        
        Task<ChangeRequestStatistics> GetChangeRequestStatisticsAsync();
    }

    public class ChangeRequestStatistics
    {
        public int TotalRequests { get; set; }
        public int PendingApproval { get; set; }
        public int Approved { get; set; }
        public int Implemented { get; set; }
        public int Rejected { get; set; }
        public Dictionary<string, int> RequestsByType { get; set; } = new();
        public Dictionary<string, int> RequestsByMonth { get; set; } = new();
    }
} 