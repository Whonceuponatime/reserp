using MaritimeERP.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaritimeERP.Services.Interfaces
{
    public interface ISecurityReviewStatementService
    {
        Task<List<SecurityReviewStatement>> GetAllSecurityReviewStatementsAsync();
        Task<SecurityReviewStatement?> GetSecurityReviewStatementByIdAsync(int id);
        Task<SecurityReviewStatement?> GetSecurityReviewStatementByRequestNumberAsync(string requestNumber);
        Task<SecurityReviewStatement> CreateSecurityReviewStatementAsync(SecurityReviewStatement securityReviewStatement);
        Task<SecurityReviewStatement> UpdateSecurityReviewStatementAsync(SecurityReviewStatement securityReviewStatement);
        Task<bool> DeleteSecurityReviewStatementAsync(int id);
        Task<string> GenerateRequestNumberAsync();
        
        // Review workflow methods
        Task<SecurityReviewStatement> SubmitForReviewAsync(int id, int reviewerId);
        Task<SecurityReviewStatement> ApproveAsync(int id, int approverId);
        Task<SecurityReviewStatement> RejectAsync(int id, int rejectorId, string reason);
        
        // Query methods
        Task<List<SecurityReviewStatement>> GetByReviewerAsync(int userId);
        Task<List<SecurityReviewStatement>> GetPendingReviewAsync();
        Task<List<SecurityReviewStatement>> GetByStatusAsync(string status);
    }
} 