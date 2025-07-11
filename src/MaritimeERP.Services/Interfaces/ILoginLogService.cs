using MaritimeERP.Core.Entities;

namespace MaritimeERP.Services.Interfaces
{
    public interface ILoginLogService
    {
        // Login log CRUD operations
        Task<List<LoginLog>> GetAllLoginLogsAsync();
        Task<List<LoginLog>> GetLoginLogsByUserIdAsync(int userId);
        Task<List<LoginLog>> GetLoginLogsByUsernameAsync(string username);
        Task<LoginLog?> GetLoginLogByIdAsync(int id);

        // Log creation methods
        Task LogSuccessfulLoginAsync(int userId, string username, string? ipAddress = null, string? userAgent = null, string? device = null);
        Task LogFailedLoginAsync(string username, string failureReason, string? ipAddress = null, string? userAgent = null, string? device = null);
        Task LogLogoutAsync(int userId, string username, int sessionDurationMinutes, string? ipAddress = null);
        Task LogPasswordResetAsync(int userId, string username, string? ipAddress = null, string? userAgent = null);
        Task LogPasswordChangeAsync(int userId, string username, string? ipAddress = null);
        Task LogAccountLockedAsync(int userId, string username, string reason, string? ipAddress = null);
        Task LogAccountUnlockedAsync(int userId, string username, string? ipAddress = null);
        Task LogSecurityEventAsync(int? userId, string username, string action, string details, string? ipAddress = null);

        // Filtering and search
        Task<List<LoginLog>> GetLoginLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<LoginLog>> GetFailedLoginAttemptsAsync(int hours = 24);
        Task<List<LoginLog>> GetSecurityEventsAsync(int days = 7);
        Task<List<LoginLog>> GetFilteredLoginLogsAsync(string? username = null, string? action = null, bool? isSuccessful = null, 
            bool? isSecurityEvent = null, DateTime? startDate = null, DateTime? endDate = null);

        // Statistics and analytics
        Task<Dictionary<string, int>> GetLoginStatsByAction();
        Task<Dictionary<string, int>> GetLoginStatsByUser(int days = 30);
        Task<Dictionary<DateTime, int>> GetLoginTrendsByDay(int days = 30);
        Task<List<string>> GetTopFailedLoginUsernamesAsync(int count = 10, int hours = 24);
        Task<int> GetFailedLoginCountAsync(string username, int hours = 1);
        Task<int> GetTotalLoginAttemptsAsync(int days = 1);
        Task<int> GetSuccessfulLoginsAsync(int days = 1);
        Task<double> GetLoginSuccessRateAsync(int days = 7);

        // Security monitoring
        Task<List<LoginLog>> GetSuspiciousActivitiesAsync(int days = 7);
        Task<bool> IsUserLoginSuspiciousAsync(string username, string ipAddress);
        Task<List<string>> GetUniqueIpAddressesAsync(string username, int days = 30);
        Task<DateTime?> GetLastSuccessfulLoginAsync(int userId);
        Task<DateTime?> GetLastFailedLoginAsync(string username);

        // Cleanup and maintenance
        Task<int> CleanupOldLogsAsync(int daysToKeep = 365);
        Task<bool> DeleteLoginLogAsync(int id);
    }
} 