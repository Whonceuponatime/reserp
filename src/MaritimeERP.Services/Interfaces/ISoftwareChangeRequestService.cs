using MaritimeERP.Core.Entities;

namespace MaritimeERP.Services.Interfaces
{
    public interface ISoftwareChangeRequestService
    {
        Task<IEnumerable<SoftwareChangeRequest>> GetAllAsync();
        Task<SoftwareChangeRequest?> GetByIdAsync(int id);
        Task<SoftwareChangeRequest?> GetByRequestNumberAsync(string requestNumber);
        Task<SoftwareChangeRequest> CreateAsync(SoftwareChangeRequest softwareChangeRequest);
        Task<SoftwareChangeRequest> UpdateAsync(SoftwareChangeRequest softwareChangeRequest);
        Task<bool> DeleteAsync(int id);
        Task<SoftwareChangeRequest> SubmitForReviewAsync(int id, int reviewerId);
        Task<SoftwareChangeRequest> ApproveAsync(int id, int approverId);
        Task<SoftwareChangeRequest> RejectAsync(int id, int rejectorId, string reason);
        Task<bool> ExistsAsync(int id);
        Task<string> GenerateRequestNumberAsync();
    }
} 