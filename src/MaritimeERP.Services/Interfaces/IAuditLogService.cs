using MaritimeERP.Core.Entities;

namespace MaritimeERP.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task<List<AuditLog>> GetAllAuditLogsAsync();
        Task<List<AuditLog>> GetAuditLogsByEntityTypeAsync(string entityType);
        Task<List<AuditLog>> GetAuditLogsByActionAsync(string action);
        Task<List<AuditLog>> GetAuditLogsByUserAsync(int userId);
        Task<List<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<AuditLog>> GetFilteredAuditLogsAsync(string? entityType = null, string? action = null, int? userId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<string>> GetDistinctEntityTypesAsync();
        Task<List<string>> GetDistinctActionsAsync();
        Task LogAsync(string entityType, string action, string entityId, string? entityName = null, string? oldValues = null, string? newValues = null, string? additionalInfo = null);
        Task LogCreateAsync<T>(T entity, string? additionalInfo = null) where T : class;
        Task LogUpdateAsync<T>(T oldEntity, T newEntity, string? additionalInfo = null) where T : class;
        Task LogDeleteAsync<T>(T entity, string? additionalInfo = null) where T : class;
        Task LogActionAsync(string entityType, string action, string entityId, string? entityName = null, string? additionalInfo = null);
    }
} 