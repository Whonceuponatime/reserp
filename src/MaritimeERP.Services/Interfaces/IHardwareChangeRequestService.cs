using MaritimeERP.Core.Entities;

namespace MaritimeERP.Services.Interfaces
{
    public interface IHardwareChangeRequestService
    {
        Task<List<HardwareChangeRequest>> GetAllAsync();
        Task<HardwareChangeRequest?> GetByIdAsync(int id);
        Task<HardwareChangeRequest> CreateAsync(HardwareChangeRequest request);
        Task<HardwareChangeRequest> UpdateAsync(HardwareChangeRequest request);
        Task<bool> DeleteAsync(int id);
        Task<string> GenerateRequestNumberAsync();
        Task<bool> SubmitForReviewAsync(int id, int userId);
        Task<bool> ReviewAsync(int id, int reviewerId, string comment);
        Task<bool> ApproveAsync(int id, int approverId);
        Task<bool> RejectAsync(int id, int reviewerId, string reason);
        Task<List<HardwareChangeRequest>> GetByRequesterAsync(int userId);
        Task<List<HardwareChangeRequest>> GetPendingReviewAsync();
        Task<List<HardwareChangeRequest>> GetByStatusAsync(string status);
    }
} 